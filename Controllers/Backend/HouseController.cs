using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using RentServer.Models;

namespace RentServer.Controllers.Backend
{
    [Route("backend/[controller]")]
    [ApiController]
    public class HouseController : BaseController
    {
        // 获取该管理员所经办的房源信息
        [HttpGet("getHouseListOfAdmin")]
        public JsonResult GetHouseListOfAdmin()
        {
            string sql =
                "select * from house inner join contract on house.id = contract.houseId where contract.type = 'withOwner' and contract.adminId=" +
                GetAdminId();
            return Success(DataOperate.FindAll(sql));
        }
        
        // 获取当前行房主所拥有的房源
        [HttpGet("getHouseListByOwnerId")]
        public JsonResult GetHouseListByOwnerId(string id)
        {
            string sql = "select * from house where rentStatus in ('empty', 'rented') and userId=" + id;
            return Success(DataOperate.FindAll(sql));
        }
        
        // 通过房主的房源委托申请
        [HttpPost("passHouseCommission")]
        public JsonResult PassHouseCommission(PassHouseCommission passHouseCommission)
        {
            var sql = "UPDATE house SET rentStatus = 'unactivated', adminId = " + GetAdminId() +
                      " WHERE id = " + passHouseCommission.Id;
            
            return Success(DataOperate.Update(sql));
        }
        
        // 拒绝房主的房源委托申请
        [HttpPost("rejectHouseCommission")]
        public JsonResult RejectHouseCommission(RejectHouseCommission rejectHouseCommission)
        {
            var sql = "UPDATE house SET rentStatus = 'reject', adminId = " + GetAdminId() +
                      " WHERE id = " + rejectHouseCommission.Id;
            
            return Success(DataOperate.Update(sql));
        }
        
        // 获取申请委托的房源列表
        [HttpPost("getHouseListToTable")]
        public JsonResult GetHouseListToTable(GetHouseListToTable getHouseListToTable)
        {
            var selSql1 = "select * from house";
            DataSet allHouse = DataOperate.FindAll(selSql1);
            
            
            string[] signed = { "activated", "empty" };
            // 检查房主合同状态 修改房源状态
            foreach (DataRow row in allHouse.Tables[0].Rows)
            {
                if (signed.Contains(row["rentStatus"].ToString()) )
                {
                    var selSql2 = "select * from contract where type='withOwner' and contractStatus = 'undue' and houseId=" + row["id"].ToString();
                    DataSet allHouseContract = DataOperate.FindAll(selSql2);
                    if (allHouseContract.Tables.Count == 1 && allHouseContract.Tables[0].Rows.Count == 0)
                    {
                        DataOperate.Update("update house set rentStatus='invalid' where id=" + row["id"]);
                    }
                }
            }

            var countSql = "select count(*) from house";
            var sql = "select * from house";

            if (getHouseListToTable.Status.Length > 0)
            {
                countSql += " where rentStatus in ('" + string.Join("','", getHouseListToTable.Status) + "')";
                sql += " where rentStatus in ('" + string.Join("','", getHouseListToTable.Status) + "')";
            }
            
            sql += " limit " + (getHouseListToTable.PageNum - 1) * getHouseListToTable.PageSize + "," + getHouseListToTable.PageSize;
            
            return Success(new {totalCount = DataOperate.Sele(countSql), data = DataOperate.FindAll(sql)});
        }
        
        // 通过合同id获取对应房源的信息进行填充  便于发布房源
        [HttpGet("getHouseInfoByContractId")]
        public JsonResult GetHouseByContractId(int contractId)
        {
            var sql = "select * from contract where id=" + contractId;
            var contract = DataOperate.FindOne(sql);
            var houseId = contract["houseId"].ToString();

            var houseSql = "select * from house where id=" + houseId;

            return Success(DataOperate.FindAll(houseSql));
        }
        
