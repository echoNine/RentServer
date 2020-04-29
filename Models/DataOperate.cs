using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Configuration;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace RentServer.Models
{
    public class DataOperate
    {
        public static MySqlConnection GetCon()
        {
            string connection = "server=localhost;user id=root;password=0720yat;database=rent; pooling=true;CharSet=utf8";
            
            return new MySqlConnection(connection);
        }

        public static MySqlDataReader FindOne(string sql)
        {
            MySqlConnection con = GetCon();         //创建数据库连接
            con.Open();         //打开数据库连接
            MySqlCommand cmd = new MySqlCommand(sql, con);        //创建SqlCommand对象
            
            MySqlDataReader sdr = cmd.ExecuteReader();
            if (sdr.Read())
            {
                return sdr;
            }

            return sdr;        //返回
        }

        public static DataSet FindAll(string sql)
        {
            MySqlConnection con = GetCon(); //创建数据库连接
            con.Open(); //打开数据库连接
            MySqlCommand cmd = new MySqlCommand(sql, con);
            MySqlDataAdapter ad = new MySqlDataAdapter(cmd);
            DataSet ds = new DataSet();
            ad.Fill(ds);
            return ds;
        }

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
        
        public static int Sele(string sql)
        {
            MySqlConnection con = GetCon();         //创建数据库连接
            con.Open();         //打开数据库连接
            MySqlCommand com = new MySqlCommand(sql, con);        //创建SqlCommand对象
            try
            {
                return Convert.ToInt32(com.ExecuteScalar());             //返回执行ExecuteScalar方法返回的值
            }
            catch (Exception ex)
            {
                return 0;            //返回0
            }
            finally
            {
                con.Close();            //关闭数据库连接
            }
        }
        
        public static bool ExecTransaction(string[] sql)
        {
            MySqlConnection con = GetCon();        //创建数据库连接
            MySqlTransaction sTransaction = null;        //创建SqlTransaction对象
            try
            {
                con.Open();            //打开数据库连接
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
            catch (Exception ex)
            {
                sTransaction.Rollback();            //设置事务回滚
                return false;            //返回布尔值False
            }
            finally
            {
                con.Close();            //关闭数据库连接
            }
        }
    }
}