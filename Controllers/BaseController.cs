using System;
using Microsoft.AspNetCore.Http;
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

        protected string GetUserId()
        {
            var userId = HttpContext.Session.GetString("userId");
            if (userId == null)
            {
                // todo exception
            }

            return userId;
        }
        
        protected string GetUserEmail()
        {
            var userEmail = HttpContext.Session.GetString("userEmail");
            if (userEmail == null)
            {
                // todo exception
            }

            return userEmail;
        }
        
        protected string GetAdminEmail()
        {
            var userId = HttpContext.Session.GetString("adminEmail");
            if (userId == null)
            {
                // todo exception
            }

            return userId;
        }
        
        protected string GetAdminId()
        {
            var userId = HttpContext.Session.GetString("adminId");
            if (userId == null)
            {
                // todo exception
            }

            return userId;
        }
    }
}