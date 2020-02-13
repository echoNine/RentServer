using System;
using MySql.Data.MySqlClient;

namespace RentServer.Models
{
    public class BaseModel
    {
        
        public static string TableName = "admin";
        
        public static MySqlDataReader FindOne(string sql)
        {
            return DataOperate.FindOne(sql);
        }

//        public static MySqlDataReader FindAll()
//        {
//            
//        }

        public static bool Create(string sql)
        {
            return DataOperate.Create(sql);
        }

        public static bool Update(string sql)
        {
            return DataOperate.Update(sql);
        }

        public static bool Delete(string sql)
        {
            return DataOperate.Delete(sql);
        }
    }
}