using System.Reflection;
using System.Linq;
using Telnet;
using Dna;
using System.Net;
using System.Text.RegularExpressions;

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
            InsertHttpData(result);
            return result;
        }

        private void InsertHttpData(IcpDasPdsData data)
        {
            HttpWebResponse response = WebRequests.GetAsync($"http://{_hostname}/modbus_M.cgi?ID=25623").Result;
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Stream stream = response.GetResponseStream();
                string body = new StreamReader(stream).ReadToEnd();

                string pattern = @"Gateway ID=(?<id>\d)";
                Group? g = Regex.Match(body, pattern).Groups.AsQueryable<Group>().FirstOrDefault(m => m.Name == "id");
                if (g != null)
                {
                    data.GatewayModbusID = int.Parse(g.Value);
                }

                pattern = @"<br>TCP\/UDP port=(?<port>[^<]*)";
                g = Regex.Match(body, pattern).Groups.AsQueryable<Group>().FirstOrDefault(m => m.Name == "port");
                if (g != null)
                {
                    data.ModbusPort = int.Parse(g.Value);
                }

                pattern = @"<td>(?<complete>COM (?<com>\d): #ID=(?<nbID>[^:]*)[^<]*)";
                MatchCollection matchs = Regex.Matches(body, pattern, RegexOptions.Multiline);
                data.ModbusComPort = new ModbusComPort[matchs.Count];
                for (int i = 0; i < matchs.Count; i++)
                {
                    Match m = matchs[i];
                    IQueryable<Group> groups = m.Groups.AsQueryable<Group>();
                    string? comPortConfig = groups.FirstOrDefault(gr => gr.Name == "complete").Value;
                    if (comPortConfig != null)
                    {
                        string subpattern = @"timeout=(?<timeout>\d*) ms, type=(?<type>\w*), ID offset=(?<IdOffset>-?\d*)";
                        IQueryable<Group> subGroups = Regex.Match(comPortConfig, subpattern).Groups.AsQueryable<Group>();
                        ModbusComPort modbusComPort = new ModbusComPort()
                        {
                            ComPortId = int.Parse(groups.FirstOrDefault(gr => gr.Name == "com").Value),
                            NbOfId = int.Parse(groups.FirstOrDefault(gr => gr.Name == "nbID").Value)
                        };
                        if (int.TryParse(subGroups.FirstOrDefault(gr => gr.Name == "timeout")?.Value, out int timeout))
                        {
                            modbusComPort.TimeOut = timeout;
                        }
                        if (int.TryParse(subGroups.FirstOrDefault(gr => gr.Name == "IdOffset")?.Value, out int idOffset))
                        {
                            modbusComPort.IdOffset = idOffset;
                        }
                        switch (subGroups.FirstOrDefault(gr => gr.Name == "type")?.Value)
                        {
                            case "ASCII":
                                modbusComPort.ModbusType = eModbusType.ASCII;
                                break;
                            case "RTU":
                                modbusComPort.ModbusType = eModbusType.RTU;
                                break;
                            default:
                                break;
                        }
                        data.ModbusComPort[i] = modbusComPort;
                    }
                }
            }
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