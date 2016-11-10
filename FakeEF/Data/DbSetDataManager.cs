using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace FakeEF.Data
{
    internal class DbSetDataManager
    {
        private int idCounter = 1;
        public Type Type { get; }

        public DbSetDataManager(Type type)
        {
            Type = type;
        }

        public List<KeyDataItem> Data { get; } = new List<KeyDataItem>();

        internal void SetId(object item)
        {
            var type = item.GetType();
            var id = GetIdPropertyInfo(type);

            if (id != null)
            {
                Trace.WriteLine($"Setting Property ({id.Name}) of {type.Name} to {idCounter}");
                id.SetValue(item, idCounter);
                idCounter++;
            }
        }


        public object GetId(object item)
        {
            var type = item.GetType();
            return GetIdPropertyInfo(type).GetValue(item);
        }

        public PropertyInfo GetIdPropertyInfo(Type type)
        {
            return type.GetProperties().FirstOrDefault(x => x.Name.ToLower() == "id") ??
                   type.GetProperties().FirstOrDefault(x => x.Name.ToLower().Contains("id"));
        }

        public void Remove(DbEntityEntry entry)
        {
            var id = GetId(entry.Entity);

            var existing = Data.FirstOrDefault(x => x.Key.Equals(id));
            Data.Remove(existing);
        }

        public void Add(DbEntityEntry entry)
        {
            SetId(entry.Entity);
            var idName = GetIdPropertyInfo(entry.Entity.GetType()).Name;
            entry.Property(idName).IsModified = false;

            Data.Add(new KeyDataItem()
            {
                Entity = entry.Entity,
                Key = GetId(entry.Entity)
            });
        }
    }
}