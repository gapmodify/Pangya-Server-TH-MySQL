using PangyaAPI.Auth;
using Connector.DataBase;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace PangyaAPI.Server
{
    public class ServerSettings
    {
        public string Key { get; set; } = "AuthConnectionKey";
        public AuthClientTypeEnum Type { get; set; }
        public uint UID { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string IP { get; set; }
        public uint Port { get; set; }
        public uint MaxPlayers { get; set; }
        public uint Property { get; set; }
        public short EventFlag { get; set; }
        public short ImgNo { get; set; }
        public long BlockFunc { get; set; }
        public string GameVersion { get; set; } = "845.01";
        public string AuthServer_Ip { get; set; } = "127.0.0.1";
        public int AuthServer_Port { get; set; } = 7997;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void InsertServer()
        {
            using (var _db = DbContextFactory.CreateX())
            {
                // Check if server exists
                var serverExists = _db.Database.SqlQuery<int>(
                    "SELECT COUNT(*) FROM pangya_server WHERE ServerID = @p0", UID).FirstOrDefault() > 0;

                if (serverExists)
                {
                    var query = $"UPDATE pangya_server SET Name = '{Name}', IP = '{IP}', Port = {Port}, MaxUser = {MaxPlayers}, Property = {Property}, BlockFunc = {BlockFunc}, ImgNo = {ImgNo}, ImgEvent = {EventFlag}, UsersOnline = 0, Active = 1 WHERE ServerID = {UID}";
                    _db.Database.ExecuteSqlCommand(query);
                }
                else
                {
                    var query = $"INSERT INTO pangya_server(ServerID, Name, IP, Port, MaxUser, UsersOnline, Property, BlockFunc, ImgNo, ImgEvent, ServerType, Active) VALUES({UID}, '{Name}', '{IP}', {Port}, {MaxPlayers}, 0, {Property}, {BlockFunc}, {ImgNo}, {EventFlag}, {Convert.ToInt32(Type)}, 1)";
                    _db.Database.ExecuteSqlCommand(query);
                }
            }
        }
        
        public void Update()
        {
            using (var _db = DbContextFactory.CreateX())
            {
                var serverExists = _db.Database.SqlQuery<int>(
                    "SELECT COUNT(*) FROM pangya_server WHERE ServerID = @p0", UID).FirstOrDefault() > 0;

                if (serverExists)
                {
                    var query = $"UPDATE pangya_server SET Name = '{Name}', IP = '{IP}', Port = {Port}, MaxUser = {MaxPlayers}, Property = {Property}, BlockFunc = {BlockFunc}, ImgNo = {ImgNo}, ImgEvent = {EventFlag}, UsersOnline = 0, Active = 1 WHERE ServerID = {UID}";
                    _db.Database.ExecuteSqlCommand(query);
                }
            }
        }
    }
}
