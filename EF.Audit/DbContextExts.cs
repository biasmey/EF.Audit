using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.SqlServer;
using System.Linq;

namespace EF.Audit
{
    public static class DbContextExts
    {
        public static int SaveChangesAndAudit<T>(this T context) where T : DbContext, IAuditDbContext
        {
            var iAuditDb = context as IAuditDbContext;
            if (iAuditDb == null)
            {
                throw new ArgumentException("This context should be of type IAuditDbContext");
            }

            using (var tram = context.Database.BeginTransaction())
            {
                try
                {
                    var addedEntries = context.ChangeTracker.Entries().Where(e => e.State == EntityState.Added).ToList();
                    var modifiedEntries = context.ChangeTracker.Entries().Where(e => e.State == EntityState.Deleted || e.State == EntityState.Modified).ToList();

                    foreach (var entry in modifiedEntries)
                    {
                        ApplyAuditLog(context, entry);
                    }

                    var result = context.SaveChanges();
                    foreach (var entry in addedEntries)
                    {
                        ApplyAuditLog(context, entry, LogOperation.Create);
                    }

                    result += context.SaveChanges();
                    tram.Commit();
                    return result;
                }
                catch (Exception)
                {
                    tram.Rollback();
                    return 0;
                }
            }
        }

        private static void ApplyAuditLog<T>(T context, DbEntityEntry entry) where T : DbContext, IAuditDbContext
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
                    operation = LogOperation.Unchanged;
                    break;
            }

            ApplyAuditLog(context, entry, operation);
        }

        private static void ApplyAuditLog<T>(T context, DbEntityEntry entry, LogOperation logOperation) where T : DbContext, IAuditDbContext
        {
            var includedProperties = new List<string>();
            var entityKey = context.GetEntityKey(entry.Entity);
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
                var originalValues = context.Entry(entry.Entity).GetDatabaseValues();
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
                        context.AuditLogs.Add(log);
                    }
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

                context.AuditLogs.Add(log);
            }
        }

        public static EntityKey GetEntityKey<T>(this IObjectContextAdapter context, T entity) where T : class
        {
            var oc = context.ObjectContext;
            ObjectStateEntry ose;

            if (null != entity && oc.ObjectStateManager.TryGetObjectStateEntry(entity, out ose))
            {
                return ose.EntityKey;
            }

            return null;
        }

        public static IEnumerable<AuditRecord<T>> GetSnapshot<T, TC>(this TC context, DateTime date)
            where T : class
            where TC : DbContext, IAuditDbContext
        {
            var d = date.Date;
            var fullName = typeof(T).FullName;
            var logs = context.AuditLogs.Where(
                q => q.EntityFullName.Equals(fullName, StringComparison.InvariantCultureIgnoreCase) &&
                     SqlFunctions.DateDiff("DAY", q.Created, d) == 0).OrderBy(q => q.Created).ToList();

            return logs.Select(log => new AuditRecord<T>
            {
                Entity = Utils.Deserialize<T>(log.Entity),
                Date = log.Created
            });
        }
    }
}