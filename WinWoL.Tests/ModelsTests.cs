using Xunit;
using Newtonsoft.Json;
using WinWoL.Models;

namespace WinWoL.Tests
{
    public class ModelsTests
    {
        [Fact]
        public void WoLModel_Properties_Roundtrip()
        {
            var model = new WoLModel
            {
                Id = 42,
                Name = "办公电脑",
                MacAddress = "AA:BB:CC:DD:EE:FF",
                IPAddress = "192.168.1.100",
                WoLAddress = "255.255.255.255",
                WoLPort = "9",
                RDPPort = "3389",
                SSHCommand = "whoami",
                SSHPort = "22",
                SSHUser = "admin",
                SSHKeyPath = "/path/to/key",
                SSHKeyId = "1",
                WoLIsOpen = "True",
                RDPIsOpen = "False",
                SSHIsOpen = "False",
                BroadcastIsOpen = "True",
                SSHKeyIsOpen = "False"
            };

            Assert.Equal(42, model.Id);
            Assert.Equal("办公电脑", model.Name);
            Assert.Equal("AA:BB:CC:DD:EE:FF", model.MacAddress);
            Assert.Equal("192.168.1.100", model.IPAddress);
            Assert.Equal("9", model.WoLPort);
            Assert.Equal("3389", model.RDPPort);
            Assert.Equal("whoami", model.SSHCommand);
            Assert.Equal("22", model.SSHPort);
            Assert.Equal("admin", model.SSHUser);
            Assert.Equal("/path/to/key", model.SSHKeyPath);
            Assert.Equal("1", model.SSHKeyId);
            Assert.Equal("True", model.WoLIsOpen);
            Assert.Equal("False", model.RDPIsOpen);
            Assert.Equal("False", model.SSHIsOpen);
            Assert.Equal("True", model.BroadcastIsOpen);
            Assert.Equal("False", model.SSHKeyIsOpen);
        }

        [Fact]
        public void WoLModel_Defaults_AreEmpty()
        {
            var model = new WoLModel();
            Assert.Equal(0, model.Id);
            Assert.Null(model.Name);
            Assert.Null(model.MacAddress);
            Assert.Null(model.IPAddress);
        }

        [Fact]
        public void WoLModel_SerializesAndDeserializes()
        {
            var original = new WoLModel
            {
                Id = 1,
                Name = "Home PC",
                MacAddress = "AA:BB:CC:DD:EE:FF",
                IPAddress = "192.168.1.10",
                WoLAddress = "255.255.255.255",
                WoLPort = "9",
                RDPPort = "3389",
                SSHCommand = "uptime",
                SSHPort = "22",
                SSHUser = "pi",
                SSHKeyPath = null,
                SSHKeyId = null,
                WoLIsOpen = "True",
                RDPIsOpen = "True",
                SSHIsOpen = "True",
                BroadcastIsOpen = "True",
                SSHKeyIsOpen = "False"
            };

            var json = JsonConvert.SerializeObject(original);
            var deserialized = JsonConvert.DeserializeObject<WoLModel>(json);

            Assert.NotNull(deserialized);
            Assert.Equal(original.Id, deserialized.Id);
            Assert.Equal(original.Name, deserialized.Name);
            Assert.Equal(original.MacAddress, deserialized.MacAddress);
            Assert.Equal(original.IPAddress, deserialized.IPAddress);
        }

        [Fact]
        public void SSHModel_Properties_Roundtrip()
        {
            var model = new SSHModel
            {
                Id = 7,
                Name = "VPS Server",
                IPAddress = "10.0.0.1",
                SSHCommand = "top -bn1",
                SSHPort = "2222",
                SSHUser = "root",
                SSHKeyPath = "",
                SSHKeyId = "3",
                SSHKeyIsOpen = "True"
            };

            Assert.Equal(7, model.Id);
            Assert.Equal("VPS Server", model.Name);
            Assert.Equal("10.0.0.1", model.IPAddress);
            Assert.Equal("2222", model.SSHPort);
            Assert.Equal("root", model.SSHUser);
            Assert.Equal("3", model.SSHKeyId);
            Assert.Equal("True", model.SSHKeyIsOpen);
        }

