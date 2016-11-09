using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace FakeEF.Tests.TestDatabase
{
    public class Person
    {
        public Person()
        {
            Addresses = new Collection<Adresse>();
        }
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public virtual ICollection<Adresse> Addresses { get; set; }
        public virtual Person ChildPerson { get; set; }
    }
}