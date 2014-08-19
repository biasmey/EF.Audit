using System;

namespace EF.Audit
{
    public class AuditRecord<T>
    {
        public DateTime Date { get; set; }
        public T Entity { get; set; }
    }
}