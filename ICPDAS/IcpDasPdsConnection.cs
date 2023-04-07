using System.Reflection;
using System.Linq;
using Telnet;
using Dna;
using System.Net;
using System.Text.RegularExpressions;
using System.Text;
using Newtonsoft.Json;
using System.Xml.Serialization;

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

        #region Read configuration from device
        /// <summary>
        /// Read the configuration from the connected device.
        /// </summary>
        /// <returns></returns>
        public IcpDasPdsData ReadConfiguration()
        {
            IcpDasPdsData result = new IcpDasPdsData();
            InsertDataFromTelnet(result);
            InsertDataFromHttp(result);
            return result;
        }

        #region Telnet
        /// <summary>
        /// Get the telnet data and insert it in the <see cref="IcpDasPdsData"/> passed in parameter.
        /// </summary>
        /// <param name="data">The data class that should be populate with Telnet data.</param>
        private void InsertDataFromTelnet(IcpDasPdsData data)
        {
            PropertyInfo[] properties = typeof(IcpDasPdsData).GetProperties();
            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo p = properties[i];
                IcpDasTelnetCommandAttribute? a = p.GetCustomAttribute<IcpDasTelnetCommandAttribute>();
                if (a != null)
                {
                    if (a.CanRead)
                    {
                        p.SetValue(data, ReadTelnetProperty(p.Name));
                    }
                }
            }
        }

        /// <summary>
        /// Read the specified property by using Telnet.
        /// </summary>
        /// <param name="propertyName">The property to read</param>
        /// <returns>The property stored in the device.</returns>
        private string? ReadTelnetProperty(string propertyName)
        {
            IcpDasTelnetCommandAttribute? a = typeof(IcpDasPdsData).GetProperty(propertyName)?.GetCustomAttribute<IcpDasTelnetCommandAttribute>();
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
        #endregion

        #region HTTP
        /// <summary>
        /// Get the HTTP data and insert it in the <see cref="IcpDasPdsData"/> passed in parameter.
        /// </summary>
        /// <param name="data">The data class that should be populate with HTTP data.</param>
        private void InsertDataFromHttp(IcpDasPdsData data)
        {
            HttpWebResponse response = Dna.WebRequests.GetAsync($"http://{_hostname}/modbus_M.cgi?ID=25623").Result;
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
                data.ModbusComPort = new ModbusComPortData[matchs.Count];
                for (int i = 0; i < matchs.Count; i++)
                {
                    Match m = matchs[i];
                    IQueryable<Group> groups = m.Groups.AsQueryable<Group>();
                    string? comPortConfig = groups?.FirstOrDefault(gr => gr.Name == "complete")?.Value;
                    if (comPortConfig != null)
                    {
                        string subpattern = @"timeout=(?<timeout>\d*) ms, type=(?<type>\w*), ID offset=(?<IdOffset>-?\d*)";
                        IQueryable<Group> subGroups = Regex.Match(comPortConfig, subpattern).Groups.AsQueryable<Group>();
                        ModbusComPortData modbusComPort = new ModbusComPortData();
                        if (int.TryParse(subGroups.FirstOrDefault(gr => gr.Name == "com")?.Value, out int comPortId))
                        {
                            modbusComPort.ComPortId = comPortId;
                        }
                        if (int.TryParse(subGroups.FirstOrDefault(gr => gr.Name == "nbID")?.Value, out int nbID))
                        {
                            modbusComPort.NbOfId = nbID;
                        }
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
        #endregion 
        #endregion

        #region Write configuration to device
        /// <summary>
        /// Write a configuration to the connected device.
        /// </summary>
        /// <param name="config">Configuration to write.</param>
        /// <param name="restart">Should restart the device or not.</param>
        public void WriteConfiguration(IcpDasPdsData config, bool restart = true)
        {
            WriteDataToTelnet(config);
            WriteDataToHttp(config);

            if (restart)
            {
                _telnetTerminal.Write("RESET");
            }
        }

        #region Telnet
        /// <summary>
        /// Write the configuration to the device by using Telnet.
        /// </summary>
        /// <param name="config">The configuration to write.</param>
        private void WriteDataToTelnet(IcpDasPdsData config)
        {
            PropertyInfo[] properties = typeof(IcpDasPdsData).GetProperties();
            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo property = properties[i];
                IcpDasTelnetCommandAttribute? attribute = property.GetCustomAttribute<IcpDasTelnetCommandAttribute>();
                if (attribute != null && attribute.CanWrite)
                {
                    string? value = (string?)property.GetValue(config);
                    if (value != null)
                    {
                        string[] toWrite = new string[] { value, "" };

                        if (attribute.HasToSplitByLine)
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
            Console.WriteLine(_telnetTerminal.Read());
        }
        #endregion

        #region HTTP
        /// <summary>
        /// Write the configuration to the device by using HTTP.
        /// </summary>
        /// <param name="config">The configuration to write.</param>
        private void WriteDataToHttp(IcpDasPdsData config)
        {
            string url = $"http://{_hostname}/modbus_M.cgi";
            string contentType = "application/x-www-form-urlencoded";
            string accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7";
            string referer = $"{url}?ID=11661";

            for (int i = 0; i < config.ModbusComPort.Length; i++)
            {
                StringBuilder sb = new StringBuilder();
                ModbusComPortData modbusPort = config.ModbusComPort[i];
                string? key;

                key = GetHttpKeyCommand<IcpDasPdsData>(nameof(IcpDasPdsData.GatewayModbusID));
                InsertKeyValue(sb, key, config.GatewayModbusID);
                
                key = GetHttpKeyCommand<ModbusComPortData>(nameof(ModbusComPortData.ComPortId));
                InsertKeyValue(sb, key, modbusPort.ComPortId);
                
                key = GetHttpKeyCommand<ModbusComPortData>(nameof(ModbusComPortData.NbOfId));
                InsertKeyValue(sb, key, modbusPort.NbOfId);
                
                key = GetHttpKeyCommand<ModbusComPortData>(nameof(ModbusComPortData.IdOffset));
                InsertKeyValue(sb, key, modbusPort.IdOffset);
                
                key = GetHttpKeyCommand<ModbusComPortData>(nameof(ModbusComPortData.TimeOut));
                InsertKeyValue(sb, key, modbusPort.TimeOut);
                
                key = GetHttpKeyCommand<ModbusComPortData>(nameof(ModbusComPortData.ModbusType));
                InsertKeyValue(sb, key, (int)modbusPort.ModbusType);

                key = GetHttpKeyCommand<IcpDasPdsData>(nameof(IcpDasPdsData.ModbusPort));
                InsertKeyValue(sb, key, config.ModbusPort);

                InsertKeyValue(sb, "SAVE", 1);
                InsertKeyValue(sb, "APPLY", 1);

                WebRequests.PostAsync(url, contentType, accept, sb.ToString(0, sb.Length - 1), referer).Wait();
            }
        }

        private void InsertKeyValue(StringBuilder sb, string? key, int value)
        {
            if (key != null)
            {
                sb.Append($"{key}={value}&");
            }
        }

        private string? GetHttpKeyCommand<T>(string property)
        {
            return typeof(T).GetProperty(property)?.GetCustomAttribute<IcpDasHttpCommandAttribute>()?.Key;
        }
        #endregion
        #endregion

        public void Dispose()
        {
            _telnetTerminal.Dispose();
        }
    }
}