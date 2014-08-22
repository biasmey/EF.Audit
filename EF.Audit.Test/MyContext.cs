using System.Data.Entity;

namespace EF.Audit.Test
{
    class MyContext : DbContext, IAuditDbContext
    {
        public DbSet<Person> Persons { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        public MyContext()
            : base("MyContext")
        {
        }
    }
}