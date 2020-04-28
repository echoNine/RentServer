using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Google.Protobuf;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RentServer.Models;

namespace RentServer.Controllers
{
    public class UserWhereItem
    {
        public string Condition { get; set; }
        public string Value { get; set; }

        public string Column { get; set; }

        public UserWhereItem(string column, string condition, string value)
        {
            Condition = condition;
            Value = value;
            Column = column;
        }
    }
    
    public class EntrustInfo
    {
        public string Name { get; set; }
        public string Phone { get; set; }
        public string City { get; set; }
        public string Community { get; set; }
    }

    [Route("[controller]")]
    [ApiController]
    public class UserHouseController : BaseController
    {
        // GET UserHouseList
        [HttpGet("userHouseList")]
        public JsonResult UserHouseList(string rentType, string priceStart, string priceEnd, string areaStart,
            string areaEnd, string payMethod, string inputInfo, int pageNum, int pageSize)
        {
            pageSize = pageSize == 0 ? 1 : pageSize;
            pageNum = pageNum == 0 ? 1 : pageNum;

            var where = new List<UserWhereItem>();

            if (rentType != null)
            {
                where.Add(new UserWhereItem("house.rentType", "=", rentType));
            }

            if (priceStart != null)
            {
                where.Add(new UserWhereItem("house.price", ">=", priceStart));
            }

            if (priceEnd != null)
            {
                where.Add(new UserWhereItem("house.price", "<=", priceEnd));
            }

            if (areaStart != null)
            {
                where.Add(new UserWhereItem("house.area", ">=", areaStart));
            }

            if (areaEnd != null)
            {
                where.Add(new UserWhereItem("house.area", "<=", areaEnd));
            }

            if (payMethod != null)
            {
                where.Add(new UserWhereItem("house.payMethod", "=", payMethod));
            }

            if (inputInfo != null)
            {
                where.Add(new UserWhereItem("house.community", "like", '%' + inputInfo + '%'));
            }

            string sql = "";
            string countSql = "";
            if (where.Count > 0)
            {
                countSql = "select count(*) from house where ";
                sql = "select * from house where ";
                foreach (var whereItem in where)
                {
                    countSql = countSql + whereItem.Column + " " + whereItem.Condition + " '" + whereItem.Value +
                               "' and ";
                    sql = sql + whereItem.Column + " " + whereItem.Condition + " '" + whereItem.Value + "' and ";
                }

                countSql = countSql.TrimEnd(new char[] {'a', 'n', 'd', ' '}) + " limit " + (pageNum - 1) * pageSize +
                           "," +
                           pageSize;
                sql = sql.TrimEnd(new char[] {'a', 'n', 'd', ' '}) + " limit " + (pageNum - 1) * pageSize + "," +
                      pageSize;
            }
            else
            {
                countSql = "select count(*) from house";
                sql = "select * from house limit " + (pageNum - 1) * pageSize + "," + pageSize;
            }

            return Success(new {totalCount = DataOperate.Sele(countSql), data = DataOperate.FindAll(sql)});
        }

        // GET houseCommunityList
        [HttpGet("houseCommunityList")]
        public JsonResult HouseCommunityList()
        {
            string sql = "select community from house";
            return Success(DataOperate.FindAll(sql));
        }
        
        // POST entrustHouse
        [HttpPost("entrustHouse")]
        public JsonResult EntrustHouse(EntrustInfo entrustInfo)
        {
            string sql = "insert into entrust (name, phone, city, community) values ('"+entrustInfo.Name+"','"+ entrustInfo.Phone+"','"+entrustInfo.City+"','"+entrustInfo.Community+"')";
            return Success(DataOperate.Update(sql));
        }
    }
}