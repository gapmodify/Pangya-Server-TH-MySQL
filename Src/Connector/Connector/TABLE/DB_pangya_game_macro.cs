using Connector.DataBase;
using System;
using System.Linq;

namespace Connector.Table
{
    public class DB_pangya_game_macro : IDisposable
    {
        private readonly MySqlDbContext _db;

        public DB_pangya_game_macro(MySqlDbContext db)
        {
            _db = db;
        }

        public DB_pangya_game_macro() : this(DbContextFactory.CreateMySql())
        {
        }

        #region SELECT Methods

        public GameMacroData SelectByUID(int uid)
        {
            string sql = "SELECT * FROM pangya_game_macro WHERE UID = @p0";
            return _db.Database.SqlQuery<GameMacroData>(sql, uid).FirstOrDefault();
        }

        public bool ExistsByUID(int uid)
        {
            string sql = "SELECT COUNT(*) FROM pangya_game_macro WHERE UID = @p0";
            return _db.Database.SqlQuery<int>(sql, uid).FirstOrDefault() > 0;
        }

        #endregion

        #region INSERT Methods

        public int Insert(GameMacroData data)
        {
            string sql = @"INSERT INTO pangya_game_macro 
                (UID, Macro1, Macro2, Macro3, Macro4, Macro5, Macro6, Macro7, Macro8, Macro9, Macro10) 
                VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10)";

            return _db.Database.ExecuteSqlCommand(sql,
                data.UID,
                data.Macro1,
                data.Macro2,
                data.Macro3,
                data.Macro4,
                data.Macro5,
                data.Macro6,
                data.Macro7,
                data.Macro8,
                data.Macro9,
                data.Macro10
            );
        }

        #endregion

        #region UPDATE Methods

        public int Update(GameMacroData data)
        {
            string sql = @"UPDATE pangya_game_macro SET 
                Macro1 = @p0, Macro2 = @p1, Macro3 = @p2, Macro4 = @p3, Macro5 = @p4, 
                Macro6 = @p5, Macro7 = @p6, Macro8 = @p7, Macro9 = @p8, Macro10 = @p9 
                WHERE UID = @p10";

            return _db.Database.ExecuteSqlCommand(sql,
                data.Macro1,
                data.Macro2,
                data.Macro3,
                data.Macro4,
                data.Macro5,
                data.Macro6,
                data.Macro7,
                data.Macro8,
                data.Macro9,
                data.Macro10,
                data.UID
            );
        }

        public int UpdateMacro(int uid, int macroNumber, string macroText)
        {
            string sql = $"UPDATE pangya_game_macro SET Macro{macroNumber} = @p0 WHERE UID = @p1";
            return _db.Database.ExecuteSqlCommand(sql, macroText, uid);
        }

        #endregion

        #region DELETE Methods

        public int Delete(int uid)
        {
            string sql = "DELETE FROM pangya_game_macro WHERE UID = @p0";
            return _db.Database.ExecuteSqlCommand(sql, uid);
        }

        #endregion

        public void Dispose()
        {
            _db?.Dispose();
        }
    }

    public class GameMacroData
    {
        public int UID { get; set; }
        public string Macro1 { get; set; }
        public string Macro2 { get; set; }
        public string Macro3 { get; set; }
        public string Macro4 { get; set; }
        public string Macro5 { get; set; }
        public string Macro6 { get; set; }
        public string Macro7 { get; set; }
        public string Macro8 { get; set; }
        public string Macro9 { get; set; }
        public string Macro10 { get; set; }
    }
}
