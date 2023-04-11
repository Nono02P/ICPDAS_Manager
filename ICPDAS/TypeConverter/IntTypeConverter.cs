namespace ICPDAS_Manager
{
    internal class IntTypeConverter : ITypeConverter
    {
        public object? Convert(string? data)
        {
            int.TryParse(data, out int result);
            return result;
        }
    }
}