﻿using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ICPDAS_Manager
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            eCommand command = RequestCommand();
            string? fileName = RequestFileName(command);
            if (fileName == null)
                return;

            string ip = RequestIp(fileName);

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
            Console.WriteLine("Press any key to close...");
            Console.ReadKey();
        }

        #region Request to user
        private static eCommand RequestCommand()
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
            return command;
        }

        private static string? RequestFileName(eCommand command)
        {
            string? fileName = null;
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
            return fileName;
        }

        private static string RequestIp(string fileName)
        {
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
                    string mac = NetworkHelper.GetMacAddress(IPAddress.Parse(ip)).ToString();
                    if (mac.Substring(0, 6) == "000DE0")
                    {
                        accepted = true;
                    }
                    else
                    {
                        if (mac == new string('0', 12))
                        {
                            Console.WriteLine($"There is no device at the IP {ip}!");
                        }
                        else
                        {
                            string pattern = "^([0-9A-F]{2})([0-9A-F]{2})([0-9A-F]{2})([0-9A-F]{2})([0-9A-F]{2})([0-9A-F]{2})$";
                            string replacement = "$1-$2-$3-$4-$5-$6";
                            string macFormated = Regex.Replace(mac, pattern, replacement);
                            Console.WriteLine($"The MAC address ({macFormated}) of the specified IP doesn't correspond to a ICP DAS Gateway!");
                            Console.WriteLine();
                            Console.WriteLine("Do you want to force and continue anyway? (Y/N)");
                            if (Console.ReadKey().Key == ConsoleKey.Y)
                            {
                                Console.WriteLine();
                                Console.WriteLine($"The file {fileName} will be probably wrong. Are you sure? (Y/N)");
                                if (Console.ReadKey().Key == ConsoleKey.Y)
                                    accepted = true;
                            }
                        }
                        Console.WriteLine();
                    }
                }
            } while (!accepted);
            return ip;
        } 
        #endregion

        #region Read / Write
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
        #endregion
    }
}