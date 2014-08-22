using System;

namespace EF.Audit
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class NotAuditableAttribute : Attribute
    {
    }
}