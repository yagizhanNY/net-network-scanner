## NetworkScanner

Light weight library for scan the local network fastly. 

### Installation

```cmd
dotnet add package NetworkScanner
```

### Usage

```csharp
using NetworkScanner;

// Get all network interfaces by mac address filter
//var ifaces = NetworkManager.GetNetworkInterfacesByFilter(null, "2C8DB19E1BA5");

// Get all network interfaces by ip address filter
//var ifaces = NetworkManager.GetNetworkInterfacesByFilter("192.168.1.10");

// Get all network interfaces
var ifaces = NetworkManager.GetAllNetworkInterfaces();

// Find all available devices
var availableDevices = await NetworkManager.GetAvailableDevices(ifaces);

// Find all available devices with specific TCP port.
var availableDevices = await NetworkManager.GetAvailableDevices(ifaces, 403);
```

### Features

- Scan the network over IPv4.
- Get available devices with specific TCP port.
- Getting MAC addresses of devices from arp table.
- Cross platform support.