using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using FakeEF.Data;

namespace FakeEF.EFInterception
{
    public class StubDbSet<T> : DbSet<T>, IDbSet<T>, ISaveable where T : class, new()
    {
        private readonly DbContext dbContext;
        private readonly List<string> includes;
        private readonly List<T> localData = new List<T>();
        private bool asNoTracking;

        public StubDbSet(DbContext dbContext)
            : this(dbContext, new List<string>())
        {
        }

        public StubDbSet(DbContext dbContext, List<string> includes)
        {
            this.includes = includes;
            this.dbContext = dbContext;
            Provider = new FakeEfQueryProvider<T>(this, dbContext);
        }

        internal IEnumerable<T> CurrentData
        {
            get
            {
                var data = LoadContextData().Concat(localData);
                return data;
            }
        }

        public override T Add(T entity)
        {
            if (!localData.Contains(entity))
            {
                localData.Add(entity);
            }

            dbContext.Entry(entity).State = EntityState.Added;
            return entity;
        }

        public override T Attach(T entity)
        {
            if (!localData.Contains(entity))
            {
                localData.Add(entity);
            }

            dbContext.Entry(entity).State = EntityState.Modified;
            return entity;
        }

        public override TDerivedEntity Create<TDerivedEntity>()
        {
            throw new NotImplementedException();
        }

        public override T Create()
        {
            return new T();
        }

        public override T Find(params object[] keyValues)
        {
            var type = typeof(T);
            var idProperty = type.GetProperty("Id");
            return LoadContextData().FirstOrDefault(x => idProperty.GetValue(x).Equals(keyValues[0]));
        }

        public override ObservableCollection<T> Local => new ObservableCollection<T>(localData);

        public override T Remove(T entity)
        {
            dbContext.Entry(entity).State = dbContext.Entry(entity).State == EntityState.Added ? EntityState.Detached : EntityState.Deleted;

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

        public Type ElementType { get; } = typeof(T);
        public Expression Expression => Expression.Constant(CurrentData.AsQueryable());
        public IQueryProvider Provider { get; }

        public void Save()
        {
            var notYetInDatabase = dbContext.ChangeTracker
                .Entries()
                .Where(x => x.State != EntityState.Unchanged && x.Entity is T)
                .ToList();
            if (notYetInDatabase.Any())
            {
                InMemoryDatabase.Instance.SaveChangesInMemory(notYetInDatabase);
            }

            notYetInDatabase.Clear();

            localData.Clear();
        }

        public override DbQuery<T> AsNoTracking()
        {
            asNoTracking = true;
            return this;
        }

        private IEnumerable<T> LoadContextData()
        {
            return InMemoryDatabase.Instance.LoadData<T>(dbContext, includes, asNoTracking);
        }

        public override IEnumerable<T> AddRange(IEnumerable<T> entities)
        {
            foreach (var item in entities)
            {
                Add(item);
            }

            return CurrentData;
        }

        public override IEnumerable<T> RemoveRange(IEnumerable<T> entities)
        {
            foreach (var item in entities)
            {
                if (localData.Contains(item))
                {
                    localData.Remove(item);
                }
                else
                {
                    dbContext.Entry(item).State = EntityState.Deleted;
                }
            }

            return CurrentData;
        }

        public override DbQuery<T> Include(string path)
        {
            includes.Add(path);
            return new StubDbSet<T>(dbContext, includes);
        }

        public void AddInclude(string includeToAdd)
        {
            if (!includes.Contains(includeToAdd))
            {
                includes.Add(includeToAdd);
            }
        }
    }
}