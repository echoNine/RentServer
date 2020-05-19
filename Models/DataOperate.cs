using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using RentServer.Setting;

namespace RentServer.Models
{
    public class DataOperate
    {
        public static MySqlConnection GetCon()
        {
            var connection = AppSetting.Mysql.ConnectString;

            var conn = new MySqlConnection(connection);

            conn.Open();

            return conn;
        }

        public static Dictionary<string, object> FindOne(MySqlCommand cmd)
        {
            using (MySqlDataReader sdr = cmd.ExecuteReader())
            {
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
                
                return null; //返回
            }
        }

        public static Dictionary<string, object> FindOne(string sql)
        {
            MySqlConnection con = GetCon(); //创建数据库连接
            MySqlCommand cmd = new MySqlCommand(sql, con); //创建SqlCommand对象

            
            using (MySqlDataReader sdr = cmd.ExecuteReader())
            {
                if (sdr.Read())
                {
                    var dic = new Dictionary<string, object>();
                    for (var i = 0; i < sdr.FieldCount; i++)
                    {
                        dic[sdr.GetName(i)] = sdr[sdr.GetName(i)];
                    }

                    sdr.Close();

                    con.Close();
                    return dic;
                }
                
                con.Close();
                return null; //返回
            }
        }

        public static DataSet FindAll(string sql)
        {
            MySqlConnection con = GetCon(); //创建数据库连接
            MySqlCommand cmd = new MySqlCommand(sql, con);
            MySqlDataAdapter ad = new MySqlDataAdapter(cmd);
            DataSet ds = new DataSet();
            ad.Fill(ds);
            con.Close();
            return ds;
        }

        public static bool Create(string sql)
        {
            MySqlConnection con = GetCon();

            try
            {
                using (MySqlCommand cmd = new MySqlCommand(sql, con))
                {
                    cmd.ExecuteNonQuery(); //执行SQL语句
                    con.Close();

                    return true;
                }
            }
            catch (System.Exception e)
            {
                con.Close();
                return false; //返回布尔值 False
            }
        }

        public static bool Update(string sql)
        {
            MySqlConnection con = GetCon();

            try
            {
                using (MySqlCommand cmd = new MySqlCommand(sql, con))
                {
                    cmd.ExecuteNonQuery(); //执行SQL语句
                    con.Close();
                    return true;
                }
            }
            catch (System.Exception e)
            {
                con.Close();
                return false; //返回布尔值 False
            }
        }

        public static bool Delete(string sql)
        {
            MySqlConnection con = GetCon();

            using (MySqlCommand cmd = new MySqlCommand(sql, con))
            {
                try
                {
                    cmd.ExecuteNonQuery(); //执行SQL语句
                    con.Close();
                }
                catch (System.Exception e)
                {
                    con.Close();
                    return false; //返回布尔值 False
                }
            }

            return true;
        }

        public static int Sele(string sql)
        {
            MySqlConnection con = GetCon(); //创建数据库连接

            using (MySqlCommand cmd = new MySqlCommand(sql, con))
            {
                try
                {
                    var c = Convert.ToInt32(cmd.ExecuteScalar()); //返回执行ExecuteScalar方法返回的值
                    
                    con.Close();
                    return c;
                }
                catch (System.Exception e)
                {
                    con.Close();
                    return 0; //返回布尔值 False
                }
            }
        }

        public static bool ExecTransaction(string[] sql)
        {
            MySqlConnection con = GetCon(); //创建数据库连接
            MySqlTransaction sTransaction = null; //创建SqlTransaction对象
            try
            {
                using (MySqlCommand com = con.CreateCommand())
                {
                    sTransaction = con.BeginTransaction(); //设置开始事务
                    com.Transaction = sTransaction; //设置需要执行事务
                    foreach (string sqlT in sql)
                    {
                        com.CommandText = sqlT; //设置SQL语句
                        if (com.ExecuteNonQuery() == -1) //判断是否执行成功
                        {
                            sTransaction.Rollback(); //设置事务回滚
                            con.Close();
                            return false; //返回布尔值False
                        }
                    }

                    sTransaction.Commit(); //提交事务
                    con.Close();
                    return true; //返回布尔值True
                }
            }
            catch (System.Exception ex)
            {
                sTransaction.Rollback(); //设置事务回滚
                con.Close();
                return false; //返回布尔值False
            }
        }
    }
}