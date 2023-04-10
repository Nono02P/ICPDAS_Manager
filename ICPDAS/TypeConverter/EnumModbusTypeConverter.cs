namespace ICPDAS_Manager
{
    public class EnumModbusTypeConverter : ITypeConverter
    {
        public object? Convert(string? data)
        {
            switch (data)
            {
                case "ASCII":
                    return eModbusType.ASCII;
                case "RTU":
                    return eModbusType.RTU;
                default:
                    throw new Exception("Unknow eModbusType data.");
            }
        }
    }
}