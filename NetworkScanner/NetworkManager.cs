using NetTools;
using NetworkScanner.Entities;
using System.Net;
using System.Net.NetworkInformation;

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

        public static async Task<IEnumerable<AvailableDevice>> GetAvailableDevices(IEnumerable<NetworkInterface> networkInterfaces)
        {
            List<AvailableDevice> availableDevices = new();
            foreach (var iface in networkInterfaces)
            {
                foreach (var ip in iface.GetIPProperties().UnicastAddresses)
                {
                    List<IPAddress> ipAddresses = new();

                    var ipRange = IPAddressRange.Parse(ip.Address.ToString() + "/" + ip.PrefixLength);

                    foreach (var ipAddress in ipRange)
                    {
                        ipAddresses.Add(ipAddress);
                    }

                    ParallelOptions parallelOptions = new()
                    {
                        MaxDegreeOfParallelism = 500
                    };

                    await Parallel.ForEachAsync(ipAddresses, parallelOptions, async (ipAddress, token) =>
                    {
                        if(token.IsCancellationRequested) return;

                        try
                        {
                            Ping ping = new();
                            IPStatus status = (await ping.SendPingAsync(ipAddress.ToString(), 1000)).Status;

                            if(status == IPStatus.Success)
                            {
                                availableDevices.Add(new AvailableDevice()
                                {
                                    IpAddress = ipAddress.ToString()
                                });
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
    }
}
