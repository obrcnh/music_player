using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLitePCL;
using Windows.UI.Xaml.Media.Imaging;
using player.List;

namespace Project
{
    class DataStore
    {
        private static string DB_NAME = "SongssDataBase.db";
        private static string TABLE_NAME = "ItemTable";
        private static string SQL_CREATE_TABLE = "CREATE TABLE IF NOT EXISTS " + TABLE_NAME + " (Title TEXT,Singer TEXT,Album TEXT, File TEXT, Like TEXT);";
        private static string SQL_INSERT = "INSERT INTO " + TABLE_NAME + " VALUES(?,?,?,?,?);";
        private static string SQL_DELETE = "DELETE FROM " + TABLE_NAME + " WHERE Title = ?;";
        private static string SQL_UPDATE = "UPDATE " + TABLE_NAME + " SET Title = ?,Singer = ?, Album = ?, File = ?, Like = ? WHERE Title = ?;";
        private static string SQL_SELECT_ALL = "SELECT * FROM ItemTable;";
        SQLiteConnection conn = new SQLiteConnection(DB_NAME);
        public DataStore()
        {

        }
        public void init(ListItem temp)
        {
            /**
             * 创建表
             * */
            using (var statement = conn.Prepare(SQL_CREATE_TABLE))
            {
                statement.Step();
            }
             /**
              *遍历数据库中的item
              */
            using (var statement = conn.Prepare(SQL_SELECT_ALL))
            {
                while (statement.Step() != SQLiteResult.DONE)
                {
                    if ((string)statement[4] == "like")
                    {
                        temp.AddItem((string)statement[0], (string)statement[1], (string)statement[2], (string)statement[3], true, 0);
                    }
                    else if ((string)statement[4] == "unlike")
                    {
                        temp.AddItem((string)statement[0], (string)statement[1], (string)statement[2], (string)statement[3], false, 0);

                    }
                }
            }


        }
        public void insert(string title, string singer, string album, string file, bool like)
        {
            //数据库插入操作
            using (var statement = conn.Prepare(SQL_INSERT))
            {
                statement.Bind(1, title);
                statement.Bind(2, singer);
                statement.Bind(3, album);
                statement.Bind(4, file);
                if (like)
                {
                    statement.Bind(5, "like");
                }
                else
                {
                    statement.Bind(5, "unlike");
                }
                statement.Step();
            }
        }
        public void delete(string title)
        {
            using (var conn = new SQLiteConnection(DB_NAME))
            {
                using (var statement = conn.Prepare(SQL_DELETE))
                {
                    statement.Bind(1, title);
                    statement.Step();
                }
            }
        }
        public void update(string title, string singer, string album, string file, bool like)
        {
            using (var statement = conn.Prepare(SQL_UPDATE))
            {
                statement.Bind(1, title);
                statement.Bind(2, singer);
                statement.Bind(3, album);
                statement.Bind(4, file);
                if (like == true)
                {
                    statement.Bind(5, "like");
                }
                else
                {
                    statement.Bind(5, "unlike");
                }
                
                statement.Bind(6, title);
                statement.Step();
            }
        }
    }
}
