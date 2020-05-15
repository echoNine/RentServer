using System.Collections.Generic;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using RentServer.Models;

namespace RentServer.Controllers.Frontend
{
    [Route("frontend/[controller]")]
    [ApiController]
    public class FinanceController : BaseController
    {
        
        [HttpGet("getFinanceList")]
        public JsonResult GetFinanceList(string tranType, string contractId, int pageNum, int pageSize)
        {
            pageSize = pageSize == 0 ? 1 : pageSize;
            pageNum = pageNum == 0 ? 1 : pageNum;
            
            var where = new List<WhereItem>();

            if (contractId != null)
            {
                where.Add(new WhereItem("transactions.contractId", "=", contractId));
            }
            var countSql = "select * from transactions where tranType ='" + tranType + "' and userId=" + GetUserId();
            var sql = "select * from transactions where tranType ='" + tranType + "' and userId=" + GetUserId();
            foreach (var whereItem in where)
            {            
                countSql = countSql + " and " + whereItem.Column + " " + whereItem.Condition + " '" + whereItem.Value + "'";
                sql = sql + " and " +  whereItem.Column + " " + whereItem.Condition + " '" + whereItem.Value + "'";
            }
            var totalCount = DataOperate.Sele(countSql);

            sql = sql + " order by id asc limit " + (pageNum - 1) * pageSize + "," + pageSize;
            return Success(new {totalCount = totalCount, data = DataOperate.FindAll(sql)});
        }

        [HttpPost("changePayStatus")]
        public JsonResult ChangePayStatus(ChangePayStatus changePayStatus)
        {
            return Success(DataOperate.Update("update transactions set tranStatus = 'paid', tranDate = '" + changePayStatus.CurrentTime +
                                              "' where id=" + changePayStatus.id));
        }
    }
    public class ChangePayStatus
    {
        public int id { get; set; }
        public string CurrentTime { get; set; }
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