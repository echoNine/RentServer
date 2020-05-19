using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using RentServer.Models;

namespace RentServer.Controllers.Frontend
{
    [Route("frontend/[controller]")]
    [ApiController]
    public class HouseController : BaseController
    {
        // 首页获取所有房源的城市列表 input输入提示
        [HttpGet("getHouseCommunityList")]
        public JsonResult GetHouseCommunityList()
        {
            string sql = "select community from house where rentStatus='empty'";
            return Success(DataOperate.FindAll(sql));
        }
        
        // 通过tag选项 筛选目标房源
        [HttpGet("getHouseListByOptions")]
        public JsonResult GetHouseListByOptions(string atCity, string rentType, string priceStart, string priceEnd, string areaStart,
            string areaEnd, string payForm, string inputCommunity)
        {
            var where = new List<UserWhereItem>();

            if (atCity != null)
            {
                where.Add(new UserWhereItem("house.atCity", "=", atCity));
            }
            
            if (rentType != null)
            {
                where.Add(new UserWhereItem("house.rentType", "=", rentType));
            }

            if (priceStart != null)
            {
                where.Add(new UserWhereItem("house.rentPrice", ">=", priceStart));
            }

            if (priceEnd != null)
            {
                where.Add(new UserWhereItem("house.rentPrice", "<=", priceEnd));
            }

            if (areaStart != null)
            {
                where.Add(new UserWhereItem("house.area", ">=", areaStart));
            }

            if (areaEnd != null)
            {
                where.Add(new UserWhereItem("house.area", "<=", areaEnd));
            }

            if (payForm != null)
            {
                where.Add(new UserWhereItem("house.payForm", "=", payForm));
            }

            if (inputCommunity != null)
            {
                where.Add(new UserWhereItem("house.community", "like", '%' + inputCommunity + '%'));
            }

            string sql = "";
            string countSql = "";
            if (where.Count > 0)
            {
                countSql =
                    "select count(*) from house inner join user on house.userId = user.id inner join resOfHouse on house.id = resOfHouse.houseId where house.rentStatus = 'empty' and ";
                sql =
                    "select * from house inner join user on house.userId = user.id where house.rentStatus = 'empty' and ";
                foreach (var whereItem in where)
                {
                    countSql = countSql + whereItem.Column + " " + whereItem.Condition + " '" + whereItem.Value +
                               "' and ";
                    sql = sql + whereItem.Column + " " + whereItem.Condition + " '" + whereItem.Value + "' and ";
                }

                countSql = countSql.TrimEnd('a', 'n', 'd', ' ');
                sql = sql.TrimEnd('a', 'n', 'd', ' ');
            }
            else
            {
                countSql =
                    "select count(*) from house where rentStatus = 'empty'";
                sql =
                    "select * from house where rentStatus  = 'empty'";
            }

            return Success(new {totalCount = DataOperate.Sele(countSql), data = DataOperate.FindAll(sql)});
        }
        
