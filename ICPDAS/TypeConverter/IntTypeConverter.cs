namespace ICPDAS_Manager
{
    public class IntTypeConverter : ITypeConverter
    {
        public object? Convert(string? data)
        {
            int.TryParse(data, out int result);
            return result;
        }
    }
}