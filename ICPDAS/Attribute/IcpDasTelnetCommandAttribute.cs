namespace ICPDAS_Manager
{
    [AttributeUsage(AttributeTargets.Property)]
    internal class IcpDasTelnetCommandAttribute : Attribute
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string Command { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public bool CanRead { get; set; }
        public bool CanWrite { get; set; }
        public bool HasToSplitByLine { get; set; }
    }
}