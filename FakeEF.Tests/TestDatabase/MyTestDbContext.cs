using System.Data.Entity;

namespace FakeEF.Tests.TestDatabase
{
    public class MyTestDbContext : DbContext
    {
        public virtual IDbSet<Person> Persons { get; set; }
        public virtual IDbSet<Adresse> Adresses { get; set; }
    }
}