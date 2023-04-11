namespace ICPDAS_Manager
{
    [AttributeUsage(AttributeTargets.Property)]
    internal class IcpDasHttpCommandAttribute : Attribute
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string Key { get; set; }
        public string RegexID { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string Converter { get; set; } = nameof(IntTypeConverter);
    }
}