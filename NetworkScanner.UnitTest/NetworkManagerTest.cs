namespace NetworkScanner.UnitTest
{
    public class NetworkManagerTest
    {
        [Fact]
        public void ParseMacCommandResponse_ShouldReturnsMacAddress()
        {
            string commandResponse = "\r\nInterface: 192.168.1.78 --- 0x10\r\n  Internet Address      Physical Address      Type\r\n  192.168.1.1           6c-e8-73-77-1b-b0     dynamic   \r\n";
            string expected = "6c-e8-73-77-1b-b0";

            string actual = NetworkManager.ParseMacCommandResponse(commandResponse);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ParseMacCommandResponse_ShouldReturnsNotFound()
        {
            string commandResponse = "Arp is not exists.";
            string expected = "not found";

            string actual = NetworkManager.ParseMacCommandResponse(commandResponse);

            Assert.Equal(expected, actual);
        }
    }
}