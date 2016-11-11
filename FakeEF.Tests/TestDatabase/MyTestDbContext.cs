using System.Data.Entity;

namespace FakeEF.Tests.TestDatabase
{
    public class MyTestDbContext : DbContext
    {
        public virtual DbSet<Person> Persons { get; set; }
        public virtual DbSet<Adresse> Adresses { get; set; }
        public virtual DbSet<TestData> TestData { get; set; }
    }

    public class TestData
    {
        public int Id { get; internal set; }
        public TestData NestedData { get; set; }
        public int Number { get; set; }
    }
}