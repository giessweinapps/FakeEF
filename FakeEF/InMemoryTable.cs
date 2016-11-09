using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace FakeEF
{
    public class InMemoryTable<T> : InMemoryTableBase
        where T : class
    {
        private static readonly InMemoryTable<T> instance = new InMemoryTable<T>();
        private readonly List<T> data = new List<T>();
        private int idCounter = 1;

        private InMemoryTable()
        {
        }

        public static InMemoryTable<T> Instance
        {
            get { return instance; }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Instance.data.Concat(data).GetEnumerator();
        }

        public void SaveChangesInMemory(IEnumerable<DbEntityEntry<T>> notYetInDatabase)
        {
            foreach (var item in notYetInDatabase)
            {
                if (item.State == EntityState.Deleted)
                    data.Remove(item.Entity);

                if (item.State == EntityState.Added)
                {
                    SetId(item.Entity);
                    var idName = GetIdPropertyInfo(item.Entity.GetType()).Name;
                    item.Property(idName).IsModified = false;
                    data.Add(item.Entity);
                }

                if (item.State == EntityState.Modified)
                {
                    var id = GetId(item.Entity);
                    var existing = data.FirstOrDefault(x => GetId(x).Equals(id));

                    foreach (var property in typeof(T).GetProperties())
                    {
                        if (property.PropertyType.IsPrimitive || property.PropertyType == typeof(string))
                        {
                            var newValue = property.GetValue(item.Entity);
                            property.SetValue(existing, newValue);
                        }
                    }
                }
            }
        }

        internal object GetId(object item)
        {
            var type = item.GetType();
            return GetIdPropertyInfo(type).GetValue(item);
        }

        internal void SetId(object item)
        {
            var type = item.GetType();
            var id = GetIdPropertyInfo(type);

            if (id != null)
            {
                Trace.WriteLine(string.Format("Setting Property ({0}) of {1} to {2}", id.Name, type.Name,
                    idCounter));
                id.SetValue(item, idCounter);
                idCounter++;
            }
        }

        private static PropertyInfo GetIdPropertyInfo(Type type)
        {
            return type.GetProperties().FirstOrDefault(x => x.Name.ToLower() == "id") ??
                   type.GetProperties().FirstOrDefault(x => x.Name.ToLower().Contains("id"));
        }

        public override void Clear()
        {
            data.Clear();
            idCounter = 1;
        }

        public override IEnumerable GetData()
        {
            return data.AsEnumerable();
        }

        public IEnumerable<T> CloneItems(bool withProxy, List<string> includes)
        {
            return instance.data.Select(x => Clone(x, withProxy, includes)).ToList();
        }

        public T Clone(T source, bool withProxy, List<string> includes)
        {
            if (withProxy)
            {
                var serialized = JsonConvert.SerializeObject(source, Formatting.Indented,
                    new JsonSerializerSettings
                    {
                        ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                        PreserveReferencesHandling = PreserveReferencesHandling.Objects
                    });
                return JsonConvert.DeserializeObject<T>(serialized);
            }
            else
            {
                var serialized = JsonConvert.SerializeObject(source, Formatting.Indented,
                    new JsonSerializerSettings
                    {
                        ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                        PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                        ContractResolver = new NonProxyContractResolver(includes)
                    });
                return JsonConvert.DeserializeObject<T>(serialized);
            }
        }
    }

    public class NonProxyContractResolver : DefaultContractResolver
    {
        private readonly List<string> includes;

        public NonProxyContractResolver(List<string> includes)
        {
            this.includes = includes;
        }

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var properties = base.CreateProperties(type, memberSerialization).Where(x => FilterProperty(x)).ToArray();
            return properties;
        }

        private bool FilterProperty(JsonProperty jsonProperty)
        {
            if ((jsonProperty.PropertyType.IsClass || jsonProperty.PropertyType.IsInterface) && 
                jsonProperty.PropertyType != typeof(string))
            {
                if (includes.Any(y => y.Contains(jsonProperty.PropertyName)))
                    return true;
                return false;
            }
            return true;
        }
    }
}