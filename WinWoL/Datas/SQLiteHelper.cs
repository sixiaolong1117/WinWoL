using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using WinWoL.Methods;
using WinWoL.Models;

namespace WinWoL.Datas
{
    public class SQLiteHelper
    {
        private const int CurrentDatabaseVersion = 3;
        private const string ConnectionString = "Data Source=wol.db";
        private const string WoLColumns = "Id, Name, MacAddress, IPAddress, WoLAddress, WoLPort, RDPPort, SSHCommand, SSHPort, SSHUser, SSHKeyPath, SSHKeyId, WoLIsOpen, RDPIsOpen, SSHIsOpen, BroadcastIsOpen, SSHKeyIsOpen";
        private const string SSHColumns = "Id, Name, IPAddress, SSHCommand, SSHPort, SSHUser, SSHKeyPath, SSHKeyId, SSHKeyIsOpen";

        public SQLiteHelper()
        {
            CreateTableIfNotExists();
            UpgradeDatabase();
        }

        // 建表
        public void CreateTableIfNotExists()
        {
            using (SqliteConnection connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();

                var createTableCommand = connection.CreateCommand();
                createTableCommand.CommandText = "CREATE TABLE IF NOT EXISTS WoLTable (Id INTEGER PRIMARY KEY, Name TEXT, MacAddress TEXT, IPAddress TEXT, WoLAddress TEXT, WoLPort TEXT, RDPPort TEXT, SSHCommand TEXT, SSHPort TEXT, SSHUser TEXT, SSHKeyPath TEXT, SSHKeyId TEXT, WoLIsOpen TEXT, RDPIsOpen TEXT, SSHIsOpen TEXT, BroadcastIsOpen TEXT, SSHKeyIsOpen TEXT)";
                createTableCommand.ExecuteNonQuery();

                var createSSHTableCommand = connection.CreateCommand();
                createSSHTableCommand.CommandText = "CREATE TABLE IF NOT EXISTS SSHTable (Id INTEGER PRIMARY KEY, Name TEXT, IPAddress TEXT, SSHCommand TEXT, SSHPort TEXT, SSHUser TEXT, SSHKeyPath TEXT, SSHKeyId TEXT, SSHKeyIsOpen TEXT)";
                createSSHTableCommand.ExecuteNonQuery();

                CreateSSHKeyTable(connection);

                var createVersionTableCommand = connection.CreateCommand();
                createVersionTableCommand.CommandText = "CREATE TABLE IF NOT EXISTS Version (VersionNumber INTEGER)";
                createVersionTableCommand.ExecuteNonQuery();
            }
        }

        // 删表
        public void DropTable()
        {
            using (SqliteConnection connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();

                var dropTableCommand = connection.CreateCommand();
                dropTableCommand.CommandText = "DROP TABLE IF EXISTS WoLTable;";
                dropTableCommand.ExecuteNonQuery();

                var dropSSHTableCommand = connection.CreateCommand();
                dropSSHTableCommand.CommandText = "DROP TABLE IF EXISTS SSHTable;";
                dropSSHTableCommand.ExecuteNonQuery();

                var dropSSHKeyTableCommand = connection.CreateCommand();
                dropSSHKeyTableCommand.CommandText = "DROP TABLE IF EXISTS SSHKeyTable;";
                dropSSHKeyTableCommand.ExecuteNonQuery();

                var dropVersionTableCommand = connection.CreateCommand();
                dropVersionTableCommand.CommandText = "DROP TABLE IF EXISTS Version;";
                dropVersionTableCommand.ExecuteNonQuery();
            }
        }

        // 检查数据库版本
        public int GetDatabaseVersion()
        {
            using (SqliteConnection connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();
                return GetDatabaseVersion(connection);
            }
        }

        // 更新数据库版本信息
        public void UpgradeDatabaseVersion()
        {
            using (SqliteConnection connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();
                SetDatabaseVersion(connection, CurrentDatabaseVersion);
            }
        }

