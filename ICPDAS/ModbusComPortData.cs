namespace ICPDAS_Manager
{
    public class ModbusComPortData
    {
        [IcpDasHttpCommand(Key = "COM")]
        public int ComPortId { get; set; }
        
        [IcpDasHttpCommand(Key = "IDNUM")]
        public int NbOfId { get; set; }
        
        [IcpDasHttpCommand(Key = "ID_OFF")]
        public int IdOffset { get; set; }
        
        [IcpDasHttpCommand(Key = "TIMEOUT")]
        public int TimeOut { get; set; }

        [IcpDasHttpCommand(Key = "TYPE")]
        public eModbusType ModbusType { get; set; }
    }
}