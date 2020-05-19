using System.Data;
using Microsoft.AspNetCore.Mvc;
using RentServer.Models;

namespace RentServer.Controllers.Backend
{
    [Route("backend/[controller]")]
    [ApiController]
    public class FinanceController : BaseController
    {
        [HttpGet("getFinanceList")]
        public JsonResult GetFinanceList(string tranType, int pageNum, int pageSize)
        {
            pageSize = pageSize == 0 ? 1 : pageSize;
            pageNum = pageNum == 0 ? 1 : pageNum;

            var totalCount = DataOperate.Sele("select count(*) from transactions where tranType = '" + tranType + "'");

            DataSet ds = DataOperate.FindAll("select count(contractId) as rowCount from transactions where tranType = '" + tranType + "' group by contractId");
            string[] countArray = new string[ds.Tables[0].Rows.Count]; //把DataSet中的数据转换为一维数组
            for (int row = 0; row < ds.Tables[0].Rows.Count; row++)
            {
                countArray[row] = ds.Tables[0].Rows[row]["rowCount"].ToString();
            }

            var sql = "select * from transactions where tranType = '" + tranType + "' order by id desc limit " + (pageNum - 1) * pageSize +
                            "," + pageSize;

            return Success(new {totalCount = totalCount, countArray = countArray, data = DataOperate.FindAll(sql)});
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
}
   