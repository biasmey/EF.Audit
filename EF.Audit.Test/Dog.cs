using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EF.Audit.Test
{
    [Auditable]
    [Serializable]
    public class Dog
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public int Year { get; set; }

        public int PersonId { get; set; }
        public virtual Person Person { get; set; }
    }
}