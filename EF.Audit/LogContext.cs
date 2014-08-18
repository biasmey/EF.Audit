using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;

namespace EF.Audit
{
    public class LogContext : DbContext, IAuditDbContext
    {
        public DbSet<AuditLog> AuditLogs { get; set; }

        //public IEnumerable<T> GetSnapshot<T, TC>(TC context, DateTime date)
        //    where T : class
        //    where TC : DbContext, IAuditDbContext
        //{
        //    var d = date.Date;
        //    var fullName = typeof(T).FullName;

        //    var logs = context.AuditLogs.Where(
        //        q => q.EntityFullName.Equals(fullName, StringComparison.InvariantCultureIgnoreCase) &&
        //             SqlFunctions.DateDiff("DAY", q.Created, d) == 0).OrderBy(q => q.Created).ToList();

        //    var result = logs.Select(q => Utils.Deserialize<T>(q.Entity));
        //    return result;
        //}

        public void ApplyAuditLog(DbContext workingContext, DbEntityEntry entry)
        {
            LogOperation operation;
            switch (entry.State)
            {
                case EntityState.Added:
                    operation = LogOperation.Create;
                    break;
                case EntityState.Deleted:
                    operation = LogOperation.Delete;
                    break;
                case EntityState.Modified:
                    operation = LogOperation.Update;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            ApplyAuditLog(workingContext, entry, operation);
        }

        public void ApplyAuditLog(DbContext workingContext, DbEntityEntry entry, LogOperation logOperation)
        {
            var includedProperties = new List<string>();
            var entityKey = workingContext.GetEntityKey(entry.Entity);
            var entityType = entry.Entity.GetType();

            if (entry.IsAttr<AuditableAttribute>())
            {
                var props = entityType.GetProperties().Where(pi => !pi.IsAttr<NotAuditableAttrubute>());
                includedProperties.AddRange(props.Select(pi => pi.Name));
            }
            else
            {
                var props = entityType.GetProperties()
                    .Where(p => p.IsAttr<AuditableAttribute>() && !p.IsAttr<NotAuditableAttrubute>());

                includedProperties.AddRange(props.Select(pi => pi.Name));
            }

            if (entry.State == EntityState.Modified)
            {
                var originalValues = workingContext.Entry(entry.Entity).GetDatabaseValues();
                var changedProperties = (from propertyName in originalValues.PropertyNames
                    let propertyEntry = entry.Property(propertyName)
                    let currentValue = propertyEntry.CurrentValue
                    let originalValue = originalValues[propertyName]
                    where (!Equals(currentValue, originalValue) && includedProperties.Contains(propertyName))
                    select new ChangedProperty
                    {
                        Name = propertyName,
                        CurrentValue = currentValue,
                        OriginalValue = originalValue
                    }).ToArray();

                if (changedProperties.Any())
                {
                    foreach (var log in changedProperties.Select(changedProperty => new AuditLog
                    {
                        Created = DateTime.Now,
                        EntityFullName = entry.Entity.GetType().FullName,
                        Entity = Utils.Serialize(entry.Entity),
                        EntityIdBytes = Utils.Serialize(entry.Entity),
                        Operation = logOperation,
                        OldValue = changedProperty.OriginalValue.ToString(),
                        NewValue = changedProperty.CurrentValue.ToString(),
                        PropertyName = changedProperty.Name
                    }))
                    {
                        AuditLogs.Add(log);
                    }

                    SaveChanges();
                }
            }
            else
            {
                var log = new AuditLog
                {
                    Created = DateTime.Now,
                    EntityFullName = entry.Entity.GetType().FullName,
                    Entity = Utils.Serialize(entry.Entity),
                    EntityIdBytes = Utils.Serialize(entityKey),
                    Operation = logOperation,
                };

                AuditLogs.Add(log);
                SaveChanges();
            }
        }
    }
}