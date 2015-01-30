using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;

namespace FakeEF
{
    public interface ISaveable
    {
        void Save();
    }

    public class StubDbSet<T> : IDbSet<T>, ISaveable where T : class, new()
    {
        private List<T> contextData;
        private List<T> localData = new List<T>();
        private IEnumerable<T> CurrentData
        {
            get { return contextData.Concat(localData); }
        }

        private readonly DbContext dbContext;

        public StubDbSet(DbContext dbContext)
        {
            this.dbContext = dbContext;
            contextData = new List<T>(InMemoryTable<T>.Instance.CloneItems());
        }

        public void Save()
        {
            var notYetInDatabase = dbContext.ChangeTracker
                .Entries<T>()
                .Where(x => x.State != EntityState.Unchanged)
                .ToList();

            foreach (var item in notYetInDatabase)
            {
                if (item.State == EntityState.Added)
                    contextData.Add(item.Entity);
                else if (item.State == EntityState.Deleted)
                    contextData.Remove(item.Entity);
            }

            InMemoryTable<T>.Instance.SaveChangesInMemory(notYetInDatabase);
            notYetInDatabase.Clear();

            localData.Clear();
        }

        public T Add(T entity)
        {
            if(!localData.Contains(entity))
                localData.Add(entity);
            dbContext.Entry(entity).State = EntityState.Added;
            return entity;
        }

        public T Attach(T entity)
        {
            if (!localData.Contains(entity))
                localData.Add(entity);
            dbContext.Entry(entity).State = EntityState.Modified;
            return entity;
        }

        public TDerivedEntity Create<TDerivedEntity>() where TDerivedEntity : class, T
        {
            throw new NotImplementedException();
        }

        public T Create()
        {
            return new T();
        }

        public T Find(params object[] keyValues)
        {
            var type = typeof(T);
            var idProperty = type.GetProperty("Id");
            return contextData.FirstOrDefault(x => idProperty.GetValue(x).Equals(keyValues[0]));
        }

        public ObservableCollection<T> Local
        {
            get { return new ObservableCollection<T>(localData); }
        }

        public T Remove(T entity)
        {
            foreach (var dbEntry in dbContext.ChangeTracker.Entries().ToList())
            {
                var entry = dbEntry.Entity;
                foreach (var property in entry.GetType().GetProperties())
                {
                    var list = property.GetValue(entry) as IList;
                    if (list != null)
                    {
                        if (list.Contains(entity))
                        {
                            list.Remove(entity);
                        }
                    }
                }
            }

            if (dbContext.Entry(entity).State == EntityState.Added)
            {
                dbContext.Entry(entity).State = EntityState.Detached;
            }
            else
            {
                dbContext.Entry(entity).State = EntityState.Deleted;
            }

            if (localData.Contains(entity))
            {
                localData.Remove(entity);
            }
            return entity;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return CurrentData.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return CurrentData.GetEnumerator();
        }

        public Type ElementType
        {
            get { return CurrentData.AsQueryable().ElementType; }
        }

        public System.Linq.Expressions.Expression Expression
        {
            get { return CurrentData.AsQueryable().Expression; }
        }

        public IQueryProvider Provider
        {
            get { return CurrentData.AsQueryable().Provider; }
        }
    }
}