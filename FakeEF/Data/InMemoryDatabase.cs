using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

using FakeEF.DataHandling;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace FakeEF.Data
{
    public class InMemoryDatabase
    {
        private readonly Dictionary<Type, DbSetDataManager> items = new Dictionary<Type, DbSetDataManager>();
        public static InMemoryDatabase Instance { get; } = new InMemoryDatabase();

        private InMemoryDatabase()
        {

        }
        internal void SaveChangesInMemory(List<DbEntityEntry> notYetInDatabase)
        {
            foreach (var item in notYetInDatabase)
            {
                var manager = GetSetData(item.Entity.GetType());

                if (item.State == EntityState.Deleted)
                {
                    manager.Remove(item);
                }

                if (item.State == EntityState.Added)
                {
                    manager.Add(item);
                }

                if (item.State == EntityState.Modified)
                {
                    var id = manager.GetId(item.Entity);
                    var existing = manager.Data.FirstOrDefault(x => x.Key.Equals(id));

                    foreach (var property in manager.Type.GetProperties())
                    {
                        if (property.PropertyType.IsPrimitive || property.PropertyType == typeof(string))
                        {
                            var newValue = property.GetValue(item.Entity);
                            property.SetValue(existing.Entity, newValue);
                        }
                    }
                }
            }
        }

        private DbSetDataManager GetSetData(Type type)
        {
            DbSetDataManager manager;
            if (!items.TryGetValue(type, out manager))
            {
                manager = new DbSetDataManager(type);
                items.Add(type, manager);
            }
            return manager;
        }

        internal IEnumerable<T> LoadData<T>(DbContext context, List<string> includes, bool asNoTracking) where T : class
        {
            var manager = GetSetData(typeof(T));
            var data = CloneItems<T>(context, manager, includes, asNoTracking);
            foreach (var item in data)
                yield return item;
        }


        private IEnumerable<T> CloneItems<T>(DbContext context, DbSetDataManager manager,List<string> includes, bool asNoTracking) where T : class
        {
            var data = manager.Data
                .Select(x => (T)x.Entity)
                .Select(x => Clone(x,  includes))
                .ToDictionary(k => manager.GetId(k), v => v);

            foreach (var item in data)
            {
                var existing = context
                    .ChangeTracker
                    .Entries<T>()
                    .ToDictionary(k => manager.GetId(k.Entity), v => v.Entity);

                if (existing.ContainsKey(item.Key))
                    yield return existing[item.Key];
                else
                {
                    var navigationProperties = item.Value.GetType()
                        .GetProperties()
                        .Where(x => (x.PropertyType.IsClass ||
                                     x.PropertyType.IsInterface) && x.PropertyType != typeof(string))
                        .ToList();
                    foreach (var navProperty in navigationProperties)
                    {
                        if (IsGenericList(navProperty.PropertyType))
                        {
                            var list = navProperty.GetValue(item.Value) as IList;
                            foreach (var listItem in list.OfType<object>().ToArray())
                            {
                                var alreadyLoaded = LookupIfExistInChangeTracker<T>(context, manager, listItem, listItem.GetType());
                                if (alreadyLoaded != null)
                                {
                                    list.Remove(listItem);
                                    list.Add(alreadyLoaded.Entity);
                                }
                            }
                            continue;
                        }

                        var currentValue = navProperty.GetValue(item.Value);
                        if (currentValue != null)
                        {
                            var alreadyLoaded = LookupIfExistInChangeTracker<T>(context, manager, currentValue, navProperty.PropertyType);
                            if (alreadyLoaded != null)
                            {
                                navProperty.SetValue(item.Value, alreadyLoaded.Entity);
                            }
                        }
                    }

                    if (!asNoTracking)
                    {
                        context.Entry(item.Value).State = EntityState.Unchanged;
                    }
                    yield return item.Value;
                }
            }
        }

        private static DbEntityEntry LookupIfExistInChangeTracker<T>(DbContext context, DbSetDataManager manager, object currentValue,
            Type targetType) where T : class
        {
            var associatedId = manager.GetId(currentValue);

            var alreadyLoaded = context.ChangeTracker.Entries().FirstOrDefault(x => x.Entity.GetType() == targetType &&
                                                                                    manager.GetId(x.Entity).Equals(associatedId));
            return alreadyLoaded;
        }

        private bool IsGenericList(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ICollection<>))
                return true;

            foreach (Type @interface in type.GetInterfaces())
            {
                if (@interface.IsGenericType)
                {
                    if (@interface.GetGenericTypeDefinition() == typeof(ICollection<>))
                    {
                        // if needed, you can also return the type used as generic argument
                        return true;
                    }
                }
            }
            return false;
        }

        internal T Clone<T>(T source,List<string> includes)
        {
            var settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                ContractResolver = new FakeEFJsonContractResolver(true, includes)
            };

            var serialized = JsonConvert.SerializeObject(source, Formatting.None, settings);
            var result = JsonConvert.DeserializeObject<T>(serialized, settings);
            return result;
        }

        public void Clear()
        {
            items.Clear();
        }
    }
}