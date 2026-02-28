using Connector.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Connector.Table
{
    public class DB_pangya_server : IDisposable
    {
        private readonly MySqlDbContext _db;

        public DB_pangya_server(MySqlDbContext db)
        {
            _db = db;
        }

        public DB_pangya_server() : this(DbContextFactory.CreateMySql())
        {
        }

        #region SELECT Methods

        public ServerData SelectByServerID(int serverId)
        {
            string sql = "SELECT * FROM pangya_server WHERE ServerID = @p0";
            return _db.Database.SqlQuery<ServerData>(sql, serverId).FirstOrDefault();
        }

        public List<ServerData> SelectAll()
        {
            string sql = "SELECT * FROM pangya_server";
            return _db.Database.SqlQuery<ServerData>(sql).ToList();
        }

        public List<ServerData> SelectActiveServers()
        {
            string sql = "SELECT * FROM pangya_server WHERE Active = 1";
            return _db.Database.SqlQuery<ServerData>(sql).ToList();
        }

        public List<ServerData> SelectByType(byte serverType)
        {
            string sql = "SELECT * FROM pangya_server WHERE ServerType = @p0 AND Active = 1";
            return _db.Database.SqlQuery<ServerData>(sql, serverType).ToList();
        }

        public bool ExistsByServerID(int serverId)
        {
            string sql = "SELECT COUNT(*) FROM pangya_server WHERE ServerID = @p0";
            return _db.Database.SqlQuery<int>(sql, serverId).FirstOrDefault() > 0;
        }

        #endregion

        #region INSERT Methods

        public int Insert(ServerData data)
        {
            string sql = @"INSERT INTO pangya_server 
                (ServerID, Name, IP, Port, MaxUser, UsersOnline, Property, BlockFunc, 
                 ImgNo, ImgEvent, ServerType, Active) 
                VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11)";

            return _db.Database.ExecuteSqlCommand(sql,
                data.ServerID,
                data.Name,
                data.IP,
                data.Port,
                data.MaxUser,
                data.UsersOnline,
                data.Property,
                data.BlockFunc,
                data.ImgNo,
                data.ImgEvent,
                data.ServerType,
                data.Active
            );
        }

        #endregion

        #region UPDATE Methods

        public int Update(ServerData data)
        {
            string sql = @"UPDATE pangya_server SET 
                Name = @p0, IP = @p1, Port = @p2, MaxUser = @p3, UsersOnline = @p4, 
                Property = @p5, BlockFunc = @p6, ImgNo = @p7, ImgEvent = @p8, 
                ServerType = @p9, Active = @p10 
                WHERE ServerID = @p11";

            return _db.Database.ExecuteSqlCommand(sql,
                data.Name,
                data.IP,
                data.Port,
                data.MaxUser,
                data.UsersOnline,
                data.Property,
                data.BlockFunc,
                data.ImgNo,
                data.ImgEvent,
                data.ServerType,
                data.Active,
                data.ServerID
            );
        }

        public int UpdateUsersOnline(int serverId, int usersOnline)
        {
            string sql = "UPDATE pangya_server SET UsersOnline = @p0 WHERE ServerID = @p1";
            return _db.Database.ExecuteSqlCommand(sql, usersOnline, serverId);
        }

        public int UpdateActive(int serverId, byte active)
        {
            string sql = "UPDATE pangya_server SET Active = @p0 WHERE ServerID = @p1";
            return _db.Database.ExecuteSqlCommand(sql, active, serverId);
        }

        #endregion

        #region DELETE Methods

        public int Delete(int serverId)
        {
            string sql = "DELETE FROM pangya_server WHERE ServerID = @p0";
            return _db.Database.ExecuteSqlCommand(sql, serverId);
        }

        #endregion

        public void Dispose()
        {
            _db?.Dispose();
        }
    }

    public class ServerData
    {
        public int ServerID { get; set; }
        public string Name { get; set; }
        public string IP { get; set; }
        public int Port { get; set; }
        public int MaxUser { get; set; }
        public int UsersOnline { get; set; }
        public int Property { get; set; }
        public long BlockFunc { get; set; }
        public byte ImgNo { get; set; }
        public short ImgEvent { get; set; }
        public byte ServerType { get; set; }
        public sbyte Active { get; set; }
    }
}
