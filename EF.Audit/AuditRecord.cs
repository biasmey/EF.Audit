using System;

namespace EF.Audit
{
    [Serializable]
    public class AuditRecord<T>
    {
        public DateTime Date { get; set; }
        public T Entity { get; set; }
    }
}