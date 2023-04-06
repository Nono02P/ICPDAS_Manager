using System.Reflection;
using Telnet;

namespace ICPDAS_Manager
{
    /// <summary>
    /// Provides a way to connect on the ICP DAS PDS and read / write a configuration.
    /// </summary>
    public class IcpDasPdsConnection : IDisposable
    {
        private TelnetTerminal _telnetTerminal;
        private string _hostname;

        public IcpDasPdsConnection(string hostname)
        {
            _telnetTerminal = new TelnetTerminal(hostname);
            _hostname = hostname;
        }

        /// <summary>
        /// Read the configuration from the connected device.
        /// </summary>
        /// <returns></returns>
        public IcpDasPdsData ReadConfiguration()
        {
            IcpDasPdsData result = new IcpDasPdsData();
            PropertyInfo[] properties = typeof(IcpDasPdsData).GetProperties();
            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo p = properties[i];
                IcpDasCommandAttribute? a = p.GetCustomAttribute<IcpDasCommandAttribute>();
                if (a != null)
                {
                    if (a.CanRead)
                    {
                        p.SetValue(result, ReadProperty(p.Name));
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Write a configuration to the connected device.
        /// </summary>
        /// <param name="config">Configuration to write.</param>
        /// <param name="restart">Should restart the device or not.</param>
        public void WriteConfiguration(IcpDasPdsData config, bool restart = true)
        {
            PropertyInfo[] properties = typeof(IcpDasPdsData).GetProperties();
            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo p = properties[i];
                IcpDasCommandAttribute? a = p.GetCustomAttribute<IcpDasCommandAttribute>();
                if (a != null)
                {
                    if (a.CanWrite)
                    {
                        string? value = (string?)p.GetValue(config);
                        if (value != null)
                        {
                            string[] toWrite = new string[] { value, "" };

                            if (a.HasToSplitByLine)
                            {
                                toWrite = value.Split("\\r\\n");
                            }
                            for (int j = 0; j < toWrite.Length - 1; j++)
                            {
                                string s = toWrite[j];
                                _telnetTerminal.Write(s);
                            }
                        }
                    }
                }
            }

            Console.WriteLine(_telnetTerminal.Read());

            if (restart)
            {
                _telnetTerminal.Write("RESET");
                if (config.Ip.Split("=")[1] != _hostname)
                {
                    
                }
            }
        }

        private string? ReadProperty(string propertyName)
        {
            IcpDasCommandAttribute? a;
            a = typeof(IcpDasPdsData).GetProperty(propertyName)?.GetCustomAttribute<IcpDasCommandAttribute>();
            if (a != null)
            {
                _telnetTerminal.Write(a.Command);
                return _telnetTerminal.Read().Replace(a.Command + "\r\n", "");
            }
            else
            {
                return null;
            }
        }

        public void Dispose()
        {
            _telnetTerminal.Dispose();
        }
    }
}