        // 发布房源
        [HttpPost("createHouse")]
        public JsonResult CreateHouse(CreateHouse createHouse)
        {
            var getHouseSql = "select * from contract where id=" + createHouse.ContractId;
            var contract = DataOperate.FindOne(getHouseSql);
            if (contract == null)
            {
                return Success(false);
            }
            var houseId = contract["houseId"].ToString();
            
            string sql =
                "update house set community=@Community, atCity=@AtCity, address=@Address, floor=@Floor, houseNum=@HouseNum, roomNum=@RoomNum, layout=@Layout, orientation=@Orientation, area=@Area, buildAt=@BuildAt, toilet=@Toilet, balcony=@Balcony, rentType=@RentType, intro=@Intro where id=@Id";
            MySqlConnection con = DataOperate.GetCon();
            var mySqlTransaction = con.BeginTransaction();
            MySqlCommand cmd = new MySqlCommand(sql, con);
            cmd.Parameters.Add(new MySqlParameter("@AtCity", MySqlDbType.VarChar, 10)).Value = createHouse.AtCity;
            cmd.Parameters.Add(new MySqlParameter("@Community", MySqlDbType.VarChar, 20)).Value = createHouse.Community;
            cmd.Parameters.Add(new MySqlParameter("@Address", MySqlDbType.VarChar,50)).Value = createHouse.Address;
            cmd.Parameters.Add(new MySqlParameter("@Floor", MySqlDbType.VarChar, 10)).Value = createHouse.Floor;
            cmd.Parameters.Add(new MySqlParameter("@HouseNum", MySqlDbType.VarChar, 10)).Value = createHouse.HouseNum;
            cmd.Parameters.Add(new MySqlParameter("@RoomNum", MySqlDbType.VarChar, 10)).Value = createHouse.RoomNum;
            cmd.Parameters.Add(new MySqlParameter("@Layout", MySqlDbType.VarChar,10)).Value = createHouse.Layout;
            cmd.Parameters.Add(new MySqlParameter("@Orientation", MySqlDbType.VarChar,10)).Value = createHouse.Orientation;
            cmd.Parameters.Add(new MySqlParameter("@Area", MySqlDbType.Float)).Value = createHouse.Area;
            cmd.Parameters.Add(new MySqlParameter("@BuildAt", MySqlDbType.Date)).Value = createHouse.BuildAt;
            cmd.Parameters.Add(new MySqlParameter("@Toilet", MySqlDbType.Int32, 11)).Value = createHouse.Toilet;
            cmd.Parameters.Add(new MySqlParameter("@Balcony", MySqlDbType.Int32, 11)).Value = createHouse.Balcony;
            cmd.Parameters.Add(new MySqlParameter("@RentType", MySqlDbType.Enum)).Value = createHouse.RentType;
            cmd.Parameters.Add(new MySqlParameter("@Intro", MySqlDbType.Text)).Value = createHouse.Intro;
            cmd.Parameters.Add(new MySqlParameter("@Id", MySqlDbType.Int32)).Value = houseId;
            cmd.Transaction = mySqlTransaction;
            try
            {
                cmd.ExecuteNonQuery();
                var cmds = new List<MySqlCommand>();
                foreach (var resImg in createHouse.ResImgs)
                {
                    var resSql =
                        "INSERT INTO resOfHouse (resType, resPath, houseId) VALUES (@ResType, @ResPath, @HouseId);";
                    MySqlCommand resCmd = new MySqlCommand(resSql, con);
                    resCmd.Parameters.Add(new MySqlParameter("@ResType", MySqlDbType.Enum)).Value = "img";
                    resCmd.Parameters.Add(new MySqlParameter("@ResPath", MySqlDbType.VarChar, 100)).Value = resImg;
                    resCmd.Parameters.Add(new MySqlParameter("@HouseId", MySqlDbType.Int32, 11)).Value = houseId;
                    cmds.Add(resCmd);
                }

                var coverSql = "update house set cover='" + createHouse.ResImgs[0] + "' where id=" + houseId;
                MySqlCommand coverSqlCmd = new MySqlCommand(coverSql, con);
                cmds.Add(coverSqlCmd);

                if (createHouse.ResVideo != "")
                {
                    var resSql =
                        "INSERT INTO resOfHouse (resType, resPath, houseId) VALUES (@ResType, @ResPath, @HouseId);";
                    MySqlCommand resCmd = new MySqlCommand(resSql, con);
                    resCmd.Parameters.Add(new MySqlParameter("@ResType", MySqlDbType.Enum)).Value = "video";
                    resCmd.Parameters.Add(new MySqlParameter("@ResPath", MySqlDbType.VarChar, 100)).Value = createHouse.ResVideo;
                    resCmd.Parameters.Add(new MySqlParameter("@HouseId", MySqlDbType.Int32, 11)).Value = houseId;
  
                    cmds.Add(resCmd);
                } 
                
                if (createHouse.Res3D != "")
                {
                    var resSql =
                        "INSERT INTO resOfHouse (resType, resPath, houseId) VALUES (@ResType, @ResPath, @HouseId);";
                    MySqlCommand resCmd = new MySqlCommand(resSql, con);
                    resCmd.Parameters.Add(new MySqlParameter("@ResType", MySqlDbType.Enum)).Value = "3d";
                    resCmd.Parameters.Add(new MySqlParameter("@ResPath", MySqlDbType.VarChar, 100)).Value = createHouse.Res3D;
                    resCmd.Parameters.Add(new MySqlParameter("@HouseId", MySqlDbType.Int32, 11)).Value = houseId;
  
                    cmds.Add(resCmd);
                }
                foreach (var mySqlCommand in cmds)
                {
                    mySqlCommand.Transaction = mySqlTransaction;
                    mySqlCommand.ExecuteNonQuery();
                }
                
                var updateHouseSql = "UPDATE house SET rentStatus = 'empty'" +
                          " WHERE id = " + houseId + " and rentStatus = 'activated'";
                MySqlCommand updateHouseCmd = new MySqlCommand(updateHouseSql, con);
                updateHouseCmd.Transaction = mySqlTransaction;
                updateHouseCmd.ExecuteNonQuery();

                mySqlTransaction.Commit(); 
            }
            catch (System.Exception e)
            {
                mySqlTransaction.Rollback(); 
                return Success(false);
            }

            return Success(true);   
        }
        
