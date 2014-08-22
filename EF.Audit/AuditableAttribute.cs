using System;

namespace EF.Audit
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class AuditableAttribute: Attribute
    {
    }
}