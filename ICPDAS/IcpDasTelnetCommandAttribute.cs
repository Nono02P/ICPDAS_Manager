﻿namespace ICPDAS_Manager
{
    [AttributeUsage(AttributeTargets.Property)]
    public class IcpDasTelnetCommandAttribute : Attribute
    {
        public string Command { get; set; }
        public bool CanRead { get; set; }
        public bool CanWrite { get; set; }
        public bool HasToSplitByLine { get; set; }
    }
}