        // 通过tag选项 筛选目标房源
        [HttpGet("getHouseListByOptions")]
        public JsonResult GetHouseListByOptions(string userId, string userName, string atCity, string rentType, string rentStatus,
            string priceStart, string priceEnd, int pageNum, int pageSize)
        {
            pageSize = pageSize == 0 ? 1 : pageSize;
            pageNum = pageNum == 0 ? 1 : pageNum;

            var where = new List<WhereItem>();

            if (userId != null)
            {
                where.Add(new WhereItem("user.id", "=", userId));
            }

            if (userName != null)
            {
                where.Add(new WhereItem("user.name", "=", userName));
            }
            
            if (atCity != null)
            {
                where.Add(new WhereItem("house.atCity", "=", atCity));
            }

            if (rentType != null)
            {
                where.Add(new WhereItem("house.rentType", "=", rentType));
            }

            if (rentStatus != null)
            {
                where.Add(new WhereItem("house.rentStatus", "=", rentStatus));
            }

            if (priceStart != null)
            {
                where.Add(new WhereItem("house.rentPrice", ">=", priceStart));
            }

            if (priceEnd != null)
            {
                where.Add(new WhereItem("house.rentPrice", "<=", priceEnd));
            }

            string sql = "";
            string countSql = "";
            if (where.Count > 0)
            {
                countSql = "select count(*) from house inner join user on house.userId = user.id inner join resOfHouse on house.id = resOfHouse.houseId where house.rentStatus in ('activated', 'empty', 'rented') and ";
                sql = "select * from house inner join user on house.userId = user.id where house.rentStatus in ('activated', 'empty', 'rented') and ";
                foreach (var whereItem in where)
                {
                    countSql = countSql + whereItem.Column + " " + whereItem.Condition + " '" + whereItem.Value + "' and ";
                    sql = sql + whereItem.Column + " " + whereItem.Condition + " '" + whereItem.Value + "' and ";
                }
                countSql = countSql.TrimEnd('a', 'n', 'd', ' ') + " limit " + (pageNum - 1) * pageSize + "," +
                      pageSize;
                sql = sql.TrimEnd('a', 'n', 'd', ' ') + " limit " + (pageNum - 1) * pageSize + "," +
                      pageSize;
            }
            else
            {
                countSql = "select count(*) from house where rentStatus in ('activated', 'empty', 'rented')";
                sql = "select * from house where rentStatus in ('activated', 'empty', 'rented') limit " + (pageNum - 1) * pageSize + "," + pageSize;
            }

            return Success(new {totalCount =  DataOperate.Sele(countSql), data = DataOperate.FindAll(sql)});
        }

