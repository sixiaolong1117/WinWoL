using System;
using System.Linq;
using System.Net;
using Xunit;
using WinWoL.Methods;

namespace WinWoL.Tests
{
    public class WoLMethodTests
    {
        [Theory]
        [InlineData("192.168.1.1", "IPv4")]
        [InlineData("10.0.0.1", "IPv4")]
        [InlineData("::1", "IPv6")]
        [InlineData("2001:db8::1", "IPv6")]
        public void DomainToIp_ParsesValidIP(string ip, string version)
        {
            var result = WoLMethod.DomainToIp(ip, version);
            Assert.Equal(IPAddress.Parse(ip), result);
        }

        [Theory]
        [InlineData("192.168.1.1", "IPv6")]
        [InlineData("::1", "IPv4")]
        public void DomainToIp_ThrowsOnVersionMismatch(string ip, string version)
        {
            Assert.Throws<ArgumentException>(() => WoLMethod.DomainToIp(ip, version));
        }

        [Fact]
        public void DomainToIp_ThrowsOnInvalidDomain()
        {
            Assert.ThrowsAny<Exception>(() => WoLMethod.DomainToIp("not-a-domain-or-ip-at-all", "IPv4"));
        }

        [Fact]
        public void BuildMagicPacket_StartsWithSixBytesOfFF()
        {
            byte[] packet = WoLMethod.BuildMagicPacket("AA:BB:CC:DD:EE:FF");
            for (int i = 0; i < 6; i++)
                Assert.Equal(0xFF, packet[i]);
        }

        [Fact]
        public void BuildMagicPacket_HasCorrectTotalLength()
        {
            byte[] packet = WoLMethod.BuildMagicPacket("AA:BB:CC:DD:EE:FF");
            Assert.Equal(17 * 6, packet.Length);
        }

        [Fact]
        public void BuildMagicPacket_ContainsMacAddress16Times()
        {
            byte[] expectedMac = new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66 };
            byte[] packet = WoLMethod.BuildMagicPacket("11:22:33:44:55:66");

            for (int i = 0; i < 16; i++)
            {
                int offset = 6 + i * 6;
                for (int j = 0; j < 6; j++)
                    Assert.Equal(expectedMac[j], packet[offset + j]);
            }
        }

        [Fact]
        public void BuildMagicPacket_TwoDifferentMacs_ProduceDifferentPackets()
        {
            byte[] packet1 = WoLMethod.BuildMagicPacket("AA:BB:CC:DD:EE:FF");
            byte[] packet2 = WoLMethod.BuildMagicPacket("11:22:33:44:55:66");

            Assert.False(packet1.SequenceEqual(packet2));
        }
    }
}
