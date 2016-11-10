using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Linq;

namespace FakeEF.EFInterception
{
    public static class DbMethodExtensions
    {
        
        public static void SetupAsTestDbContext<T>(this T context) where T : DbContext
        {
            Database.SetInitializer<T>(null);
            var allSaveables = new List<ISaveable>();
            foreach (var property in typeof (T).GetProperties())
            {
                if (property.PropertyType.IsGenericType &&
                    (property.PropertyType.GetGenericTypeDefinition() == typeof(IDbSet<>) ||
                    property.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>)))
                {
                    var argument = property.PropertyType.GetGenericArguments();
                    var genericClass = typeof (StubDbSet<>).MakeGenericType(argument);
                    var stubDbSet = (ISaveable) Activator.CreateInstance(genericClass, context);
                    allSaveables.Add(stubDbSet);

                    property.SetValue(context, stubDbSet);
                }
            }

            ((IObjectContextAdapter)context).ObjectContext.SavingChanges += (o, e) =>
            {
                var validationErrors = context.GetValidationErrors().ToList();
                if (validationErrors.Any())
                    throw new DbEntityValidationException("Validation Failed", validationErrors);

                foreach (var item in allSaveables)
                    item.Save();

                ((IObjectContextAdapter)context).ObjectContext.AcceptAllChanges();
            };

        }
    }
}