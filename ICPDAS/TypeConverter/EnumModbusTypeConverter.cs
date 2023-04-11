namespace ICPDAS_Manager
{
    internal class EnumModbusTypeConverter : ITypeConverter
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
                    return null;
            }
        }
    }
}