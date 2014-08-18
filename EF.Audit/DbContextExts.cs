using System.Data.Entity.Core;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;

namespace EF.Audit
{
    public static class DbContextExts
    {
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
    }
}