        // 获取房源详情
        [HttpGet("getHouseDetailInfo")]
        public JsonResult GetHouseDetailInfo(int houseId)
        {
            string sql = "select * from house inner join contract on house.id = contract.houseId where house.id=" + houseId;
            return Success(DataOperate.FindAll(sql));
        }

        // 获取房源相关展示资源
        [HttpGet("getResOfHouse")]
        public JsonResult GetResOfHouse(int houseId, string resType)
        {
            string sql = "select * from resOfHouse where resType ='" + resType + "' and houseId=" + houseId;
            return Success(DataOperate.FindAll(sql));
        }
        
        // 获取未激活的房源列表 得到houseId列表 填充下拉框 选择房源进行签约
        [HttpGet("getUnactivatedHouseList")]
        public JsonResult GetUnactivatedHouseList()
        {
            string sql = "select * from house where rentStatus='unactivated'";
            return Success(DataOperate.FindAll(sql));
        }
        
        // 获取空闲的房源列表 得到houseId列表 填充下拉框 选择房源进行签约
        [HttpGet("getEmptyHouseList")]
        public JsonResult GetEmptyHouseList()
        {
            string sql = "select * from house where rentStatus='empty'";
            return Success(DataOperate.FindAll(sql));
        }
        
        // 通过房源id获取空闲的房源出租价格
        [HttpGet("getPriceByHouseId")]
        public JsonResult GetPriceByHouseId(int houseId)
        {
            string sql = "select * from house where id="+houseId;
            return Success(DataOperate.FindAll(sql));
        }
        
        // 获取所有的约看申请
        [HttpGet("getOrderList")]
        public JsonResult GetOrderList()
        {
            string sql = "select * from houseOrdered";
            return Success(DataOperate.FindAll(sql));
        }
        
        // 通过房源约看申请
        [HttpPost("passHouseOrder")]
        public JsonResult PassHouseOrder(PassHouseOrder passHouseOrder)
        {
            var sql = "UPDATE houseOrdered SET orderStatus = 'done', adminId = " + GetAdminId() +
                      " WHERE id = " + passHouseOrder.Id;
            
            return Success(DataOperate.Update(sql));
        }
        
        // 拒绝房主的房源约看申请
        [HttpPost("rejectHouseOrder")]
        public JsonResult RejectHouseOrder(RejectHouseOrder rejectHouseOrder)
        {
            var sql = "UPDATE houseOrdered SET orderStatus = 'reject', adminId = " + GetAdminId() +
                      " WHERE id = " + rejectHouseOrder.Id;
            
            return Success(DataOperate.Update(sql));
        }
    }
    
    
    public class CreateHouse
    {
        public int ContractId { get; set; }
        public string AtCity { get; set; }
        public string Community { get; set; }
        public string Address { get; set; }
        public string Floor { get; set; }
        public string HouseNum { get; set; }
        public string RoomNum { get; set; }
        public string Layout { get; set; }
        public string Orientation { get; set; }
        public float Area { get; set; }
        public string BuildAt { get; set; }
        public int Toilet { get; set; }
        public int Balcony { get; set; }
        public string RentType { get; set; }
        public string Intro { get; set; }
        public string[] ResImgs { get; set; }
        public string ResVideo { get; set; }
        public string Res3D { get; set; }
    }
    
    public class GetHouseListToTable
    { 
        public int PageNum { get; set; }
        public int PageSize { get; set; }
        public string[] Status { get; set; }
    }

    public class RejectHouseCommission 
    {
        public int Id { get; set; } 
    }

    public class PassHouseCommission
    {
        public int Id { get; set; }
    }
    
    public class RejectHouseOrder
    {
        public int Id { get; set; } 
    }

    public class PassHouseOrder
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
}