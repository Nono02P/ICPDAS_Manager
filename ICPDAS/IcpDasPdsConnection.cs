using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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
            HttpWebResponse response = WebRequests.GetAsync($"http://{_hostname}/modbus_M.cgi?ID=25623").Result;
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Stream stream = response.GetResponseStream();
                string body = new StreamReader(stream).ReadToEnd();
                TypeConverterFactory factory = new TypeConverterFactory();

                string pattern = @"Gateway ID=(?<gId>\d).*<br>TCP\/UDP port=(?<gPort>[^<]*)";
                IQueryable<Group> groups = Regex.Match(body, pattern).Groups.AsQueryable<Group>();
                PropertyInfo[] properties = typeof(IcpDasPdsData).GetProperties();
                InsertPropertyData(data, groups, properties, factory);

                pattern = @"<td>COM (?<COM>\w+): #ID=(?<NBID>\d+):Range=(?<range>\d+).*?timeout=(?<timeout>\d+).*?type=(?<type>\w+),\s+ID offset=(?<offset>-?\d+)|COM (?<COM2>\d+):\s#ID=(?<NBID2>0):Disable";
                MatchCollection matchs = Regex.Matches(body, pattern, RegexOptions.Multiline | RegexOptions.Compiled);
                data.ModbusComPort = new ModbusComPortData[matchs.Count];
                for (int i = 0; i < matchs.Count; i++)
                {
                    data.ModbusComPort[i] = GetModbusComPortData(matchs[i], factory);
                }
            }
        }

        /// <summary>
        /// Insert the Modbus com port data from a regex match containing the data.
        /// </summary>
        /// <param name="data">The data class that should be populate with the extracted data from the regex match.</param>
        /// <param name="match">The regex match contening the data.</param>
        private ModbusComPortData GetModbusComPortData(Match match, TypeConverterFactory factory)
        {
            ModbusComPortData data = new ModbusComPortData();
            IQueryable<Group> groups = match.Groups.AsQueryable<Group>();
            PropertyInfo[] properties = typeof(ModbusComPortData).GetProperties();
            InsertPropertyData(data, groups, properties, factory);
            return data;
        }

        /// <summary>
        /// For each properties, get the value from the regex groups and write the value in data object.
        /// </summary>
        /// <param name="data">The object to be written.</param>
        /// <param name="groups">The Regex groups (with names) containing the data.</param>
        /// <param name="properties">The properties of data class to be checked.</param>
        /// <param name="factory">A factory to convert a string from the regex extraction.</param>
        private void InsertPropertyData(object data, IQueryable<Group> groups, PropertyInfo[] properties, TypeConverterFactory factory)
        {
            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo p = properties[i];
                IcpDasHttpCommandAttribute? a = p.GetCustomAttribute<IcpDasHttpCommandAttribute>();
                if (a != null)
                {
                    ITypeConverter valueConverter = factory.GetInstance(a.Converter);
                    string? rawValue = groups.FirstOrDefault(g => g.Length > 0 && g.Name.Substring(0, Math.Min(g.Name.Length, a.RegexID.Length)) == a.RegexID)?.Value;
                    object? value = valueConverter.Convert(rawValue);
                    if (value != null)
                        p.SetValue(data, value);
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