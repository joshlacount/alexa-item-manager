using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data;
using System.Data.SQLite;

namespace Item_Manager
{
    class SQLiteDatabase
    {
        private SQLiteConnection sqlite;

        public SQLiteDatabase(string pathToFile)
        {
            sqlite = new SQLiteConnection("Data Source=" + pathToFile + ";Version=3");
        }

        public DataTable selectQuery(string query)
        {
            SQLiteDataAdapter ad;
            DataTable dt = new DataTable();

            try
            {
                SQLiteCommand cmd;
                sqlite.Open();
                sqlite.EnableExtensions(true);
                sqlite.LoadExtension("SQLite.Interop.dll", "sqlite3_json_init");
                cmd = sqlite.CreateCommand();
                cmd.CommandText = query;
                ad = new SQLiteDataAdapter(cmd);
                ad.Fill(dt);
            }
            catch(SQLiteException ex)
            {
                Console.WriteLine("SQL Error: " + ex.Message);
            }
            sqlite.Close();
            return dt;
        }
    }
}
