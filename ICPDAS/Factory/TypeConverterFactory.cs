namespace ICPDAS_Manager
{
    public class TypeConverterFactory : IFactory<ITypeConverter>
    {
        private IntTypeConverter? _intConverter;
        private EnumModbusTypeConverter? _enumConverter;

        public ITypeConverter GetInstance(string name)
        {
            switch (name)
            {
                case nameof(IntTypeConverter):
                    if (_intConverter == null)
                    {
                        _intConverter = new IntTypeConverter();
                    }
                    return _intConverter;
                case nameof(EnumModbusTypeConverter):
                    if (_enumConverter == null)
                    {
                        _enumConverter = new EnumModbusTypeConverter();
                    }
                    return _enumConverter;
                default:
                    throw new Exception($"Impossible to instanciate the TypeConverter {name}");
            }
        }
    }
}