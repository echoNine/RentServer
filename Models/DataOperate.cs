using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;

namespace RentServer.Models
{
    public class DataOperate
    {
        private static MySqlConnection _conn;
        
        public static MySqlConnection GetCon()
        {
            if (_conn != null) return _conn;
            const string connection = "server=127.0.0.1;user id=root;password=test123;database=rent; pooling=true;CharSet=utf8;port=3307";
            
            _conn = new MySqlConnection(connection);
            
            _conn.Open();

            return _conn;
        }

        public static Dictionary<string, object> FindOne(MySqlCommand cmd)
        {
            MySqlDataReader sdr = cmd.ExecuteReader();
            if (sdr.Read())
            {
                var dic = new Dictionary<string, object>();
                for (var i = 0; i < sdr.FieldCount; i++)
                {
                    dic[sdr.GetName(i)] = sdr[sdr.GetName(i)];
                }
                sdr.Close();
                
                return dic;
            }

            return null;        //返回
        }
        
        public static Dictionary<string, object> FindOne(string sql)
        {
            MySqlConnection con = GetCon();         //创建数据库连接
            MySqlCommand cmd = new MySqlCommand(sql, con);        //创建SqlCommand对象
            
            MySqlDataReader sdr = cmd.ExecuteReader();
            if (sdr.Read())
            {
                var dic = new Dictionary<string, object>();
                for (var i = 0; i < sdr.FieldCount; i++)
                {
                    dic[sdr.GetName(i)] = sdr[sdr.GetName(i)];
                }
                sdr.Close();
                
                return dic;
            }

            return null;        //返回
        }

        public static DataSet FindAll(string sql)
        {
            MySqlConnection con = GetCon(); //创建数据库连接
            MySqlCommand cmd = new MySqlCommand(sql, con);
            MySqlDataAdapter ad = new MySqlDataAdapter(cmd);
            DataSet ds = new DataSet();
            ad.Fill(ds);
            return ds;
        }

        public static bool Create(string sql)
        {
            MySqlConnection con = GetCon();
            
            MySqlCommand com = new MySqlCommand(sql, con);       //创建SQLCommand对象
            try
            {
                com.ExecuteNonQuery();       //执行SQL语句
            }
            catch (System.Exception e)
            {
                return false;       //返回布尔值 False
            }

            return true;
        }

        public static bool Update(string sql)
        {
            MySqlConnection con = GetCon();
            
            MySqlCommand com = new MySqlCommand(sql, con);       //创建SQLCommand对象
            try
            {
                com.ExecuteNonQuery();       //执行SQL语句
            }
            catch (System.Exception e)
            {
                return false;       //返回布尔值 False
            }

            return true;   
        }

        public static bool Delete(string sql)
        {
            MySqlConnection con = GetCon();
            
            MySqlCommand com = new MySqlCommand(sql, con);       //创建SQLCommand对象
            try
            {
                com.ExecuteNonQuery();       //执行SQL语句
            }
            catch (System.Exception e)
            {
                return false;       //返回布尔值 False
            }

            return true;  
        }
        
        public static int Sele(string sql)
        {
            MySqlConnection con = GetCon();         //创建数据库连接
            MySqlCommand com = new MySqlCommand(sql, con);        //创建SqlCommand对象
            try
            {
                return Convert.ToInt32(com.ExecuteScalar());             //返回执行ExecuteScalar方法返回的值
            }
            catch (System.Exception ex)
            {
                return 0;            //返回0
            }
        }
        
        public static bool ExecTransaction(string[] sql)
        {
            MySqlConnection con = GetCon();        //创建数据库连接
            MySqlTransaction sTransaction = null;        //创建SqlTransaction对象
            try
            {
                MySqlCommand com = con.CreateCommand();            //创建SqlCommand对象
                sTransaction = con.BeginTransaction();            //设置开始事务
                com.Transaction = sTransaction;            //设置需要执行事务
                foreach (string sqlT in sql)
                {
                    com.CommandText = sqlT;                //设置SQL语句
                    if (com.ExecuteNonQuery() == -1)                //判断是否执行成功
                    {
                        sTransaction.Rollback();                    //设置事务回滚
                        return false;                    //返回布尔值False
                    }
                }
                sTransaction.Commit();            //提交事务
                return true;            //返回布尔值True
            }
            catch (System.Exception ex)
            {
                sTransaction.Rollback();            //设置事务回滚
                return false;            //返回布尔值False
            }
        }
    }
}