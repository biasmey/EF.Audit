using System;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Core.Common.CommandTrees;
using System.Data.Entity.Infrastructure.Interception;
using System.Linq;

namespace EF.Audit
{
    public class AuditLogInterceptor : IDbCommandInterceptor
    {
        public void TreeCreated(DbCommandTreeInterceptionContext interceptionContext)
        {
            var dbCommandTreeKind = interceptionContext.Result.CommandTreeKind;
            var context = interceptionContext.DbContexts.First();
            var auditContenxt = new LogContext();

            switch (dbCommandTreeKind)
            {
                case DbCommandTreeKind.Insert:
                    var addedEntries = context.ChangeTracker.Entries()
                        .Where(e => e.State == EntityState.Added && e.IsAttr<AuditableAttribute>())
                        .ToList();

                    foreach (var entry in addedEntries)
                    {
                        auditContenxt.ApplyAuditLog(context, entry);
                    }
                    break;
                case DbCommandTreeKind.Update:
                case DbCommandTreeKind.Delete:
                    var entries = context.ChangeTracker.Entries().Where(
                        e => (e.State == EntityState.Deleted || e.State == EntityState.Modified)
                             && e.IsAttr<AuditableAttribute>()).ToList();

                    foreach (var entry in entries)
                    {
                        auditContenxt.ApplyAuditLog(context, entry);
                    }
                    break;
            }
        }

        public void NonQueryExecuted(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        {
        }

        public void NonQueryExecuting(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        {
            // just for update and delete commands
            if (command.CommandText.StartsWith("update", StringComparison.InvariantCultureIgnoreCase) ||
                command.CommandText.StartsWith("delete", StringComparison.InvariantCultureIgnoreCase))
            {
                var context = interceptionContext.DbContexts.First();
                var entries = context.ChangeTracker.Entries().Where(
                    e => e.State == EntityState.Deleted || e.State == EntityState.Modified).ToList();

                var auditContenxt = new LogContext();
                foreach (var entry in entries)
                {
                    auditContenxt.ApplyAuditLog(context, entry);
                }
            }
        }

        public void ReaderExecuting(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
        {
        }

        public void ReaderExecuted(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
        {
            // just for update and delete commands
            if (command.CommandText.StartsWith("insert", StringComparison.InvariantCultureIgnoreCase))
            {
                var context = interceptionContext.DbContexts.First();
                var entries = context.ChangeTracker.Entries().Where(e => e.State == EntityState.Added).ToList();

                var auditContenxt = new LogContext();
                foreach (var entry in entries)
                {
                    auditContenxt.ApplyAuditLog(context, entry);
                }
            }
        }

        public void ScalarExecuting(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        {
            //throw new System.NotImplementedException();
        }

        public void ScalarExecuted(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        {
            //throw new System.NotImplementedException();
        }
    }
}