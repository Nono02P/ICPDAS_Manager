namespace ICPDAS_Manager
{
    [AttributeUsage(AttributeTargets.Property)]
    public class IcpDasHttpCommandAttribute : Attribute
    {
        public string Key { get; set; }
    }
}