namespace ICPDAS_Manager
{
    [AttributeUsage(AttributeTargets.Property)]
    public class IcpDasHttpCommandAttribute : Attribute
    {
        public string Key { get; set; }
        public string RegexID { get; set; }
        public string Converter { get; set; } = nameof(IntTypeConverter);
    }
}