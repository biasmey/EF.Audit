namespace EF.Audit
{
    internal class ChangedProperty
    {
        public string Name;
        public object CurrentValue;
        public object OriginalValue;
    }
}