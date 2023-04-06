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
        [IcpDasCommand(Command = "VER", CanRead = true)]
        public string? FirmwareVersion { get; set; }

        [IcpDasCommand(Command = "MAC", CanRead = true)]
        public string? Mac { get; set; }

        [IcpDasCommand(Command = "IPFILTER", CanRead = true)]
        public string? IpFilter { get; set; }

        [IcpDasCommand(Command = "SOCKET", CanRead = true)]
        public string? Socket { get; set; }
        #endregion

        #region Read / Write
        private string? _comPorts;
        [IcpDasCommand(Command = "COM", CanRead = true, CanWrite = true, HasToSplitByLine = true)]
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

        [IcpDasCommand(Command = "NAME", CanRead = true, CanWrite = true)]
        public string? Name { get; set; }

        [IcpDasCommand(Command = "ALIAS", CanRead = true, CanWrite = true)]
        public string? Alias { get; set; }

        [IcpDasCommand(Command = "IP", CanRead = true, CanWrite = true)]
        public string? Ip { get; set; }

        [IcpDasCommand(Command = "MASK", CanRead = true, CanWrite = true)]
        public string? Mask { get; set; }

        [IcpDasCommand(Command = "GATEWAY", CanRead = true, CanWrite = true)]
        public string? Gateway { get; set; }

        [IcpDasCommand(Command = "DHCP", CanRead = true, CanWrite = true)]
        public string? Dhcp { get; set; }

        [IcpDasCommand(Command = "M", CanRead = true, CanWrite = true)]
        public string? EchoMode { get; set; }

        [IcpDasCommand(Command = "UDP", CanRead = true, CanWrite = true)]
        public string? UdpSearch { get; set; }

        [IcpDasCommand(Command = "Broadcast", CanRead = true, CanWrite = true)]
        public string? Broadcast { get; set; }

        [IcpDasCommand(Command = "SystemTimeout", CanRead = true, CanWrite = true)]
        public string? SystemTimeout { get; set; }

        [IcpDasCommand(Command = "SocketTimeout", CanRead = true, CanWrite = true)]
        public string? SocketTimeout { get; set; }

        [IcpDasCommand(Command = "EndChar", CanRead = true, CanWrite = true)]
        public string? EndChar { get; set; }

        [IcpDasCommand(Command = "EchoCmdNo", CanRead = true, CanWrite = true)]
        public string? EchoCmdNo { get; set; }
        #endregion
        #endregion

        #region Http
        public int GatewayModbusID { get; set; }

        public int ModbusPort { get; set; }

        public ModbusComPort[] ModbusComPort { get; set; }
        #endregion
    }
}