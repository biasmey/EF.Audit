using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Interception;

namespace EF.Audit.Test
{
    class MyContext : DbContext, IAuditDbContext
    {
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Person> Persons { get; set; }

        public MyContext()
        {
            //DbInterception.Add(new AuditLogInterceptor());
        }
    }

    [Serializable]
    public class Person
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Auditable]
        public string Name { get; set; }

        [Auditable]
        public int Age { get; set; }

        public Person()
        {
            Dogs = new HashSet<Dog>();
        }

        public virtual ICollection<Dog> Dogs { get; set; }
    }

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