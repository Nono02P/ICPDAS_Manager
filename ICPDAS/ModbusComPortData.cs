namespace ICPDAS_Manager
{
    public class ModbusComPortData
    {
        [IcpDasHttpCommand(Key = "COM", RegexID = "COM")]
        public int ComPortId { get; set; }

        [IcpDasHttpCommand(Key = "IDNUM", RegexID = "NBID")]
        public int NbOfId { get; set; }

        [IcpDasHttpCommand(Key = "ID_OFF", RegexID = "offset")]
        public int IdOffset { get; set; }

        [IcpDasHttpCommand(Key = "TIMEOUT", RegexID = "timeout")]
        public int TimeOut { get; set; }

        [IcpDasHttpCommand(Key = "TYPE", RegexID = "type", Converter = nameof(EnumModbusTypeConverter))]
        public eModbusType ModbusType { get; set; }
    }
}