namespace ICPDAS_Manager
{
    public class TypeConverterFactory : IFactory<ITypeConverter>
    {
        public ITypeConverter GetInstance(string name)
        {
            switch (name)
            {
                case nameof(IntTypeConverter):
                    return new IntTypeConverter();
                case nameof(EnumModbusTypeConverter):
                    return new EnumModbusTypeConverter();
                default:
                    throw new Exception($"Impossible to instanciate the TypeConverter {name}");
            }
        }
    }
}