using System.Data;
using Microsoft.AspNetCore.Mvc;
using RentServer.Models;

namespace RentServer.Controllers.Backend
{
    [Route("backend/[controller]")]
    [ApiController]
    public class UserController : BaseController
    {
        // 获取所有用户列表（包括租户和房主）按类型type筛选
        [HttpGet("getUserList")]
        public JsonResult GetUserList(int id, int pageNum, int pageSize, string type)
        {
            int totalCount;
            pageSize = pageSize == 0 ? 1 : pageSize;
            pageNum = pageNum == 0 ? 1 : pageNum;
            string sql = "";

            if (id == 0)
            {
                totalCount = DataOperate.Sele("select count(*) from user where type = '" + type + "'");
                sql = "select * from user where type = '" + type + "' order by id desc limit " + (pageNum - 1) * pageSize + "," +
                      pageSize;
            }
            else
            {
                totalCount = DataOperate.Sele("select count(*) from user where type ='" + type + "' and id=" + id);
                sql = "select * from user where type = '" + type + "' and id=" + id + " order by id desc limit " +
                      (pageNum - 1) * pageSize + "," + pageSize;
            }

            return Success(new {totalCount = totalCount, data = DataOperate.FindAll(sql)});
        }
        
        // 获取所有用户
        [HttpGet("getAllUser")]
        public JsonResult GetAllUser()
        {
            string sql = "select * from user";

            return Success(DataOperate.FindAll(sql));
        }

        // 注销房主资格 owner -> tenant
        [HttpPost("deleteOwnerToTenant")]
        public bool DeleteOwnerToTenant(DeleteOwnerToTenant deleteOwnerToTenant)
        {
            var selSql = "select * from contract where userId=" + deleteOwnerToTenant.Id;
            string[] sqlT = new string[2];
            int i = 0;
            sqlT[i++] = "update house set houseStatus = 'unactivated' where userId=" + deleteOwnerToTenant.Id;
            sqlT[i] = "update user SET type = 'tenant' where id= " + deleteOwnerToTenant.Id;
            DataSet allContract = DataOperate.FindAll(selSql);
            if (allContract.Tables.Count == 1 && allContract.Tables[0].Rows.Count == 0)
            {
                DataOperate.Update(sqlT[i]);
                return true;
            }
            else if (GetContractBool(allContract))
            {
                return DataOperate.ExecTransaction(sqlT);
            }
            else
            {
                return false;
            }
        }

        // 注销租户
        [HttpPost("deleteTenant")]
        public bool DeleteTenant(TenantToDelete tenantToDelete)
        {
            string selSql = "select * from contract where userId=" + tenantToDelete.Id;
            string delSql = "update user set type='cancelled' where id=" + tenantToDelete.Id;
            DataSet allContract = DataOperate.FindAll(selSql);
            if ((allContract.Tables.Count == 1 && allContract.Tables[0].Rows.Count == 0) ||
                GetContractBool(allContract))
            {
                DataOperate.Update(delSql);
                return true;
            }

            return false;
        }

        public bool GetContractBool(DataSet allContract)
        {
            bool delable = true;
            foreach (DataRow row in allContract.Tables[0].Rows)
            {
                if (row["contractStatus"].ToString() == "undue")
                {
                    delable = false;
                }
            }

            return delable;
        }

        // 通过普通租户申请房主资格
        [HttpPost("passTobeOwnerApply")]
        public JsonResult PassTobeOwnerApply(PassTobeOwnerApply passTobeOwnerApply)
        {
            string sql = "select * from applyTobeowner where id = " + passTobeOwnerApply.Id + "";
            var record = DataOperate.FindOne(sql);

            string updateSql = "UPDATE applyTobeowner SET applyStatus = 'pass', adminId = " + GetAdminId() +
                               " WHERE id = " + passTobeOwnerApply.Id + " and applyStatus = 'todo'";

            string updateUserSql = "UPDATE user SET type = 'owner' WHERE id = " + record["userId"].ToString() + ";";
            
            return Success(DataOperate.ExecTransaction(new[] {updateSql, updateUserSql}));
        }
        
        // 拒绝普通租户申请房主资格
        [HttpPost("rejectTobeOwnerApply")]
        public JsonResult RejectTobeOwnerApply(RejectTobeOwnerApply rejectTobeOwnerApply)
        { 
            string sql = "update applyTobeowner set applyStatus = 'reject', adminId = " + GetAdminId() + " where id = " + rejectTobeOwnerApply.Id + ";";
            return Success(DataOperate.Update(sql));
        }
        
        // 获取所有房主资格申请列表
        [HttpGet("getTobeOwnerApplyList")]
        public JsonResult GetTobeOwnerApplyList(int pageNum, int pageSize)
        {
            int totalCount;
            pageSize = pageSize == 0 ? 1 : pageSize;
            pageNum = pageNum == 0 ? 1 : pageNum;

            totalCount = DataOperate.Sele("select count(*) from applyTobeowner");

            string sql = "select * from applyTobeowner order by id desc limit " + (pageNum - 1) * pageSize + "," + pageSize;

            return Success(new {totalCount = totalCount, data = DataOperate.FindAll(sql)});
        }

        // 获取房源对应的用户id
        [HttpGet("getUserIdByHouseId")]
        public JsonResult GetUserIdByHouseId(int houseId)
        {
            return Success(DataOperate.FindAll("select * from house where id=" + houseId));
        }
    }

    public class RejectTobeOwnerApply
    {
        public int Id { get; set; }

    }

    public class TenantToDelete
    {
        public int Id { get; set; }
    }

    public class DeleteOwnerToTenant
    {
        public int Id { get; set; }
    }

    public class PassTobeOwnerApply
    {
        public int Id { get; set; }

    }
}