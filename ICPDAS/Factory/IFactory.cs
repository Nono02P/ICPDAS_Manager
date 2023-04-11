namespace ICPDAS_Manager
{
    internal interface IFactory<T>
    {
        T GetInstance(string name);
    }
}