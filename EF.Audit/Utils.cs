using System;
using System.Data.Entity.Infrastructure;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

namespace EF.Audit
{
    public static class Utils
    {
        #region ReflectionExt

        public static bool IsAttr<T>(this PropertyInfo entry) where T : Attribute
        {
            return entry.CustomAttributes.Any(q => q.AttributeType == typeof(T));
        }

        public static bool IsAttr<T>(this DbEntityEntry entry) where T : Attribute
        {
            return entry.Entity.GetType().CustomAttributes.Any(q => q.AttributeType == typeof(T));
        }

        public static byte[] Serialize<T>(T entity) where T : class
        {
            var bf = new BinaryFormatter();

            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, entity);
                return ms.GetBuffer();
            }

        }

        public static T Deserialize<T>(byte[] arrBytes) where T : class
        {
            var bf = new BinaryFormatter();
            T result;

            using (var ms = new MemoryStream())
            {
                ms.Write(arrBytes, 0, arrBytes.Length);
                ms.Position = 0;
                result = bf.Deserialize(ms) as T;
            }
            return result;
        }

        #endregion
    }
}