        // 数据库升级
        public void UpgradeDatabase()
        {
            using (SqliteConnection connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();

                EnsureColumn(connection, "WoLTable", "SSHKeyId", "TEXT");
                EnsureColumn(connection, "SSHTable", "SSHKeyId", "TEXT");
                CreateSSHKeyTable(connection);
                EnsureColumn(connection, "SSHKeyTable", "PublicKey", "TEXT");
                EnsureColumn(connection, "SSHKeyTable", "Fingerprint", "TEXT");
                MigrateSSHKeyPaths(connection);
                BackfillSSHKeyMetadata(connection);

                if (GetDatabaseVersion(connection) != CurrentDatabaseVersion)
                {
                    SetDatabaseVersion(connection, CurrentDatabaseVersion);
                }
            }
        }

        // 插入数据
        public void InsertData(WoLModel model)
        {
            UpgradeDatabase();

            using (SqliteConnection connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();

                var insertCommand = connection.CreateCommand();
                insertCommand.CommandText = "INSERT INTO WoLTable (Name, MacAddress, IPAddress, WoLAddress, WoLPort, RDPPort, SSHCommand, SSHPort, SSHUser, SSHKeyPath, SSHKeyId, WoLIsOpen, RDPIsOpen, SSHIsOpen, BroadcastIsOpen, SSHKeyIsOpen) VALUES (@Name, @MacAddress, @IPAddress, @WoLAddress, @WoLPort, @RDPPort, @SSHCommand, @SSHPort, @SSHUser, @SSHKeyPath, @SSHKeyId, @WoLIsOpen, @RDPIsOpen, @SSHIsOpen, @BroadcastIsOpen, @SSHKeyIsOpen)";

                insertCommand.Parameters.AddWithValue("@Name", model.Name ?? (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("@MacAddress", model.MacAddress ?? (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("@IPAddress", model.IPAddress ?? (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("@WoLAddress", model.WoLAddress ?? (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("@WoLPort", model.WoLPort ?? (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("@RDPPort", model.RDPPort ?? (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("@SSHCommand", model.SSHCommand ?? (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("@SSHPort", model.SSHPort ?? (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("@SSHUser", model.SSHUser ?? (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("@SSHKeyPath", model.SSHKeyPath ?? (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("@SSHKeyId", model.SSHKeyId ?? (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("@WoLIsOpen", model.WoLIsOpen ?? (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("@RDPIsOpen", model.RDPIsOpen ?? (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("@SSHIsOpen", model.SSHIsOpen ?? (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("@BroadcastIsOpen", model.BroadcastIsOpen ?? (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("@SSHKeyIsOpen", model.SSHKeyIsOpen ?? (object)DBNull.Value);

                insertCommand.ExecuteNonQuery();
            }
        }

        public void InsertSSHData(SSHModel model)
        {
            UpgradeDatabase();

            using (SqliteConnection connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();

                var insertCommand = connection.CreateCommand();
                insertCommand.CommandText = "INSERT INTO SSHTable (Name, IPAddress, SSHCommand, SSHPort, SSHUser, SSHKeyPath, SSHKeyId, SSHKeyIsOpen) VALUES (@Name, @IPAddress, @SSHCommand, @SSHPort, @SSHUser, @SSHKeyPath, @SSHKeyId, @SSHKeyIsOpen)";

                insertCommand.Parameters.AddWithValue("@Name", model.Name ?? (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("@IPAddress", model.IPAddress ?? (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("@SSHCommand", model.SSHCommand ?? (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("@SSHPort", model.SSHPort ?? (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("@SSHUser", model.SSHUser ?? (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("@SSHKeyPath", model.SSHKeyPath ?? (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("@SSHKeyId", model.SSHKeyId ?? (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("@SSHKeyIsOpen", model.SSHKeyIsOpen ?? (object)DBNull.Value);

                insertCommand.ExecuteNonQuery();
            }
        }

        public int InsertSSHKey(SSHKeyModel model)
        {
            UpgradeDatabase();

            using (SqliteConnection connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();

                return InsertSSHKey(connection, model);
            }
        }

        // 删除数据
        public void DeleteData(WoLModel model)
        {
            UpgradeDatabase();

            using (SqliteConnection connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();

                var deleteCommand = connection.CreateCommand();
                deleteCommand.CommandText = "DELETE FROM WoLTable WHERE Id = @Id";
                deleteCommand.Parameters.AddWithValue("@Id", model.Id);
                deleteCommand.ExecuteNonQuery();
            }
        }

        public void DeleteSSHData(SSHModel model)
        {
            UpgradeDatabase();

            using (SqliteConnection connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();

                var deleteCommand = connection.CreateCommand();
                deleteCommand.CommandText = "DELETE FROM SSHTable WHERE Id = @Id";
                deleteCommand.Parameters.AddWithValue("@Id", model.Id);
                deleteCommand.ExecuteNonQuery();
            }
        }

        public void DeleteSSHKey(int id)
        {
            UpgradeDatabase();

            using (SqliteConnection connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();

                var deleteCommand = connection.CreateCommand();
                deleteCommand.CommandText = "DELETE FROM SSHKeyTable WHERE Id = @Id";
                deleteCommand.Parameters.AddWithValue("@Id", id);
                deleteCommand.ExecuteNonQuery();

                var clearWoLCommand = connection.CreateCommand();
                clearWoLCommand.CommandText = "UPDATE WoLTable SET SSHKeyId = NULL WHERE SSHKeyId = @SSHKeyId";
                clearWoLCommand.Parameters.AddWithValue("@SSHKeyId", id.ToString());
                clearWoLCommand.ExecuteNonQuery();

                var clearSSHCommand = connection.CreateCommand();
                clearSSHCommand.CommandText = "UPDATE SSHTable SET SSHKeyId = NULL WHERE SSHKeyId = @SSHKeyId";
                clearSSHCommand.Parameters.AddWithValue("@SSHKeyId", id.ToString());
                clearSSHCommand.ExecuteNonQuery();
            }
        }

        // 更新数据
        public void UpdateData(WoLModel model)
        {
            UpgradeDatabase();

            using (SqliteConnection connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();

                var updateCommand = connection.CreateCommand();
                updateCommand.CommandText = "UPDATE WoLTable SET Name = @Name, MacAddress = @MacAddress, IPAddress = @IPAddress, WoLAddress = @WoLAddress, WoLPort = @WoLPort, RDPPort = @RDPPort, SSHCommand = @SSHCommand, SSHPort = @SSHPort, SSHUser = @SSHUser, SSHKeyPath = @SSHKeyPath, SSHKeyId = @SSHKeyId, WoLIsOpen = @WoLIsOpen, RDPIsOpen = @RDPIsOpen, SSHIsOpen = @SSHIsOpen, BroadcastIsOpen = @BroadcastIsOpen, SSHKeyIsOpen = @SSHKeyIsOpen WHERE Id = @Id";

                updateCommand.Parameters.AddWithValue("@Id", model.Id);
                updateCommand.Parameters.AddWithValue("@Name", model.Name ?? (object)DBNull.Value);
                updateCommand.Parameters.AddWithValue("@MacAddress", model.MacAddress ?? (object)DBNull.Value);
                updateCommand.Parameters.AddWithValue("@IPAddress", model.IPAddress ?? (object)DBNull.Value);
                updateCommand.Parameters.AddWithValue("@WoLAddress", model.WoLAddress ?? (object)DBNull.Value);
                updateCommand.Parameters.AddWithValue("@WoLPort", model.WoLPort ?? (object)DBNull.Value);
                updateCommand.Parameters.AddWithValue("@RDPPort", model.RDPPort ?? (object)DBNull.Value);
                updateCommand.Parameters.AddWithValue("@SSHCommand", model.SSHCommand ?? (object)DBNull.Value);
                updateCommand.Parameters.AddWithValue("@SSHPort", model.SSHPort ?? (object)DBNull.Value);
                updateCommand.Parameters.AddWithValue("@SSHUser", model.SSHUser ?? (object)DBNull.Value);
                updateCommand.Parameters.AddWithValue("@SSHKeyPath", model.SSHKeyPath ?? (object)DBNull.Value);
                updateCommand.Parameters.AddWithValue("@SSHKeyId", model.SSHKeyId ?? (object)DBNull.Value);
                updateCommand.Parameters.AddWithValue("@WoLIsOpen", model.WoLIsOpen ?? (object)DBNull.Value);
                updateCommand.Parameters.AddWithValue("@RDPIsOpen", model.RDPIsOpen ?? (object)DBNull.Value);
                updateCommand.Parameters.AddWithValue("@SSHIsOpen", model.SSHIsOpen ?? (object)DBNull.Value);
                updateCommand.Parameters.AddWithValue("@BroadcastIsOpen", model.BroadcastIsOpen ?? (object)DBNull.Value);
                updateCommand.Parameters.AddWithValue("@SSHKeyIsOpen", model.SSHKeyIsOpen ?? (object)DBNull.Value);

                updateCommand.ExecuteNonQuery();
            }
        }

        public void UpdateSSHData(SSHModel model)
        {
            UpgradeDatabase();

            using (SqliteConnection connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();

                var updateCommand = connection.CreateCommand();
                updateCommand.CommandText = "UPDATE SSHTable SET Name = @Name, IPAddress = @IPAddress, SSHCommand = @SSHCommand, SSHPort = @SSHPort, SSHUser = @SSHUser, SSHKeyPath = @SSHKeyPath, SSHKeyId = @SSHKeyId, SSHKeyIsOpen = @SSHKeyIsOpen WHERE Id = @Id";

                updateCommand.Parameters.AddWithValue("@Id", model.Id);
                updateCommand.Parameters.AddWithValue("@Name", model.Name ?? (object)DBNull.Value);
                updateCommand.Parameters.AddWithValue("@IPAddress", model.IPAddress ?? (object)DBNull.Value);
                updateCommand.Parameters.AddWithValue("@SSHCommand", model.SSHCommand ?? (object)DBNull.Value);
                updateCommand.Parameters.AddWithValue("@SSHPort", model.SSHPort ?? (object)DBNull.Value);
                updateCommand.Parameters.AddWithValue("@SSHUser", model.SSHUser ?? (object)DBNull.Value);
                updateCommand.Parameters.AddWithValue("@SSHKeyPath", model.SSHKeyPath ?? (object)DBNull.Value);
                updateCommand.Parameters.AddWithValue("@SSHKeyId", model.SSHKeyId ?? (object)DBNull.Value);
                updateCommand.Parameters.AddWithValue("@SSHKeyIsOpen", model.SSHKeyIsOpen ?? (object)DBNull.Value);

                updateCommand.ExecuteNonQuery();
            }
        }

        // 根据ID获得数据
        public WoLModel GetDataById(int id)
        {
            UpgradeDatabase();

            using (SqliteConnection connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();

                var selectCommand = connection.CreateCommand();
                selectCommand.CommandText = $"SELECT {WoLColumns} FROM WoLTable WHERE Id = @Id";
                selectCommand.Parameters.AddWithValue("@Id", id);

                using (SqliteDataReader reader = selectCommand.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return ReadWoLModel(reader);
                    }
                }
            }

            return null;
        }

        public SSHModel GetSSHDataById(int id)
        {
            UpgradeDatabase();

            using (SqliteConnection connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();

                var selectCommand = connection.CreateCommand();
                selectCommand.CommandText = $"SELECT {SSHColumns} FROM SSHTable WHERE Id = @Id";
                selectCommand.Parameters.AddWithValue("@Id", id);

                using (SqliteDataReader reader = selectCommand.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return ReadSSHModel(reader);
                    }
                }
            }

            return null;
        }

        public SSHKeyModel GetSSHKeyById(int id)
        {
            UpgradeDatabase();

            using (SqliteConnection connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();

                var selectCommand = connection.CreateCommand();
                selectCommand.CommandText = "SELECT Id, Name, PrivateKey, PublicKey, Fingerprint, CreatedAt FROM SSHKeyTable WHERE Id = @Id";
                selectCommand.Parameters.AddWithValue("@Id", id);

                using (SqliteDataReader reader = selectCommand.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return ReadSSHKeyModel(reader, true);
                    }
                }
            }

            return null;
        }

        public List<WoLModel> GetDataListById(int id)
        {
            List<WoLModel> entries = new List<WoLModel>();
            UpgradeDatabase();

            using (SqliteConnection connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();

                var queryCommand = connection.CreateCommand();
                queryCommand.CommandText = $"SELECT {WoLColumns} FROM WoLTable WHERE Id = @Id";
                queryCommand.Parameters.AddWithValue("@Id", id);

                using (SqliteDataReader reader = queryCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        entries.Add(ReadWoLModel(reader));
                    }
                }
            }

            return entries;
        }

        public List<WoLModel> GetDataListByIdHideAddress(int id)
        {
            List<WoLModel> entries = new List<WoLModel>();
            UpgradeDatabase();

            using (SqliteConnection connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();

                var queryCommand = connection.CreateCommand();
                queryCommand.CommandText = $"SELECT {WoLColumns} FROM WoLTable WHERE Id = @Id";
                queryCommand.Parameters.AddWithValue("@Id", id);

                using (SqliteDataReader reader = queryCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        WoLModel entry = ReadWoLModel(reader);
                        entry.MacAddress = "**:**:**:**";
                        entry.IPAddress = "***.***.***.***";
                        entry.WoLAddress = "***.***.***.***";
                        entry.WoLPort = "*";
                        entry.RDPPort = "****";
                        entry.SSHCommand = "*******";
                        entry.SSHPort = "**";
                        entry.SSHUser = "*";
                        entry.SSHKeyPath = "*";
                        entry.SSHKeyId = "*";
                        entries.Add(entry);
                    }
                }
            }

            return entries;
        }

        // 查询数据
        public List<WoLModel> QueryData()
        {
            List<WoLModel> entries = new List<WoLModel>();
            UpgradeDatabase();

            using (SqliteConnection connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();

                var queryCommand = connection.CreateCommand();
                queryCommand.CommandText = $"SELECT {WoLColumns} FROM WoLTable";

                using (SqliteDataReader reader = queryCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        entries.Add(ReadWoLModel(reader));
                    }
                }
            }

            return entries;
        }

        public List<SSHModel> QuerySSHData()
        {
            List<SSHModel> entries = new List<SSHModel>();
            UpgradeDatabase();

            using (SqliteConnection connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();

                var queryCommand = connection.CreateCommand();
                queryCommand.CommandText = $"SELECT {SSHColumns} FROM SSHTable";

                using (SqliteDataReader reader = queryCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        entries.Add(ReadSSHModel(reader));
                    }
                }
            }

            return entries;
        }

        public List<SSHKeyModel> QuerySSHKeys()
        {
            List<SSHKeyModel> entries = new List<SSHKeyModel>();
            UpgradeDatabase();

            using (SqliteConnection connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();

                var queryCommand = connection.CreateCommand();
                queryCommand.CommandText = "SELECT Id, Name, PrivateKey, PublicKey, Fingerprint, CreatedAt FROM SSHKeyTable ORDER BY Id";

                using (SqliteDataReader reader = queryCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        entries.Add(ReadSSHKeyModel(reader, false));
                    }
                }
            }

            return entries;
        }

        // 获取上一行的ID
        public int GetPreRowsId(WoLModel wolModel)
        {
            return GetAdjacentId("WoLTable", wolModel.Id, true);
        }

        public int GetSSHPreRowsId(SSHModel sshModel)
        {
            return GetAdjacentId("SSHTable", sshModel.Id, true);
        }

        // 向上移动项
        public bool UpSwapRows(WoLModel wolModel)
        {
            int srcId = wolModel.Id;
            int preId = GetPreRowsId(wolModel);
            if (preId >= 0)
            {
                WoLModel srcModel = GetDataById(srcId);
                WoLModel preModel = GetDataById(preId);

                srcModel.Id = preId;
                preModel.Id = srcId;

                UpdateData(srcModel);
                UpdateData(preModel);
                return true;
            }
            return false;
        }

        public bool UpSwapSSHRows(SSHModel sshModel)
        {
            int srcId = sshModel.Id;
            int preId = GetSSHPreRowsId(sshModel);
            if (preId >= 0)
            {
                SSHModel srcModel = GetSSHDataById(srcId);
                SSHModel preModel = GetSSHDataById(preId);

                srcModel.Id = preId;
                preModel.Id = srcId;

                UpdateSSHData(srcModel);
                UpdateSSHData(preModel);
                return true;
            }
            return false;
        }

        // 获取下一行的ID
        public int GetPosRowsId(WoLModel wolModel)
        {
            return GetAdjacentId("WoLTable", wolModel.Id, false);
        }

        public int GetSSHPosRowsId(SSHModel sshModel)
        {
            return GetAdjacentId("SSHTable", sshModel.Id, false);
        }

        // 向下移动项
        public bool DownSwapRows(WoLModel wolModel)
        {
            int srcId = wolModel.Id;
            int posId = GetPosRowsId(wolModel);
            if (posId >= 0)
            {
                WoLModel srcModel = GetDataById(srcId);
                WoLModel posModel = GetDataById(posId);

                srcModel.Id = posId;
                posModel.Id = srcId;

                UpdateData(srcModel);
                UpdateData(posModel);
                return true;
            }
            return false;
        }

        public bool DownSwapSSHRows(SSHModel sshModel)
        {
            int srcId = sshModel.Id;
            int posId = GetSSHPosRowsId(sshModel);
            if (posId >= 0)
            {
                SSHModel srcModel = GetSSHDataById(srcId);
                SSHModel posModel = GetSSHDataById(posId);

                srcModel.Id = posId;
                posModel.Id = srcId;

                UpdateSSHData(srcModel);
                UpdateSSHData(posModel);
                return true;
            }
            return false;
        }

        private static int GetDatabaseVersion(SqliteConnection connection)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT VersionNumber FROM Version LIMIT 1";
                var result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value && int.TryParse(result.ToString(), out int version))
                {
                    return version;
                }
                return -1;
            }
        }

        private static void SetDatabaseVersion(SqliteConnection connection, int version)
        {
            using (var cmd = connection.CreateCommand())
            {
                if (GetDatabaseVersion(connection) == -1)
                {
                    cmd.CommandText = "INSERT INTO Version (VersionNumber) VALUES (@VersionNumber)";
                }
                else
                {
                    cmd.CommandText = "UPDATE Version SET VersionNumber = @VersionNumber";
                }
                cmd.Parameters.AddWithValue("@VersionNumber", version);
                cmd.ExecuteNonQuery();
            }
        }

        private static void CreateSSHKeyTable(SqliteConnection connection)
        {
            var createSSHKeyTableCommand = connection.CreateCommand();
            createSSHKeyTableCommand.CommandText = "CREATE TABLE IF NOT EXISTS SSHKeyTable (Id INTEGER PRIMARY KEY, Name TEXT, PrivateKey TEXT, PublicKey TEXT, Fingerprint TEXT, CreatedAt TEXT)";
            createSSHKeyTableCommand.ExecuteNonQuery();
        }

        private static int InsertSSHKey(SqliteConnection connection, SSHKeyModel model)
        {
            SSHKeyModel enrichedModel = EnsureSSHKeyMetadata(model);
            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = "INSERT INTO SSHKeyTable (Name, PrivateKey, PublicKey, Fingerprint, CreatedAt) VALUES (@Name, @PrivateKey, @PublicKey, @Fingerprint, @CreatedAt)";
            insertCommand.Parameters.AddWithValue("@Name", enrichedModel.Name ?? (object)DBNull.Value);
            insertCommand.Parameters.AddWithValue("@PrivateKey", SSHKeyProtection.Protect(enrichedModel.PrivateKey) ?? (object)DBNull.Value);
            insertCommand.Parameters.AddWithValue("@PublicKey", enrichedModel.PublicKey ?? (object)DBNull.Value);
            insertCommand.Parameters.AddWithValue("@Fingerprint", enrichedModel.Fingerprint ?? (object)DBNull.Value);
            insertCommand.Parameters.AddWithValue("@CreatedAt", enrichedModel.CreatedAt ?? (object)DBNull.Value);
            insertCommand.ExecuteNonQuery();

            insertCommand.CommandText = "SELECT last_insert_rowid()";
            insertCommand.Parameters.Clear();
            return Convert.ToInt32(insertCommand.ExecuteScalar());
        }

        private static SSHKeyModel EnsureSSHKeyMetadata(SSHKeyModel model)
        {
            if (!string.IsNullOrWhiteSpace(model.PublicKey) && !string.IsNullOrWhiteSpace(model.Fingerprint))
            {
                return model;
            }

            SSHKeyModel enrichedModel = SSHKeyMethod.CreateSSHKeyModel(model.Name, model.PrivateKey);
            if (!string.IsNullOrWhiteSpace(model.CreatedAt))
            {
                enrichedModel.CreatedAt = model.CreatedAt;
            }

            return enrichedModel;
        }

        private static void MigrateSSHKeyPaths(SqliteConnection connection)
        {
            List<string> keyPaths = new List<string>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT DISTINCT SSHKeyPath FROM SSHTable WHERE SSHKeyPath IS NOT NULL AND SSHKeyPath <> '' AND (SSHKeyId IS NULL OR SSHKeyId = '') UNION SELECT DISTINCT SSHKeyPath FROM WoLTable WHERE SSHKeyPath IS NOT NULL AND SSHKeyPath <> '' AND (SSHKeyId IS NULL OR SSHKeyId = '')";
                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string keyPath = reader.IsDBNull(0) ? "" : reader.GetString(0);
                        if (!string.IsNullOrWhiteSpace(keyPath) && !int.TryParse(keyPath, out _))
                        {
                            keyPaths.Add(keyPath);
                        }
                    }
                }
            }

            foreach (string keyPath in keyPaths)
            {
                try
                {
                    if (!File.Exists(keyPath))
                    {
                        continue;
                    }

                    string privateKey = File.ReadAllText(keyPath);
                    if (string.IsNullOrWhiteSpace(privateKey))
                    {
                        continue;
                    }

                    int sshKeyId = InsertSSHKey(connection, new SSHKeyModel
                    {
                        Name = Path.GetFileName(keyPath),
                        PrivateKey = privateKey,
                        CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    });
                    UpdateMigratedSSHKeyPath(connection, "SSHTable", keyPath, sshKeyId);
                    UpdateMigratedSSHKeyPath(connection, "WoLTable", keyPath, sshKeyId);
                }
                catch
                {
                    // 如果旧路径不可读，保留配置，用户可在编辑时重新导入密钥。
                }
            }
        }

        private static void UpdateMigratedSSHKeyPath(SqliteConnection connection, string tableName, string keyPath, int sshKeyId)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"UPDATE {tableName} SET SSHKeyId = @SSHKeyId WHERE SSHKeyPath = @SSHKeyPath AND (SSHKeyId IS NULL OR SSHKeyId = '')";
                command.Parameters.AddWithValue("@SSHKeyId", sshKeyId.ToString());
                command.Parameters.AddWithValue("@SSHKeyPath", keyPath);
                command.ExecuteNonQuery();
            }
        }

        private static void BackfillSSHKeyMetadata(SqliteConnection connection)
        {
            List<SSHKeyModel> keys = new List<SSHKeyModel>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT Id, Name, PrivateKey, PublicKey, Fingerprint, CreatedAt FROM SSHKeyTable WHERE PrivateKey IS NOT NULL AND PrivateKey <> '' AND (PublicKey IS NULL OR PublicKey = '' OR Fingerprint IS NULL OR Fingerprint = '')";
                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        try
                        {
                            keys.Add(ReadSSHKeyModel(reader, true));
                        }
                        catch
                        {
                            // 加密内容损坏时跳过回填，不影响其他密钥使用。
                        }
                    }
                }
            }

            foreach (SSHKeyModel key in keys)
            {
                try
                {
                    SSHKeyModel enrichedKey = SSHKeyMethod.CreateSSHKeyModel(key.Name, key.PrivateKey);
                    using (var updateCommand = connection.CreateCommand())
                    {
                        updateCommand.CommandText = "UPDATE SSHKeyTable SET PublicKey = @PublicKey, Fingerprint = @Fingerprint WHERE Id = @Id";
                        updateCommand.Parameters.AddWithValue("@Id", key.Id);
                        updateCommand.Parameters.AddWithValue("@PublicKey", enrichedKey.PublicKey ?? (object)DBNull.Value);
                        updateCommand.Parameters.AddWithValue("@Fingerprint", enrichedKey.Fingerprint ?? (object)DBNull.Value);
                        updateCommand.ExecuteNonQuery();
                    }
                }
                catch
                {
                    // 旧数据无法解析时保留原记录，用户可删除后重新导入。
                }
            }
        }

        private static void EnsureColumn(SqliteConnection connection, string tableName, string columnName, string columnType)
        {
            bool columnExists = false;
            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"PRAGMA table_info({tableName})";
                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                        {
                            columnExists = true;
                            break;
                        }
                    }
                }
            }

            if (!columnExists)
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnType}";
                    command.ExecuteNonQuery();
                }
            }
        }

        private static int GetAdjacentId(string tableName, int sourceId, bool previous)
        {
            using (SqliteConnection connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = previous
                        ? $"SELECT MAX(Id) FROM {tableName} WHERE Id < @srcId"
                        : $"SELECT MIN(Id) FROM {tableName} WHERE Id > @srcId";
                    command.Parameters.AddWithValue("@srcId", sourceId);

                    var result = command.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        return Convert.ToInt32(result);
                    }
                }
            }

            return -1;
        }

        private static WoLModel ReadWoLModel(SqliteDataReader reader)
        {
            return new WoLModel
            {
                Id = reader.GetInt32(0),
                Name = reader.IsDBNull(1) ? "" : reader.GetString(1),
                MacAddress = reader.IsDBNull(2) ? "" : reader.GetString(2),
                IPAddress = reader.IsDBNull(3) ? "" : reader.GetString(3),
                WoLAddress = reader.IsDBNull(4) ? "" : reader.GetString(4),
                WoLPort = reader.IsDBNull(5) ? "" : reader.GetString(5),
                RDPPort = reader.IsDBNull(6) ? "" : reader.GetString(6),
                SSHCommand = reader.IsDBNull(7) ? "" : reader.GetString(7),
                SSHPort = reader.IsDBNull(8) ? "" : reader.GetString(8),
                SSHUser = reader.IsDBNull(9) ? "" : reader.GetString(9),
                SSHKeyPath = reader.IsDBNull(10) ? "" : reader.GetString(10),
                SSHKeyId = reader.IsDBNull(11) ? "" : reader.GetString(11),
                WoLIsOpen = reader.IsDBNull(12) ? "" : reader.GetString(12),
                RDPIsOpen = reader.IsDBNull(13) ? "" : reader.GetString(13),
                SSHIsOpen = reader.IsDBNull(14) ? "" : reader.GetString(14),
                BroadcastIsOpen = reader.IsDBNull(15) ? "" : reader.GetString(15),
                SSHKeyIsOpen = reader.IsDBNull(16) ? "" : reader.GetString(16)
            };
        }

        private static SSHModel ReadSSHModel(SqliteDataReader reader)
        {
            return new SSHModel
            {
                Id = reader.GetInt32(0),
                Name = reader.IsDBNull(1) ? "" : reader.GetString(1),
                IPAddress = reader.IsDBNull(2) ? "" : reader.GetString(2),
                SSHCommand = reader.IsDBNull(3) ? "" : reader.GetString(3),
                SSHPort = reader.IsDBNull(4) ? "" : reader.GetString(4),
                SSHUser = reader.IsDBNull(5) ? "" : reader.GetString(5),
                SSHKeyPath = reader.IsDBNull(6) ? "" : reader.GetString(6),
                SSHKeyId = reader.IsDBNull(7) ? "" : reader.GetString(7),
                SSHKeyIsOpen = reader.IsDBNull(8) ? "" : reader.GetString(8)
            };
        }

        private static SSHKeyModel ReadSSHKeyModel(SqliteDataReader reader, bool includePrivateKey)
        {
            return new SSHKeyModel
            {
                Id = reader.GetInt32(0),
                Name = reader.IsDBNull(1) ? "" : reader.GetString(1),
                PrivateKey = includePrivateKey && !reader.IsDBNull(2) ? SSHKeyProtection.Unprotect(reader.GetString(2)) : "",
                PublicKey = reader.IsDBNull(3) ? "" : reader.GetString(3),
                Fingerprint = reader.IsDBNull(4) ? "" : reader.GetString(4),
                CreatedAt = reader.IsDBNull(5) ? "" : reader.GetString(5)
            };
        }
    }
}
