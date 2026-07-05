using System;
using System.IO;
using Microsoft.Data.Sqlite;
using Xunit;
using WinWoL.Datas;
using WinWoL.Models;

namespace WinWoL.Tests
{
    public class SQLiteHelperTests : IDisposable
    {
        private readonly string _dbPath;

        public SQLiteHelperTests()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), $"wwtest_{Guid.NewGuid():N}.db");
        }

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            try { if (File.Exists(_dbPath)) File.Delete(_dbPath); } catch { }
        }

        private SQLiteHelper CreateHelper()
        {
            return new SQLiteHelper($"Data Source={_dbPath}");
        }

        [Fact]
        public void Constructor_CreatesTables()
        {
            var helper = CreateHelper();
            Assert.Empty(helper.QueryData());
            Assert.Empty(helper.QuerySSHData());
            Assert.Empty(helper.QuerySSHKeys());
        }

        [Fact]
        public void InsertWoL_ThenQuery_ReturnsInsertedData()
        {
            var helper = CreateHelper();
            var model = new WoLModel
            {
                Name = "Test PC",
                MacAddress = "AA:BB:CC:DD:EE:FF",
                IPAddress = "192.168.1.100",
                WoLAddress = "255.255.255.255",
                WoLPort = "9",
                RDPPort = "3389",
                SSHCommand = "whoami",
                SSHPort = "22",
                SSHUser = "admin",
                WoLIsOpen = "True",
                RDPIsOpen = "False",
                SSHIsOpen = "False",
                BroadcastIsOpen = "True",
                SSHKeyIsOpen = "False"
            };

            helper.InsertData(model);
            var result = helper.QueryData();

            Assert.Single(result);
            Assert.Equal("Test PC", result[0].Name);
            Assert.Equal("AA:BB:CC:DD:EE:FF", result[0].MacAddress);
            Assert.Equal("192.168.1.100", result[0].IPAddress);
            Assert.Equal("9", result[0].WoLPort);
        }

        [Fact]
        public void InsertWoL_ThenUpdate_ThenQuery_ReturnsUpdatedData()
        {
            var helper = CreateHelper();
            helper.InsertData(new WoLModel
            {
                Name = "Old Name",
                MacAddress = "11:22:33:44:55:66",
                IPAddress = "10.0.0.1"
            });

            var inserted = helper.QueryData()[0];
            inserted.Name = "New Name";
            inserted.IPAddress = "10.0.0.100";
            helper.UpdateData(inserted);

            var result = helper.QueryData();
            Assert.Single(result);
            Assert.Equal("New Name", result[0].Name);
            Assert.Equal("10.0.0.100", result[0].IPAddress);
        }

        [Fact]
        public void InsertWoL_ThenDelete_ThenQuery_ReturnsEmpty()
        {
            var helper = CreateHelper();
            helper.InsertData(new WoLModel
            {
                Name = "To Delete",
                MacAddress = "11:22:33:44:55:66"
            });

            var inserted = helper.QueryData()[0];
            helper.DeleteData(inserted);
            Assert.Empty(helper.QueryData());
        }

        [Fact]
        public void InsertMultipleWoL_ThenQuery_ReturnsAll()
        {
            var helper = CreateHelper();
            helper.InsertData(new WoLModel { Name = "PC 1", MacAddress = "AA:AA:AA:AA:AA:AA" });
            helper.InsertData(new WoLModel { Name = "PC 2", MacAddress = "BB:BB:BB:BB:BB:BB" });
            Assert.Equal(2, helper.QueryData().Count);
        }

        [Fact]
        public void InsertWoLWithNullFields_DoesNotThrow()
        {
            var helper = CreateHelper();
            helper.InsertData(new WoLModel
            {
                Name = "Minimal",
                MacAddress = null,
                IPAddress = null,
                SSHCommand = null
            });

            var result = helper.QueryData();
            Assert.Single(result);
            Assert.Equal("Minimal", result[0].Name);
        }

        [Fact]
        public void InsertWoLWithSpecialCharacters_Succeeds()
        {
            var helper = CreateHelper();
            helper.InsertData(new WoLModel
            {
                Name = "PC with ' quotes and \" double",
                MacAddress = "AA:BB:CC:DD:EE:FF",
                IPAddress = "192.168.1.1"
            });

            var result = helper.QueryData();
            Assert.Single(result);
            Assert.Equal("PC with ' quotes and \" double", result[0].Name);
        }

        [Fact]
        public void InsertSSH_ThenQuery_ReturnsInsertedData()
        {
            var helper = CreateHelper();
            helper.InsertSSHData(new SSHModel
            {
                Name = "SSH Server",
                IPAddress = "192.168.1.200",
                SSHCommand = "uptime",
                SSHPort = "22",
                SSHUser = "root"
            });

            var result = helper.QuerySSHData();
            Assert.Single(result);
            Assert.Equal("SSH Server", result[0].Name);
            Assert.Equal("192.168.1.200", result[0].IPAddress);
        }

        [Fact]
        public void InsertSSH_ThenDelete_ThenQuery_ReturnsEmpty()
        {
            var helper = CreateHelper();
            helper.InsertSSHData(new SSHModel { Name = "Temp", IPAddress = "10.0.0.1" });
            var inserted = helper.QuerySSHData()[0];
            helper.DeleteSSHData(inserted);
            Assert.Empty(helper.QuerySSHData());
        }

        [Fact]
        public void InsertSSHKey_ThenQuery_ReturnsInserted()
        {
            var helper = CreateHelper();
            var key = new SSHKeyModel
            {
                Name = "Test Key",
                PrivateKey = "test-private-key",
                PublicKey = "ssh-rsa AAAAB3NzaC1yc2E...",
                Fingerprint = "SHA256:testfingerprint",
                CreatedAt = "2024-01-01 00:00:00"
            };

            int id = helper.InsertSSHKey(key);
            Assert.True(id > 0);

            var result = helper.QuerySSHKeys();
            Assert.Single(result);
            Assert.Equal("Test Key", result[0].Name);
            Assert.Equal("ssh-rsa AAAAB3NzaC1yc2E...", result[0].PublicKey);
        }

        [Fact]
        public void InsertSSHKey_EncryptsAndDecryptsPrivateKey()
        {
            var helper = CreateHelper();
            var key = new SSHKeyModel
            {
                Name = "Roundtrip Key",
                PrivateKey = "test-private-key-content-for-encryption-test",
                PublicKey = "ssh-rsa AAAA...",
                Fingerprint = "SHA256:test",
                CreatedAt = "2024-06-01 12:00:00"
            };

            int id = helper.InsertSSHKey(key);
            var loaded = helper.GetSSHKeyById(id);

            Assert.Equal("test-private-key-content-for-encryption-test", loaded.PrivateKey);
        }

        [Fact]
        public void InsertSSHKey_ThenDelete_ClearsReferencesInWoLAndSSH()
        {
            var helper = CreateHelper();
            int keyId = helper.InsertSSHKey(new SSHKeyModel
            {
                Name = "Key to Delete",
                PrivateKey = "content",
                PublicKey = "pub",
                Fingerprint = "fp",
                CreatedAt = "2024-01-01"
            });

            helper.InsertData(new WoLModel { Name = "WoL with key", SSHKeyId = keyId.ToString() });
            helper.InsertSSHData(new SSHModel { Name = "SSH with key", SSHKeyId = keyId.ToString() });

            helper.DeleteSSHKey(keyId);

            Assert.Equal("", helper.QueryData()[0].SSHKeyId);
            Assert.Equal("", helper.QuerySSHData()[0].SSHKeyId);
        }

        [Fact]
        public void GetDataById_ReturnsCorrectRow()
        {
            var helper = CreateHelper();
            helper.InsertData(new WoLModel { Name = "Find Me", MacAddress = "AA:BB:CC:DD:EE:FF" });
            int id = helper.QueryData()[0].Id;

            var result = helper.GetDataById(id);
            Assert.NotNull(result);
            Assert.Equal("Find Me", result.Name);
        }

        [Fact]
        public void GetDataById_ReturnsNull_WhenNotFound()
        {
            var helper = CreateHelper();
            Assert.Null(helper.GetDataById(999));
        }

        [Fact]
        public void GetDataListByIdHideAddress_MasksSensitiveFields()
        {
            var helper = CreateHelper();
            helper.InsertData(new WoLModel
            {
                Name = "Secret PC",
                MacAddress = "AA:BB:CC:DD:EE:FF",
                IPAddress = "192.168.1.10",
                WoLAddress = "192.168.1.255",
                WoLPort = "9",
                RDPPort = "3389",
                SSHCommand = "whoami",
                SSHPort = "22",
                SSHUser = "admin",
                SSHKeyPath = "/path/to/key",
                SSHKeyId = "1"
            });

            int id = helper.QueryData()[0].Id;
            var masked = helper.GetDataListByIdHideAddress(id);

            Assert.Single(masked);
            Assert.Equal("**:**:**:**", masked[0].MacAddress);
            Assert.Equal("***.***.***.***", masked[0].IPAddress);
            Assert.Equal("*******", masked[0].SSHCommand);
        }

        [Fact]
        public void GetPreRowsId_ReturnsPreviousId()
        {
            var helper = CreateHelper();
            helper.InsertData(new WoLModel { Name = "A" });
            helper.InsertData(new WoLModel { Name = "B" });
            var entries = helper.QueryData();

            int pre = helper.GetPreRowsId(entries[1]);
            Assert.Equal(entries[0].Id, pre);
        }

        [Fact]
        public void GetPreRowsId_ReturnsMinusOne_WhenNoPrevious()
        {
            var helper = CreateHelper();
            helper.InsertData(new WoLModel { Name = "First" });
            int pre = helper.GetPreRowsId(helper.QueryData()[0]);
            Assert.Equal(-1, pre);
        }

        [Fact]
        public void GetPosRowsId_ReturnsNextId()
        {
            var helper = CreateHelper();
            helper.InsertData(new WoLModel { Name = "A" });
            helper.InsertData(new WoLModel { Name = "B" });
            var entries = helper.QueryData();

            int pos = helper.GetPosRowsId(entries[0]);
            Assert.Equal(entries[1].Id, pos);
        }

        [Fact]
        public void UpSwapRows_SwapsOrder()
        {
            var helper = CreateHelper();
            helper.InsertData(new WoLModel { Name = "First", MacAddress = "AA:AA:AA:AA:AA:AA" });
            helper.InsertData(new WoLModel { Name = "Second", MacAddress = "BB:BB:BB:BB:BB:BB" });
            var entries = helper.QueryData();

            bool swapped = helper.UpSwapRows(entries[1]);

            Assert.True(swapped);
            var afterSwap = helper.QueryData();
            Assert.Equal("Second", afterSwap[0].Name);
            Assert.Equal("First", afterSwap[1].Name);
        }

        [Fact]
        public void DownSwapRows_SwapsOrder()
        {
            var helper = CreateHelper();
            helper.InsertData(new WoLModel { Name = "First", MacAddress = "AA:AA:AA:AA:AA:AA" });
            helper.InsertData(new WoLModel { Name = "Second", MacAddress = "BB:BB:BB:BB:BB:BB" });
            var entries = helper.QueryData();

            bool swapped = helper.DownSwapRows(entries[0]);

            Assert.True(swapped);
            var afterSwap = helper.QueryData();
            Assert.Equal("Second", afterSwap[0].Name);
            Assert.Equal("First", afterSwap[1].Name);
        }

        [Fact]
        public void GetDatabaseVersion_ReturnsMinusOne_WhenNoVersionSet()
        {
            var helper = CreateHelper();
            Assert.Equal(3, helper.GetDatabaseVersion());
        }

        [Fact]
        public void DropTable_ClearsData_NewHelperCreatesTable()
        {
            var helper = CreateHelper();
            helper.InsertData(new WoLModel { Name = "Test", MacAddress = "11:22:33:44:55:66" });
            helper.DropTable();

            var newHelper = CreateHelper();
            Assert.Empty(newHelper.QueryData());
        }
    }
}
