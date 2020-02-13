using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using RentServer.Models;

namespace RentServer.Controllers
{
    public class LoginForm
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    [Route("[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        // POST user
        [HttpPost("login")]
        public ActionResult<string> Login (LoginForm loginForm)
        {
            
            string sql = "select * from admin";
            MySqlDataReader sdr = DataOperate.FindOne(sql);
            sdr.Read();

            return sdr["mgrTrueName"].ToString();
//            if (loginForm.Password == "123")
//            {
//                // cookie 处理
//                return true;
//            }
//            else
//            {
//                return false;
//            }

        }
        
        // POST user
        [HttpPost("register")]
        public ActionResult<IEnumerable<string>> Register (LoginForm loginForm)
        {
            return new string[] {loginForm.Username, loginForm.Password};
        }
    }
}