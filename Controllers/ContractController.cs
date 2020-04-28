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
    public class ApplyTenantParent
    {
        public int ParentNum { get; set; }
    }
    
    [Route("[controller]")]
    [ApiController]
    public class ContractController : BaseController
    {
        // POST applyTenantRenewal
        [HttpPost("applyTenantRenewal")]
        public JsonResult applyTenantRenewal(ApplyTenantParent applyTenantParent)
        {
            string sql = "insert into applytenant(parentNum) values (" + applyTenantParent.ParentNum + ")";
            return Success(DataOperate.Update(sql));
        }
    }
}