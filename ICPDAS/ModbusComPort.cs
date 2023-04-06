namespace ICPDAS_Manager
{
    public class ModbusComPort
    {
        public int ComPortId { get; set; }
        public int NbOfId { get; set; }
        public int IdOffset { get; set; }
        public int TimeOut { get; set; }
        public eModbusType ModbusType { get; set; }
    }
}