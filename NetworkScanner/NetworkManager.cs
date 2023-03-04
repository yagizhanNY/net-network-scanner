﻿using NetTools;
using NetworkScanner.Entities;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
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
                                    IpAddress = ipAddress.ToString(),
                                    MacAddress = GetMacAddress(ipAddress.ToString())
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

        private static string GetMacAddress(string ipAddress)
        {
            using Process process = new();

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
            string strOutput = process.StandardOutput.ReadToEnd();
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
    }
}