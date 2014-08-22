using System.Data.Entity;

namespace EF.Audit
{
    public interface IAuditDbContext
    {
        DbSet<AuditLog> AuditLogs { get; set; }
    }
}