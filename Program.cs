using System.Text.Json;

namespace ICPDAS_Manager
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //Read("Configuration.json", "192.168.1.51");
            Write("Configuration.json", "192.168.1.51");
            Console.WriteLine("End");
        }

        static void Read(string fileName, string hostname)
        {
            IcpDasPdsConnection pdsConnection = new IcpDasPdsConnection(hostname);
            try
            {
                IcpDasPdsData config = pdsConnection.ReadConfiguration();
                JsonSerializerOptions options = new JsonSerializerOptions()
                {
                    WriteIndented = true,
                };
                string jsonString = JsonSerializer.Serialize(config, options);
                File.WriteAllText(fileName, jsonString);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                pdsConnection.Dispose();
            }
        }

        static void Write(string fileName, string hostname)
        {
            string jsonString = File.ReadAllText(fileName);
            IcpDasPdsData? config = JsonSerializer.Deserialize<IcpDasPdsData>(jsonString);

            if (config != null)
            {
                IcpDasPdsConnection pdsConnection = new IcpDasPdsConnection(hostname);
                try
                {
                    pdsConnection.WriteConfiguration(config);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    pdsConnection.Dispose();
                }
            }
        }
    }
}