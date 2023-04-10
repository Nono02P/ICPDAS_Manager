using System.Text.RegularExpressions;

namespace ICPDAS_Manager
{
    /// <summary>
    /// Configuration data of ICP DAS PDS
    /// </summary>
    public class IcpDasPdsData
    {
        #region Telnet
        #region Read Only
        [IcpDasTelnetCommand(Command = "VER", CanRead = true)]
        public string? FirmwareVersion { get; set; }

        [IcpDasTelnetCommand(Command = "MAC", CanRead = true)]
        public string? Mac { get; set; }

        [IcpDasTelnetCommand(Command = "IPFILTER", CanRead = true)]
        public string? IpFilter { get; set; }

        [IcpDasTelnetCommand(Command = "SOCKET", CanRead = true)]
        public string? Socket { get; set; }
        #endregion

        #region Read / Write
        private string? _comPorts;
        [IcpDasTelnetCommand(Command = "COM", CanRead = true, CanWrite = true, HasToSplitByLine = true)]
        public string? ComPorts
        {
            get { return _comPorts; }
            set
            {
                if (value == null)
                    _comPorts = value;
                else
                {
                    string pattern = @"\. connect=\d\r\n";
                    _comPorts = Regex.Replace(value, pattern, @"\r\n", RegexOptions.Multiline);
                }
            }
        }

        [IcpDasTelnetCommand(Command = "NAME", CanRead = true, CanWrite = true)]
        public string? Name { get; set; }

        [IcpDasTelnetCommand(Command = "ALIAS", CanRead = true, CanWrite = true)]
        public string? Alias { get; set; }

        [IcpDasTelnetCommand(Command = "IP", CanRead = true, CanWrite = true)]
        public string? Ip { get; set; }

        [IcpDasTelnetCommand(Command = "MASK", CanRead = true, CanWrite = true)]
        public string? Mask { get; set; }

        [IcpDasTelnetCommand(Command = "GATEWAY", CanRead = true, CanWrite = true)]
        public string? Gateway { get; set; }

        [IcpDasTelnetCommand(Command = "DHCP", CanRead = true, CanWrite = true)]
        public string? Dhcp { get; set; }

        [IcpDasTelnetCommand(Command = "M", CanRead = true, CanWrite = true)]
        public string? EchoMode { get; set; }

        [IcpDasTelnetCommand(Command = "UDP", CanRead = true, CanWrite = true)]
        public string? UdpSearch { get; set; }

        [IcpDasTelnetCommand(Command = "Broadcast", CanRead = true, CanWrite = true)]
        public string? Broadcast { get; set; }

        [IcpDasTelnetCommand(Command = "SystemTimeout", CanRead = true, CanWrite = true)]
        public string? SystemTimeout { get; set; }

        [IcpDasTelnetCommand(Command = "SocketTimeout", CanRead = true, CanWrite = true)]
        public string? SocketTimeout { get; set; }

        [IcpDasTelnetCommand(Command = "EndChar", CanRead = true, CanWrite = true)]
        public string? EndChar { get; set; }

        [IcpDasTelnetCommand(Command = "EchoCmdNo", CanRead = true, CanWrite = true)]
        public string? EchoCmdNo { get; set; }
        #endregion
        #endregion

        #region Http
        [IcpDasHttpCommand(Key = "MID", RegexID = "gId")]
        public int GatewayModbusID { get; set; }

        [IcpDasHttpCommand(Key = "M_PORT", RegexID = "gPort")]
        public int ModbusPort { get; set; }

        public ModbusComPortData[] ModbusComPort { get; set; }
        #endregion
    }
}