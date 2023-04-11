using System.Net.NetworkInformation;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ICPDAS_Manager
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            eCommand command = eCommand.None;
            do
            {
                Console.WriteLine("Read from the gateway or Write to the gateway ? (R/W)");
                ConsoleKeyInfo key = Console.ReadKey();
                Console.WriteLine();
                switch (key.Key)
                {
                    case ConsoleKey.R:
                        command = eCommand.Read;
                        break;
                    case ConsoleKey.W:
                        command = eCommand.Write;
                        break;
                    default:
                        Console.WriteLine("Please enter a valid command !");
                        break;
                }
            } while (command == eCommand.None);

            string fileName = string.Empty;
            FileDialog? fileDialog;
            switch (command)
            {
                case eCommand.Read:
                    fileDialog = new SaveFileDialog();
                    break;
                case eCommand.Write:
                    fileDialog = new OpenFileDialog();
                    break;
                default:
                    throw new Exception("Invalid command");
            }
            fileDialog.Filter = "PDS config file|*.pds";
            DialogResult result = fileDialog.ShowDialog();
            if (DialogResult.OK == result)
            {
                fileName = fileDialog.FileName;
            }
            else
                return;

            bool accepted = false;
            string? ip;
            string ipPattern = @"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$";
            do
            {
                Console.WriteLine("Please enter the gateway IP :");
                ip = Console.ReadLine();
                if (ip == null || !Regex.IsMatch(ip, ipPattern))
                {
                    Console.WriteLine("Invalid IP!");
                }
                else
                {
                    PhysicalAddress r = GetMacAddress(IPAddress.Parse(ip));
                    if (r.ToString().Substring(0, 6) == "000DE0")
                    {
                        accepted = true;
                    }
                    else
                    {
                        Console.WriteLine("The MAC address of the specified IP doesn't correspond to a ICP DAS Gateway!");
                        Console.WriteLine();
                        Console.WriteLine("Do you want to force and continue anyway? (Y/N)");
                        if (Console.ReadKey().Key == ConsoleKey.Y)
                        {
                            Console.WriteLine();
                            Console.WriteLine($"The file {fileName} will be probably wrong. Are you sure? (Y/N)");
                            if (Console.ReadKey().Key == ConsoleKey.Y)
                                accepted = true;
                        }
                        Console.WriteLine();
                    }
                }
            } while (!accepted);

            Task.Run(() =>
            {
                switch (command)
                {
                    case eCommand.None:
                        break;
                    case eCommand.Read:
                        Read(fileName, ip);
                        Console.WriteLine("Reading configuration end");
                        break;
                    case eCommand.Write:
                        Write(fileName, ip);
                        Console.WriteLine("Writing configuration end");
                        break;
                    default:
                        break;
                }
            }).Wait();
        }

        [System.Runtime.InteropServices.DllImport("iphlpapi.dll", ExactSpelling = true)]
        static extern int SendARP(int DestIP, int SrcIP, byte[] pMacAddr, ref int PhyAddrLen);
        public static PhysicalAddress GetMacAddress(IPAddress ipAddress)
        {
            const int MacAddressLength = 6;
            int length = MacAddressLength;
            var macBytes = new byte[MacAddressLength];
            SendARP(BitConverter.ToInt32(ipAddress.GetAddressBytes(), 0), 0, macBytes, ref length);
            return new PhysicalAddress(macBytes);
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