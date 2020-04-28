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
    public class HouseIdToDelete
    {
        public int Id { get; set; }
    }
    public class WhereItem
    {
        public string Condition { get; set; }
        public string Value { get; set; }

        public string Column { get; set; }

        public WhereItem(string column, string condition, string value)
        {
            Condition = condition;
            Value = value;
            Column = column;
        }
    }

    public class CollectInfo
    {
        public bool Liked { get; set; }
        public int HouseId { get; set; }

        public string Email { get; set; }
        
        public string CreatedAt { get; set; }
    }
    
    public class DeleteCollectHouse
    {
        public int HouseId { get; set; }
        public string Email { get; set; }
    }
    
    public class DeleteOrderHouse
    {
        public int HouseId { get; set; }
        public string Email { get; set; }
    }

    public class OrderInfo
    {
        public int HouseId { get; set; }
        public string Email { get; set; }
        public string OrderTime { get; set; }
        public string OrderPhone { get; set; }
    }

    [Route("[controller]")]
    [ApiController]
    public class HouseController : BaseController
    {
        // GET houseList
        [HttpGet("houseList")]
        public JsonResult GetHouse(string userId, string userName, string rentType, string houseStatus,
            string priceStart, string priceEnd, int pageNum, int pageSize)
        {
            pageSize = pageSize == 0 ? 1 : pageSize;
            pageNum = pageNum == 0 ? 1 : pageNum;

            string gethouse = "select * from house";
            DataSet allhouse = DataOperate.FindAll(gethouse);
            foreach (DataRow row in allhouse.Tables[0].Rows)
            {
                string getOwnerContractStatus = "select * from ownercontract where houseId=" + row["id"];
                string getTenantContractStatus = "select * from tenantcontract where houseId=" + row["id"];
                DataSet allOwnerContractStatus = DataOperate.FindAll(getOwnerContractStatus);
                DataSet allTenantContractStatus = DataOperate.FindAll(getTenantContractStatus);

                bool ownerActivate = false;
                bool tenantActivate = false;
                string defaultStatus = "";
                foreach (DataRow row1 in allOwnerContractStatus.Tables[0].Rows)
                {
                    if (row1["contractStatus"].ToString() == "未到期")
                    {
                        ownerActivate = true; // 已激活=空闲/已租用
                        break;
                    }
                }

                foreach (DataRow row2 in allTenantContractStatus.Tables[0].Rows)
                {
                    if (row2["contractStatus"].ToString() == "未到期")
                    {
                        tenantActivate = true; // 已激活=空闲/已租用
                        break;
                    }
                }

                if (ownerActivate)
                {
                    if (tenantActivate)
                    {
                        defaultStatus = "已租用";
                    }
                    else
                    {
                        defaultStatus = "空闲";
                    }
                }
                else
                {
                    defaultStatus = "未激活";
                }

                DataOperate.Update("update house set status='" + defaultStatus + "' where id=" + row["id"]);
            }

            var where = new List<WhereItem>();

            if (userId != null)
            {
                where.Add(new WhereItem("owner.id", "=", userId));
            }

            if (userName != null)
            {
                where.Add(new WhereItem("owner.username", "=", userName));
            }

            if (rentType != null)
            {
                where.Add(new WhereItem("house.rentType", "=", rentType));
            }

            if (houseStatus != null)
            {
                where.Add(new WhereItem("house.status", "=", houseStatus));
            }

            if (priceStart != null)
            {
                where.Add(new WhereItem("house.price", ">=", priceStart));
            }

            if (priceEnd != null)
            {
                where.Add(new WhereItem("house.price", "<=", priceEnd));
            }

            string sql = "";
            string countSql = "";
            if (where.Count > 0)
            {
                countSql = "select count(*) from house inner join owner on house.owner = owner.id where ";
                sql = "select * from house inner join owner on house.owner = owner.id where ";
                foreach (var whereItem in where)
                {
                    countSql = countSql + whereItem.Column + " " + whereItem.Condition + " '" + whereItem.Value + "' and ";
                    sql = sql + whereItem.Column + " " + whereItem.Condition + " '" + whereItem.Value + "' and ";
                }
                countSql = countSql.TrimEnd(new char[] {'a', 'n', 'd', ' '}) + " limit " + (pageNum - 1) * pageSize + "," +
                      pageSize;
                sql = sql.TrimEnd(new char[] {'a', 'n', 'd', ' '}) + " limit " + (pageNum - 1) * pageSize + "," +
                      pageSize;
            }
            else
            {
                countSql = "select count(*) from house";
                sql = "select * from house limit " + (pageNum - 1) * pageSize + "," + pageSize;
            }

            return Success(new {totalCount =  DataOperate.Sele(countSql), data = DataOperate.FindAll(sql)});
        }
        
        // POST dropHouse
        [HttpPost("dropHouse")]
        public bool DropHouse(HouseIdToDelete houseIdToDelete)
        {
            string selsql = "select status from house where id='" + houseIdToDelete.Id + "'";
            string status = DataOperate.FindAll(selsql).Tables[0].Rows[0][0].ToString();
            string delsql = "delete from house where id='" + houseIdToDelete.Id + "'";
            if (status == "未激活")
            {
                DataOperate.Update(delsql);
                return true;
            }

            return false;
        }
        
        // GET houseDetail
        [HttpGet("houseDetail")]
        public JsonResult HouseDetail(int houseId, string email)
        {
            string sqlExits = "select count(*) from housecollect where houseId=" + houseId + " and email='" + email + "'";
            int count = Convert.ToInt32(DataOperate.Sele(sqlExits));
            string sql = "select * from house where id=" + houseId;
            bool liked;
            if (count == 0)
            {
                return Success(new
                {
                    self = DataOperate.FindAll(sql),
                    liked = false
                });
            }
            else
            {
                return Success(new
                {
                    self = DataOperate.FindAll(sql),
                    liked = true
                });
            }
        }

        // POST addCollect
        [HttpPost("addCollect")]
        public JsonResult AddCollect(CollectInfo collectInfo)
        {
            string sql = "";
            if (collectInfo.Liked)
            {
                sql = "insert into housecollect(houseId,email,createdAt) values (" + collectInfo.HouseId + ", '" +
                      collectInfo.Email + "', '" + collectInfo.CreatedAt + "')";
            }
            else
            {
                sql = "delete from housecollect where houseId=" + collectInfo.HouseId + " and email='" +
                      collectInfo.Email + "'";
            }
            return Success(DataOperate.Update(sql));
        }

        // GET likeList
        [HttpGet("likeList")]
        public JsonResult LikeList(string email, string houseId)
        {
            string sql = "select * from housecollect inner join house on house.id = housecollect.houseId where email='" + email + "'";
            string addSql = "select count(*) as count,houseId from housecollect where houseId in (select houseId from housecollect where email='" + email + "') group by houseId";
            return Success(new
            {
                self = DataOperate.FindAll(sql),
                addNum = DataOperate.FindAll(addSql)
            });
        }
        
        // POST delCollect
        [HttpPost("delCollect")]
        public JsonResult DelCollect(DeleteCollectHouse deleteCollectHouse)
        {
            string sql = "delete from housecollect where houseId=" + deleteCollectHouse.HouseId + " and email='" +
                         deleteCollectHouse.Email + "'";
            return Success(DataOperate.Update(sql));
        }

        // POST addOrder
        [HttpPost("addOrder")]
        public JsonResult AddOrder(OrderInfo orderInfo)
        {
            string sqlAdmin =
                "select username from admin inner join ownercontract on admin.id = ownercontract.adminId where ownercontract.houseId=" +
                orderInfo.HouseId;
            DataSet ds = DataOperate.FindAll(sqlAdmin);
            string adminName = ds.Tables[0].Rows[0]["username"].ToString();
            string sql = "insert into houseorder(houseId,email,orderTime,orderPhone,adminName) values (" + orderInfo.HouseId +
                         ", '" +
                         orderInfo.Email + "', '" + orderInfo.OrderTime + "', '" + orderInfo.OrderPhone + "', '"+adminName+ "')";
            return Success(DataOperate.Update(sql));
        }
        
        // GET orderList
        [HttpGet("orderList")]
        public JsonResult OrderList(string email)
        {
            string sql = "select * from houseorder inner join admin on houseorder.adminName = admin.username where houseorder.email='" + email+"'";
            return Success(DataOperate.FindAll(sql));
        }
        
        // POST delOrder
        [HttpPost("delOrder")]
        public JsonResult DelOrder(DeleteOrderHouse deleteOrderHouse)
        {
            string sql = "delete from houseorder where houseId=" + deleteOrderHouse.HouseId + " and email='" +
                         deleteOrderHouse.Email + "'";
            return Success(DataOperate.Update(sql));
        }

        // GET rentedList
        [HttpGet("rentedList")]
        public JsonResult RentedList(string email)
        {
            string sql =
                "select * from tenantcontract inner join user on tenantcontract.userId=user.id where user.email='" +
                email + "'";
            return Success(DataOperate.FindAll(sql));
        }
    }
}