        [Fact]
        public void SSHModel_Defaults_AreNull()
        {
            var model = new SSHModel();
            Assert.Equal(0, model.Id);
            Assert.Null(model.Name);
            Assert.Null(model.IPAddress);
        }

        [Fact]
        public void SSHModel_SerializesAndDeserializes()
        {
            var original = new SSHModel
            {
                Id = 2,
                Name = "Dev Server",
                IPAddress = "192.168.1.200",
                SSHCommand = "df -h",
                SSHPort = "22",
                SSHUser = "dev",
                SSHKeyPath = null,
                SSHKeyId = "1",
                SSHKeyIsOpen = "True"
            };

            var json = JsonConvert.SerializeObject(original);
            var deserialized = JsonConvert.DeserializeObject<SSHModel>(json);

            Assert.NotNull(deserialized);
            Assert.Equal(original.Id, deserialized.Id);
            Assert.Equal(original.Name, deserialized.Name);
            Assert.Equal(original.IPAddress, deserialized.IPAddress);
            Assert.Equal(original.SSHCommand, deserialized.SSHCommand);
            Assert.Equal(original.SSHKeyId, deserialized.SSHKeyId);
        }

        [Fact]
        public void SSHKeyModel_Properties_Roundtrip()
        {
            var model = new SSHKeyModel
            {
                Id = 5,
                Name = "My Key",
                PrivateKey = "-----BEGIN OPENSSH PRIVATE KEY-----\nfake\n-----END OPENSSH PRIVATE KEY-----",
                PublicKey = "ssh-ed25519 AAAAC3...",
                Fingerprint = "SHA256:abc123",
                CreatedAt = "2024-03-15 10:30:00"
            };

            Assert.Equal(5, model.Id);
            Assert.Equal("My Key", model.Name);
            Assert.Contains("BEGIN OPENSSH PRIVATE KEY", model.PrivateKey);
            Assert.Equal("ssh-ed25519 AAAAC3...", model.PublicKey);
            Assert.Equal("SHA256:abc123", model.Fingerprint);
            Assert.Equal("2024-03-15 10:30:00", model.CreatedAt);
        }

        [Fact]
        public void SSHKeyModel_Defaults_AreNull()
        {
            var model = new SSHKeyModel();
            Assert.Equal(0, model.Id);
            Assert.Null(model.Name);
            Assert.Null(model.PrivateKey);
            Assert.Null(model.PublicKey);
        }

        [Fact]
        public void SSHKeyModel_SerializesAndDeserializes()
        {
            var original = new SSHKeyModel
            {
                Id = 3,
                Name = "GitHub Key",
                PrivateKey = "private-content",
                PublicKey = "ssh-rsa AAAAB3NzaC1yc2E...",
                Fingerprint = "SHA256:xyz789",
                CreatedAt = "2024-01-01 00:00:00"
            };

            var json = JsonConvert.SerializeObject(original);
            var deserialized = JsonConvert.DeserializeObject<SSHKeyModel>(json);

            Assert.NotNull(deserialized);
            Assert.Equal(original.Id, deserialized.Id);
            Assert.Equal(original.Name, deserialized.Name);
            Assert.Equal(original.PrivateKey, deserialized.PrivateKey);
            Assert.Equal(original.PublicKey, deserialized.PublicKey);
            Assert.Equal(original.Fingerprint, deserialized.Fingerprint);
            Assert.Equal(original.CreatedAt, deserialized.CreatedAt);
        }

        [Fact]
        public void SSHPasswdModel_Properties_Roundtrip()
        {
            var model = new SSHPasswdModel { SSHPasswd = "s3cret!" };
            Assert.Equal("s3cret!", model.SSHPasswd);
        }

        [Fact]
        public void SSHPasswdModel_Default_IsNull()
        {
            var model = new SSHPasswdModel();
            Assert.Null(model.SSHPasswd);
        }
    }
}
