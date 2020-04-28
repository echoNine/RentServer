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
    public class TenantIdToDelete
    {
        public int Id { get; set; }
    }
    
    [Route("[controller]")]
    [ApiController]
    public class TenantController : BaseController
    {
        // GET tenantList
        [HttpGet("tenantList")]
        public JsonResult GetTenantList(int id, int pageNum, int pageSize)
        {
            int totalCount;
            pageSize = pageSize == 0 ? 1 : pageSize;
            pageNum = pageNum == 0 ? 1 : pageNum;
            string sql = "";
            
            if (id == 0)
            {
                totalCount = DataOperate.Sele("select count(*) from tenant");
                sql = "select * from tenant limit "+ (pageNum-1)*pageSize + "," +pageSize;;
            }
            else
            {
                totalCount = DataOperate.Sele("select count(*) from tenant where id=" + id);
                sql = "select * from tenant where id=" + id +" limit "+ (pageNum-1)*pageSize + "," +pageSize;
            }

            return Success(new {totalCount = totalCount, data = DataOperate.FindAll(sql)});
        }
        
        // Post deleteTenant
        [HttpPost("deleteTenant")]
        public bool DeleteTenant(TenantIdToDelete tenantIdToDelete)
        {
            string selSql = "select * from tenantcontract where userId=" + tenantIdToDelete.Id;
            string delSql = "delete from tenant where id=" + tenantIdToDelete.Id;
            DataSet allContract = DataOperate.FindAll(selSql);
            if ((allContract.Tables.Count == 1 && allContract.Tables[0].Rows.Count == 0) || getContractBool(allContract))
            {
                DataOperate.Update(delSql);
                return true;
            }
            return false;
        }
        public bool getContractBool(DataSet allContract)
        {
            bool delable = true;
            foreach (DataRow row in allContract.Tables[0].Rows)
            {
                if (row["contractStatus"].ToString() == "未到期")
                {
                    delable = false;
                }
            }
            return delable;
        }
    }
}