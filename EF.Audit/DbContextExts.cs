using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.SqlServer;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EF.Audit
{
    public static class DbContextExts
    {
        /// <summary>
        /// Saves DbContext changes taking into account Audit
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="context">The current context</param>
        /// <returns></returns>
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

        /// <summary>
        /// Saves DbContext changes taking into account Audit
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="context">The current context</param>
        /// <returns></returns>
        public static async Task<int> SaveChangesAndAuditAsync<T>(this T context) where T : DbContext, IAuditDbContext
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

                    var result = await context.SaveChangesAsync();
                    foreach (var entry in addedEntries)
                    {
                        ApplyAuditLog(context, entry, LogOperation.Create);
                    }

                    result += await context.SaveChangesAsync();
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

        /// <summary>
        /// Register audit information
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="context">The current context</param>
        /// <param name="entry">DbContext entry to audit</param>
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

        /// <summary>
        /// Register audit information
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="context">The current context</param>
        /// <param name="entry">DbContext entry to audit</param>
        /// <param name="logOperation">Audit operation</param>
        private static void ApplyAuditLog<T>(T context, DbEntityEntry entry, LogOperation logOperation) where T : DbContext, IAuditDbContext
        {
            var includedProperties = new List<string>();
            var entityKey = context.GetEntityKey(entry.Entity).GetEntityString();
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
                        EntityId = entityKey,
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
                    EntityId = entityKey,
                    Operation = logOperation,
                };

                context.AuditLogs.Add(log);
            }
        }

        /// <summary>
        /// Get's the entity's key
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="context">The current context</param>
        /// <param name="entity">Entity</param>
        /// <returns>The entity key</returns>
        public static EntityKey GetEntityKey<T>(this IObjectContextAdapter context, T entity) where T : class
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }

            var oc = context.ObjectContext;
            ObjectStateEntry ose;

            return oc.ObjectStateManager.TryGetObjectStateEntry(entity, out ose) ? ose.EntityKey : null;
        }

        private static string GetEntityString(this EntityKey entityKey)
        {
            var result = new StringBuilder();
            if (entityKey == null)
            {
                throw new ArgumentNullException("entityKey");
            }

            foreach (var entry in entityKey.EntityKeyValues)
            {
                result.Append(string.Format("{0}={1}", entry.Key, entry.Value));
            }

            result.Remove(result.Length - 1, 1);
            return result.ToString();
        }

        /// <summary>
        /// Get the state of an entity
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <typeparam name="TC">Context type</typeparam>
        /// <param name="context">The current context</param>
        /// <param name="date">snapshot date</param>
        /// <param name="entityKey">the key of the entity to check</param>
        /// <returns></returns>
        public static IEnumerable<AuditRecord<T>> GetSnapshot<T, TC>(this TC context, DateTime date, EntityKey entityKey)
            where T : class
            where TC : DbContext, IAuditDbContext
        {
            if (entityKey == null)
            {
                throw new ArgumentNullException("entityKey");
            }

            var d = date.Date;
            var key = entityKey.GetEntityString();

            var entityType = typeof(T);
            var logs = context.AuditLogs.Where(q =>
                q.EntityFullName.Equals(entityType.FullName, StringComparison.InvariantCultureIgnoreCase) &&
                SqlFunctions.DateDiff("DAY", q.Created, d) == 0 && q.EntityId == key).ToList();

            return logs.OrderBy(l => l.Created).Select(log => new AuditRecord<T>
            {
                Entity = Utils.Deserialize<T>(log.Entity),
                Date = log.Created
            });
        }

        /// <summary>
        /// Get's an entity changes history from it's creation
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <typeparam name="TC">Context type</typeparam>
        /// <param name="context">The current context</param>
        /// <param name="to">Ending date to compare</param>
        /// <param name="entityKey">The key of the entity to check</param>
        /// <returns></returns>
        public static IEnumerable<AuditLog> GetHistory<T, TC>(this TC context, DateTime to, EntityKey entityKey)
            where T : class
            where TC : DbContext, IAuditDbContext
        {
            return GetHistory<T, TC>(context, DateTime.MinValue, to, entityKey);
        }

        /// <summary>
        /// Get's entity changes history in a date range
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <typeparam name="TC">Context type</typeparam>
        /// <param name="context">The current context</param>
        /// <param name="from">Starting date to compare</param>
        /// <param name="to">Ending date to compare</param>
        /// <param name="entityKey">The key of the entity to check</param>
        /// <returns></returns>
        public static IEnumerable<AuditLog> GetHistory<T, TC>(this TC context, DateTime from, DateTime to, EntityKey entityKey)
            where T : class
            where TC : DbContext, IAuditDbContext
        {
            if (entityKey == null)
            {
                throw new ArgumentNullException("entityKey");
            }

            var key = entityKey.GetEntityString();
            var entityType = typeof(T);
            return context.AuditLogs.Where(l =>
                l.EntityFullName.Equals(entityType.FullName, StringComparison.InvariantCultureIgnoreCase) &&
                l.Created >= from && l.Created <= to && l.EntityId == key).OrderBy(l => l.Created).ToList();
        }
    }
}