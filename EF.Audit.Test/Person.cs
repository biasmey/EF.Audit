using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EF.Audit.Test
{
    [Serializable]
    public class Person
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Auditable]
        public string Name { get; set; }
        public int Age { get; set; }

        public Person()
        {
            Dogs = new HashSet<Dog>();
        }
        public virtual ICollection<Dog> Dogs { get; set; }
    }
}