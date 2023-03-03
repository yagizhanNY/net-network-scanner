using NetworkScanner;

// Get all network interfaces by filter
//var ifaces = NetworkManager.GetNetworkInterfacesByFilter(null, "2C8DB19E1BA5");

// Get all network interfaces
var ifaces = NetworkManager.GetAllNetworkInterfaces();

// Find all available devices
var availableDevices = await NetworkManager.GetAvailableDevices(ifaces);

Console.WriteLine(availableDevices);