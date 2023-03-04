## NetworkScanner

Light weight library for scan the local network fastly. 

### Installation

```cmd
dotnet add package NetworkScanner --version 1.0.0
```

### Usage

```csharp
using NetworkScanner;

// Get all network interfaces by filter
//var ifaces = NetworkManager.GetNetworkInterfacesByFilter(null, "2C8DB19E1BA5");

// Get all network interfaces
var ifaces = NetworkManager.GetAllNetworkInterfaces();

// Find all available devices
var availableDevices = await NetworkManager.GetAvailableDevices(ifaces);
```

### Features

- Scan the network over IPv4.
- Getting MAC addresses of devices from arp table.
- Cross platform support.