        // 获取房源详情
        [HttpGet("getHouseDetailInfo")]
        public JsonResult GetHouseDetailInfo(int houseId)
        {
            bool liked;
            var sqlExits = "select count(*) from houseCollected where houseId=" + houseId + " and userId=" +
                           GetUserId();
            var count = Convert.ToInt32(DataOperate.Sele(sqlExits));
            var sql = "select * from house inner join contract on house.id = contract.houseId where house.id=" +
                      houseId;
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

        // 获取房源相关展示资源
        [HttpGet("getResOfHouse")]
        public JsonResult GetResOfHouse(int houseId, string resType)
        {
            string sql = "select * from resOfHouse where resType ='" + resType + "' and houseId=" + houseId;
            return Success(DataOperate.FindAll(sql));
        }
        
        // 收藏或去取消收藏房源
        [HttpPost("handleCollect")]
        public JsonResult HandleCollect(HandleCollect handleCollect)
        {
            string sql = "";
            if (handleCollect.Liked)
            {
                sql = "insert into houseCollected(houseId, userId) values (" + handleCollect.HouseId + ", " + GetUserId()+")";
            }
            else
            {
                sql = "delete from houseCollected where houseId=" + handleCollect.HouseId + " and userId=" + GetUserId();
            }

            return Success(DataOperate.Create(sql));
        }
        
        // 检查是否已约看过该房源 但还未处理
        [HttpPost("checkOrder")]
        public JsonResult CheckOrder(CheckOrder checkOrder)
        {
            var sqlExits = "select count(*) from houseOrdered where houseId=" + checkOrder.HouseId + " and userId=" +
                           GetUserId() + " and orderStatus='todo'";
            var count = Convert.ToInt32(DataOperate.Sele(sqlExits));
            return Success(count != 0);
        }
        
        // 申请约看房源
        [HttpPost("handleOrder")]
        public JsonResult HandleOrder(OrderInfo orderInfo)
        {
            string sql = "insert into houseOrdered(houseId,userId,orderTime,orderPhone) values (" + orderInfo.HouseId +
                         ", " + GetUserId() + ", '" + orderInfo.OrderTime + "', '" + orderInfo.OrderPhone + "')";
            return Success(DataOperate.Create(sql));

        }
        
        // 提交房源委托申请相关信息
        [HttpPost("createHouseCommission")]
        public JsonResult CreateHouseCommission(CreateHouseCommission createHouseCommission)
        {
            string sql =
                "insert into house(atCity,community,address,area,floor,layout,orientation,buildAt,rentType,toilet,balcony,houseNum,roomNum,userId) "+
                "values(@AtCity,@Community,@Address,@Area,@Floor,@Layout,@Orientation,@BuildAt,@RentType,@Toilet,@Balcony,@HouseNum,@RoomNum,@UserId)";
            MySqlConnection con = DataOperate.GetCon();
            MySqlCommand cmd = new MySqlCommand(sql, con);
            cmd.Parameters.Add(new MySqlParameter("@AtCity", MySqlDbType.VarChar, 10)).Value = createHouseCommission.AtCity;
            cmd.Parameters.Add(new MySqlParameter("@Community", MySqlDbType.VarChar, 10)).Value = createHouseCommission.Community;
            cmd.Parameters.Add(new MySqlParameter("@Address", MySqlDbType.VarChar, 50)).Value = createHouseCommission.Address;
            cmd.Parameters.Add(new MySqlParameter("@Area", MySqlDbType.Float)).Value = createHouseCommission.Area;
            cmd.Parameters.Add(new MySqlParameter("@Floor", MySqlDbType.VarChar, 10)).Value = createHouseCommission.Floor;
            cmd.Parameters.Add(new MySqlParameter("@Layout", MySqlDbType.VarChar, 10)).Value = createHouseCommission.Layout;
            cmd.Parameters.Add(new MySqlParameter("@Orientation", MySqlDbType.VarChar, 10)).Value = createHouseCommission.Orientation;
            cmd.Parameters.Add(new MySqlParameter("@BuildAt", MySqlDbType.Date)).Value = createHouseCommission.BuildAt;
            cmd.Parameters.Add(new MySqlParameter("@RentType", MySqlDbType.Enum)).Value = createHouseCommission.RentType;
            cmd.Parameters.Add(new MySqlParameter("@Toilet", MySqlDbType.Int32, 11)).Value = createHouseCommission.Toilet;
            cmd.Parameters.Add(new MySqlParameter("@Balcony", MySqlDbType.Int32, 11)).Value = createHouseCommission.Balcony;
            cmd.Parameters.Add(new MySqlParameter("@HouseNum", MySqlDbType.VarChar, 10)).Value = createHouseCommission.HouseNum;
            cmd.Parameters.Add(new MySqlParameter("@RoomNum", MySqlDbType.VarChar, 10)).Value = createHouseCommission.RoomNum;
            cmd.Parameters.Add(new MySqlParameter("@UserId", MySqlDbType.Int32, 11)).Value = GetUserId();
            try
            {
                cmd.ExecuteNonQuery();
                con.Close();
            }
            catch (System.Exception e)
            {
                con.Close();
                return Success(false);
            }

            return Success(true);   
        }

        // 获取收藏的房源列表
        [HttpGet("getCollectList")]
        public JsonResult GetCollectList()
        {
            var sql =
                "select * from houseCollected inner join house on house.id = houseCollected.houseId where houseCollected.userId=" +
                GetUserId();
            var addSql =
                "select count(*) as count,houseId from houseCollected where houseId in (select houseId from houseCollected where userId=" +
                GetUserId() + ") group by houseId";
            return Success(new
            {
                self = DataOperate.FindAll(sql),
                addNum = DataOperate.FindAll(addSql)
            });
        }
        
        // 取消收藏表格里的某房源
        [HttpPost("removeCollect")]
        public JsonResult RemoveCollect(RemoveCollect removeCollect)
        {
            string sql = "delete from houseCollected where houseId=" + removeCollect.HouseId + " and userId=" +
                         GetUserId();
            return Success(DataOperate.Update(sql));
        }
        
        // 获取约看的房源列表
        [HttpGet("getOrderList")]
        public JsonResult GetOrderList()
        {
            string sql =
                "select * from houseOrdered inner join house on houseOrdered.houseId = house.id inner join contract on houseOrdered.houseId = contract.houseId inner join admin on admin.id = contract.adminId where contract.type='withOwner' and houseOrdered.userId=" +
                GetUserId();
            return Success(DataOperate.FindAll(sql));
        }
        
        // 取消约看表格里的某房源
        [HttpPost("removeOrder")]
        public JsonResult RemoveOrder(RemoveOrder removeOrder)
        {
            string sql = "delete from houseOrdered where houseId=" + removeOrder.HouseId + " and userId=" + GetUserId();
            return Success(DataOperate.Update(sql));
        }
    }

    public class RemoveOrder
    {
        public int HouseId { get; set; }
    }

    public class RemoveCollect 
     { 
         public int HouseId { get; set; } 
     }

    public class CreateHouseCommission
    {
        public string AtCity { get; set; }
        public string Address { get; set; }
        public float Area { get; set; }
        public string Community { get; set; }
        public string Floor { get; set; }
        public string Layout { get; set; }
        public string Orientation { get; set; }
        public string BuildAt { get; set; }
        public int Toilet { get; set; }
        public int Balcony { get; set; }
        public string RentType { get; set; }
        public string HouseNum { get; set; }
        public string RoomNum { get; set; }
    }
    public class OrderInfo
    {
        public int HouseId { get; set; }
        public string OrderTime { get; set; }
        public string OrderPhone { get; set; }
    }
    
    public class CheckOrder
    {
        public int HouseId { get; set; }
    }

    public class HandleCollect
    {
        public bool Liked { get; set; }
        public int HouseId { get; set; }
    }
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
}