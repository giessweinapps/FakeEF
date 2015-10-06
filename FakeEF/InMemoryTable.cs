using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

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

                    foreach (var property in typeof (T).GetProperties())
                    {
                        if (property.PropertyType.IsPrimitive || property.PropertyType == typeof (string))
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
                idCounter ++;
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

        public IEnumerable<T> CloneItems()
        {
            return instance.data.Select(Clone);
        }

        public T Clone(T source)
        {
            var serialized = JsonConvert.SerializeObject(source, Formatting.Indented,
                new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects
                });
            return JsonConvert.DeserializeObject<T>(serialized);
        }
    }
}