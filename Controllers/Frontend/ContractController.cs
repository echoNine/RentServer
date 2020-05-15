using Microsoft.AspNetCore.Mvc;
using RentServer.Models;

namespace RentServer.Controllers.Frontend
{
    [Route("frontend/[controller]")]
    [ApiController]
    public class ContractController : BaseController
    {
        // 获取所有合同 table 按类型
        [HttpGet("getContractList")]
        public JsonResult GetContractList(string type)
        {
            var sql = "select * from contract where contractStatus != 'invalid' and type='" + type + "' and userId=" + GetUserId();
            return Success(DataOperate.FindAll(sql));
        }
        
        // 检查是否已提交过续签合同 但还未处理
        [HttpGet("checkRenewal")]
        public bool CheckRenewal(int contractId)
        {
            var selSql = "select count(*) from renewalContractApply where applyStatus='todo' and contractId=" + contractId+" and userId="+GetUserId();
            return DataOperate.Sele(selSql) == 0;
        } 
        
        // 新增续签记录
        [HttpPost("applyRenewal")]
        public JsonResult ApplyHouseRenewal(ApplyRenewal applyRenewal)
        {
            var sql = "insert into renewalContractApply(contractId,type,houseId,userId) values(" +
                      applyRenewal.ContractId + ",'" + applyRenewal.Type + "'," + applyRenewal.HouseId +","+
                      GetUserId() + ")";
            return Success(DataOperate.Create(sql));
        }
    }

    public class ApplyRenewal
    {
        public int ContractId { get; set; }
        public int HouseId { get; set; }
        public string Type { get; set; }
    }
}