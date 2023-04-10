namespace ICPDAS_Manager
{
    public interface IFactory<T>
    {
        T GetInstance(string name);
    }
}