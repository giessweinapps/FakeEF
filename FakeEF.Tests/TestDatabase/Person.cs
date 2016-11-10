using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace FakeEF.Tests.TestDatabase
{
    public class Person
    {
        public Person()
        {
            Addresses = new Collection<Adresse>();
        }
        [JsonIgnore]
        public int Id { get; internal set; }
        internal int TestProperty { get; set; }
        [Required]
        public string Name { get; set; }
        public virtual ICollection<Adresse> Addresses { get; set; }
        public virtual Person ChildPerson { get; set; }
    }
}