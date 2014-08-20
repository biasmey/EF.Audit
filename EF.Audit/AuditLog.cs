using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.Core;

namespace EF.Audit
{
    [Serializable]
    public class AuditLog
    {
        public AuditLog()
        {
            Id = Guid.NewGuid();
        }

        [Key]
        public Guid Id { get; set; }

        public DateTime Created { get; set; }
        public string EntityFullName { get; set; }
        public byte [] Entity { get; set; }
        public string EntityId { get; set; }
        public string User { get; set; }

        //[NotMapped]
        //public EntityKey EntityKey {
        //    get { return Utils.Deserialize<EntityKey>(EntityIdBytes); }
        //}

        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public string PropertyName { get; set; }
        public LogOperation Operation { get; set; }
    }
}