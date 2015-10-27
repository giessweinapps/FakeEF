using System.Data.Entity;

namespace FakeEF.Tests.TestDatabase
{
    public class MyTestDbContext : DbContext
    {
        public virtual DbSet<Person> Persons { get; set; }
        public virtual DbSet<Adresse> Adresses { get; set; }
    }
}