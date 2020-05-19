using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using RentServer.Models;

namespace RentServer.Controllers.Backend
{
    [Route("backend/[controller]")]
    [ApiController]
    public class DataController : BaseController
    {
        [HttpGet("getUserCount")]
        public JsonResult GetUserCount()
        {
            return Success(DataOperate.Sele("select count(*) from user")); // 所有用户
        }
        
        [HttpGet("getHouseCount")]
        public JsonResult GetHouseCount()
        {
            return Success(DataOperate.Sele("select count(*) from house where rentStatus in ('activated','empty','rented')")); // 所有有效房源
        }
        
        [HttpGet("getContractCount")]
        public JsonResult GetContractCount()
        {
            return Success(DataOperate.Sele("select count(*) from contract")); // 所有签办的合同
        }
        
        [HttpGet("getFinanceCount")]
        public JsonResult GetFinanceCount()
        {
            return Success(DataOperate.Sele("select sum(account) from transactions where tranStatus='paid'")); // 所有已完成的交易paid
        }
        
        [HttpGet("getUserCountByType")]
        public JsonResult GetUserCountByType()
        {
            var ownerCount = DataOperate.Sele("select count(*) from user where type='owner'");
            var tenantCount = DataOperate.Sele("select count(*) from user where type='tenant'")+ ownerCount;
            var countList = new List<int> {tenantCount, ownerCount};
            var countArr = countList.ToArray();
            return Success(new {countArr});
        }

        [HttpGet("getHouseCountByType")]
        public JsonResult GetHouseCountByType()
        {
            var activatedCount = DataOperate.Sele("select count(*) from house where rentStatus = 'activated'");
            var emptyCount = DataOperate.Sele("select count(*) from house where rentStatus = 'empty'");
            var rentedCount = DataOperate.Sele("select count(*) from house where rentStatus = 'rented'");
            var countList = new List<int> {activatedCount, emptyCount, rentedCount};
            var countArr = countList.ToArray();
            return Success(new {countArr});
        }

        [HttpGet("getContractCountByType")]
        public JsonResult GetContractCountByType()
        { 
            var startMonth = DateTime.Now.AddMonths(+1).Month; // 开始月份
            var monthList = new List<int>();
            for (int i = 0; i < 12; i++,startMonth++)
            {
                if (startMonth <= 12)
                {
                    monthList.Add(startMonth);
                }
                else
                {
                    monthList.Add(startMonth-12);
                }
            }
            var monthArray = monthList.ToArray();
            var tenantData =
                DataOperate.FindAll("select MONTH(startAt) as month, count(startAt) as count from contract where type = 'withTenant' and DATE_FORMAT(contract.startAt,'%Y-%m')>DATE_FORMAT(date_sub(curdate(), interval 12 month),'%Y-%m') group by MONTH(startAt)");
            var ownerData =
                DataOperate.FindAll("select MONTH(startAt) as month, count(startAt) as count from contract where type = 'withOwner' and DATE_FORMAT(contract.startAt,'%Y-%m')>DATE_FORMAT(date_sub(curdate(), interval 12 month),'%Y-%m') group by MONTH(startAt)");
            return Success(new {tenantData, ownerData, monthArray});
        } 
        
        [HttpGet("getFinanceCountByType")]
        public JsonResult GetFinanceCountByType()
        { 
            var startMonth = DateTime.Now.AddMonths(+1).Month; // 开始月份
            var monthList = new List<int>();
            for (int i = 0; i < 12; i++,startMonth++)
            {
                if (startMonth <= 12)
                {
                    monthList.Add(startMonth);
                }
                else
                {
                    monthList.Add(startMonth-12);
                }
            }
            var monthArray = monthList.ToArray();
            var payData =
                DataOperate.FindAll("select MONTH(tranDate) as month, Sum(account) as account from transactions where tranType = 'withOwner' and DATE_FORMAT(transactions.tranDate,'%Y-%m')>DATE_FORMAT(date_sub(curdate(), interval 12 month),'%Y-%m') group by MONTH(tranDate)");
            var incomeData =
                DataOperate.FindAll("select MONTH(tranDate) as month, Sum(account) as account from transactions where tranType = 'withTenant' and DATE_FORMAT(transactions.tranDate,'%Y-%m')>DATE_FORMAT(date_sub(curdate(), interval 12 month),'%Y-%m') group by MONTH(tranDate)");
            return Success(new {payData, incomeData, monthArray});
        } 

    }
}
