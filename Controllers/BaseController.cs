using System;
using Microsoft.AspNetCore.Mvc;

namespace RentServer.Controllers
{
    public class BaseController : ControllerBase
    {
        public JsonResult LoginSuccess(object value)
        {
            return new JsonResult(new {success = true, msg = value, token = Guid.NewGuid().ToString().Replace("-", "")});
        }
        public JsonResult Success(object value)
        {
            return new JsonResult(new {success = true, msg = value});
        }
        
        public JsonResult Fail(object value, int errorCode)
        {
            return new JsonResult(new {success = false, msg = value, error = errorCode});
        }
    }
}