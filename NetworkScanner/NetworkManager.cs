using NetTools;
using NetworkScanner.Entities;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace NetworkScanner
{
    public class NetworkManager
    {
        public static IEnumerable<NetworkInterface> GetAllNetworkInterfaces()
        {
            return NetworkInterface.GetAllNetworkInterfaces().Where(iface => iface.OperationalStatus == OperationalStatus.Up 
            && iface.Supports(NetworkInterfaceComponent.IPv4) &&
            iface.NetworkInterfaceType != NetworkInterfaceType.Loopback);
        }

        public static IEnumerable<NetworkInterface> GetNetworkInterfacesByFilter(string? interfaceAddress = null, string? macAddress = null)
        {
            return GetAllNetworkInterfaces().Where(iface =>
            {
                if (!string.IsNullOrEmpty(interfaceAddress))
                {
                    foreach (var ip in iface.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.ToString() == interfaceAddress) return true;
                    }
                }

                if(!string.IsNullOrEmpty(macAddress))
                {
                    if (iface.GetPhysicalAddress().ToString() == macAddress) return true;
                }

                return false;
                
            });
        }

        public static async Task<IEnumerable<AvailableDevice>> GetAvailableDevices(IEnumerable<NetworkInterface> networkInterfaces, int? port = null)
        {
            List<AvailableDevice> availableDevices = new();
            foreach (var iface in networkInterfaces)
            {
                foreach (var ip in iface.GetIPProperties().UnicastAddresses)
                {
                    List<IPAddress> ipAddresses = GetIpRange(ip);

                    ParallelOptions parallelOptions = new()
                    {
                        MaxDegreeOfParallelism = 500
                    };

                    await Parallel.ForEachAsync(ipAddresses, parallelOptions, async (ipAddress, token) =>
                    {
                        if (token.IsCancellationRequested) return;

                        try
                        {
                            IPStatus status = await CheckPingStatus(ipAddress);

                            if (status == IPStatus.Success)
                            {
                                if(!port.HasValue)
                                {
                                    AddDeviceToList(ipAddress, availableDevices);
                                }
                                else
                                {
                                    bool isPortAvailable = await IsPortAvailable(ipAddress.ToString(), (int)port);

                                    if(isPortAvailable)
                                    {
                                        AddDeviceToList(ipAddress, availableDevices);
                                    }
                                }

                            }
                        }
                        catch
                        {
                            throw;
                        }
                    });
                }
            }

            return availableDevices;
        }

        private static void AddDeviceToList(IPAddress ipAddress, List<AvailableDevice> availableDevices)
        {
            availableDevices.Add(new AvailableDevice()
            {
                IpAddress = ipAddress.ToString(),
                MacAddress = GetMacAddress(ipAddress.ToString())
            });
        }

        private static async Task<IPStatus> CheckPingStatus(IPAddress ipAddress)
        {
            Ping ping = new();
            IPStatus status = (await ping.SendPingAsync(ipAddress.ToString(), 1000)).Status;
            return status;
        }

        private static List<IPAddress> GetIpRange(UnicastIPAddressInformation ip)
        {
            List<IPAddress> ipAddresses = new();

            var ipRange = IPAddressRange.Parse(ip.Address.ToString() + "/" + ip.PrefixLength);

            foreach (var ipAddress in ipRange)
            {
                ipAddresses.Add(ipAddress);
            }

            return ipAddresses;
        }

        private static string GetMacAddress(string ipAddress)
        {
            SendArpCommand(ipAddress, out string strOutput);
            return ParseMacCommandResponse(strOutput);
        }

        public static string ParseMacCommandResponse(string strOutput)
        {
            string[] substrings = strOutput.Split('-');
            if (substrings.Length >= 8)
            {
                string macAddress = substrings[3].Substring(Math.Max(0, substrings[3].Length - 2))
                         + "-" + substrings[4] + "-" + substrings[5] + "-" + substrings[6]
                         + "-" + substrings[7] + "-"
                         + substrings[8].Substring(0, 2);
                return macAddress;
            }

            else
            {
                return "not found";
            }
        }

        public static void SendArpCommand(string ipAddress, out string strOutput)
        {
            Process process = new();
            string arpCommand = "-a " + ipAddress;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                arpCommand = "-n " + ipAddress;
            }

            process.StartInfo.FileName = "arp";
            process.StartInfo.Arguments = arpCommand;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            strOutput = process.StandardOutput.ReadToEnd();
        }

        private static async Task<bool> IsPortAvailable(string ipAddress, int port)
        {
            using TcpClient tcpClient = new();
            try
            {
                await tcpClient.ConnectAsync(ipAddress, port);
                tcpClient.Close();

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
