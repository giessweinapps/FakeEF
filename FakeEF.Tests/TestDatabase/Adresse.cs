using System.ComponentModel.DataAnnotations;

namespace FakeEF.Tests.TestDatabase
{
    public class Adresse
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public virtual Person Person { get; set; }

    }
}