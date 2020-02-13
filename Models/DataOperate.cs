using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Configuration;
using System.Data;
using MySql.Data.MySqlClient;

namespace RentServer.Models
{
    public class DataOperate
    {
        public static MySqlConnection GetCon()
        {
            string connection = "server=localhost;user id=root;password=0720yat;database=rent; pooling=true;";
            
            return new MySqlConnection(connection);
        }

        public static MySqlDataReader FindOne(string sql)
        {
            MySqlConnection con = GetCon();         //创建数据库连接
            con.Open();         //打开数据库连接
            MySqlCommand com = new MySqlCommand(sql, con);        //创建SqlCommand对象

            return com.ExecuteReader();        //返回
        }

//        public static MySqlDataReader FindAll()
//        {
//            
//        }

        public static bool Create(string sql)
        {
            MySqlConnection con = GetCon();
            con.Open();
            
            MySqlCommand com = new MySqlCommand(sql, con);       //创建SQLCommand对象
            try
            {
                com.ExecuteNonQuery();       //执行SQL语句
            }
            catch (Exception e)
            {
                return false;       //返回布尔值 False
            }
            finally
            {
                con.Close();      //关闭数据库连接
            }

            return true;
        }

        public static bool Update(string sql)
        {
            MySqlConnection con = GetCon();
            con.Open();
            
            MySqlCommand com = new MySqlCommand(sql, con);       //创建SQLCommand对象
            try
            {
                com.ExecuteNonQuery();       //执行SQL语句
            }
            catch (Exception e)
            {
                return false;       //返回布尔值 False
            }
            finally
            {
                con.Close();      //关闭数据库连接
            }

            return true;   
        }

        public static bool Delete(string sql)
        {
            MySqlConnection con = GetCon();
            con.Open();
            
            MySqlCommand com = new MySqlCommand(sql, con);       //创建SQLCommand对象
            try
            {
                com.ExecuteNonQuery();       //执行SQL语句
            }
            catch (Exception e)
            {
                return false;       //返回布尔值 False
            }
            finally
            {
                con.Close();      //关闭数据库连接
            }

            return true;  
        }
    }
}