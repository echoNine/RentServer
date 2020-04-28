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
    public class OwnerIdToDelete
    {
        public int Id { get; set; }
    }
    
    [Route("[controller]")]
    [ApiController]
    public class OwnerController : BaseController
    {
        // GET ownerList
        [HttpGet("ownerList")]
        public JsonResult GetOwnerList(int id, int pageNum, int pageSize)
        {
            int totalCount;
            pageSize = pageSize == 0 ? 1 : pageSize;
            pageNum = pageNum == 0 ? 1 : pageNum;
            string sql = "";
            
            if (id == 0)
            {
                totalCount = DataOperate.Sele("select count(*) from owner");
                sql = "select * from owner limit "+ (pageNum-1)*pageSize + "," +pageSize;;
            }
            else
            {
                totalCount = DataOperate.Sele("select count(*) from owner where id=" + id);
                sql = "select * from owner where id=" + id +" limit "+ (pageNum-1)*pageSize + "," +pageSize;
            }
            
            return Success(new {totalCount = totalCount, data = DataOperate.FindAll(sql)});
        }
        
        // Post deleteOwner
        [HttpPost("deleteOwner")]
        public bool DeleteOwner(OwnerIdToDelete ownerIdToDelete)
        {
            string selSql = "select * from ownercontract where userId=" + ownerIdToDelete.Id;
            string[] sqlT = new string[2];
            int i = 0;
            sqlT[i++] = "delete from house where owner=" + ownerIdToDelete.Id;
            sqlT[i] = "delete from owner where id=" + ownerIdToDelete.Id;
            DataSet allContract = DataOperate.FindAll(selSql);
            if (allContract.Tables.Count == 1 && allContract.Tables[0].Rows.Count == 0)
            {
                DataOperate.Update(sqlT[i]);
                return true;
            } else if (getContractBool(allContract))
            {
                return DataOperate.ExecTransaction(sqlT);
            }
            else
            {
                return false;
            }
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