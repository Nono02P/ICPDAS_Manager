using System.Net.Sockets;
using System.Text;

namespace Telnet
{
    internal class TelnetTerminal : IDisposable
    {
        private TcpClient _client;
        private int _timeOutMs;

        public TelnetTerminal(string hostname, int port = 23, int timeOutMs = 100)
        {
            _client = new TcpClient(hostname, port);
            _timeOutMs = timeOutMs;
            Init();
        }

        private void Init()
        {
            if (_client.Connected)
            {
                byte[] init = new byte[] {
                (byte)eTelnetCommand.IAC, (byte)eTelnetCommand.WILL, (byte)eTelnetOption.NegotiateAboutWindowSize,
                (byte)eTelnetCommand.IAC, (byte)eTelnetCommand.WILL, (byte)eTelnetOption.TerminalSpeed,
                (byte)eTelnetCommand.IAC, (byte)eTelnetCommand.WILL, (byte)eTelnetOption.TerminalType,
                (byte)eTelnetCommand.IAC, (byte)eTelnetCommand.WILL, (byte)eTelnetOption.NewEnvironment,
                (byte)eTelnetCommand.IAC, (byte)eTelnetCommand.DO, (byte)eTelnetOption.Echo,
                (byte)eTelnetCommand.IAC, (byte)eTelnetCommand.WILL, (byte)eTelnetOption.SuppressGoAhead,
                (byte)eTelnetCommand.IAC, (byte)eTelnetCommand.DO, (byte)eTelnetOption.SuppressGoAhead,
                };
                _client.GetStream().Write(init, 0, init.Length);
            }
        }

        public void Write(string message)
        {
            if (!_client.Connected)
            {
                throw new Exception("Client disconnected");
            }

            NetworkStream stream = _client.GetStream();
            byte[] data = Encoding.ASCII.GetBytes(message + "\r\n");
            stream.Write(data, 0, data.Length);
            Thread.Sleep(50);
        }

        public string Read()
        {
            if (!_client.Connected)
            {
                throw new Exception("Client disconnected");
            }

            StringBuilder sb = new StringBuilder();
            do
            {
                ParseTelnet(sb);
                Thread.Sleep(_timeOutMs);
            } while (_client.Available > 0);

            string[] lines = sb.ToString().Split("\r\n");
            string lastLine = lines[lines.Length - 1];
            int length = sb.Length;
            if (lastLine.Length > 0 && lastLine.Substring(lastLine.Length - 1) == ">")
            {
                length -= lastLine.Length;
            }

            return sb.ToString(0, length);
        }

        private void ParseTelnet(StringBuilder sb)
        {
            NetworkStream stream = _client.GetStream();
            while (_client.Available > 0)
            {
                int input = stream.ReadByte();
                switch (input)
                {
                    case -1:
                        break;
                    case (int)eTelnetCommand.IAC:
                        // interpret as command
                        int inputverb = stream.ReadByte();
                        if (inputverb == -1)
                            break;

                        switch (inputverb)
                        {
                            case (int)eTelnetCommand.IAC:
                                //literal IAC = 255 escaped, so append char 255 to string
                                sb.Append(inputverb);
                                break;
                            case (int)eTelnetCommand.DO:
                            case (int)eTelnetCommand.DONT:
                            case (int)eTelnetCommand.WILL:
                            case (int)eTelnetCommand.WONT:
                                // reply to all commands with "WONT", unless it is SGA (suppres go ahead)
                                int inputoption = stream.ReadByte();
                                if (inputoption == -1)
                                {
                                    break;
                                }
                                /*
                                stream.WriteByte((byte)eTelnetCommand.IAC);

                                if (inputoption == (int)eTelnetOption.SuppressGoAhead)
                                {
                                    stream.WriteByte(inputverb == (int)eTelnetCommand.DO ? (byte)eTelnetCommand.WILL : (byte)eTelnetCommand.DO);
                                }
                                else
                                {
                                    stream.WriteByte(inputverb == (int)eTelnetCommand.DO ? (byte)eTelnetCommand.WONT : (byte)eTelnetCommand.DONT);
                                }
                                stream.WriteByte((byte)inputoption);
                                */
                                break;
                            default:
                                break;
                        }
                        break;
                    default:
                        sb.Append((char)input);
                        break;
                }
            }
        }

        public void Connect(string hostname, int port = 23)
        {
            _client.Connect(hostname, port);
            Init();
        }

        public void Close()
        {
            _client.Close();
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}