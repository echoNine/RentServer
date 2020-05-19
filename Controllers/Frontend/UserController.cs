using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MimeKit;
using MySql.Data.MySqlClient;
using RentServer.Models;
using RentServer.Setting;

namespace RentServer.Controllers.Frontend
{
    [Route("frontend/[controller]")]
    [ApiController]
    public class UserController : BaseController
    {
        private SmtpSettings SmtpSettings { get; set; }

        public UserController(IOptions<SmtpSettings> settings)
        {
            SmtpSettings = settings.Value;
        }

        // 用户注册
        [HttpPost("register")]
        public JsonResult Register(UserRegisterForm registerForm)
        {
            string MailCode = registerForm.Code;
            string str = HttpContext.Session.GetInt32("code").ToString();
            if (str == MailCode)
            {
                string sql = "insert into user(email,pwd) values(@Email,@Pwd)";
                MySqlConnection con = DataOperate.GetCon();
                MySqlCommand com = new MySqlCommand(sql, con);
                com = new MySqlCommand(sql, con);
                com.Parameters.Add(new MySqlParameter("@Email", MySqlDbType.VarChar, 16));
                com.Parameters["@Email"].Value = registerForm.Email;
                com.Parameters.Add(new MySqlParameter("@Pwd", MySqlDbType.VarChar, 32));
                com.Parameters["@Pwd"].Value = registerForm.Pwd;
                if (com.ExecuteNonQuery() > 0)
                {
                    // HttpContext.Session.SetString("email",registerForm.Email);
                    con.Close();
                    return Success("true");
                }
                else
                {
                    con.Close();
                    return Fail("注册失败，请重试..", 1004);
                }
            }
            else
            {
                return Fail("验证码输入有误，请重试..", 1005);
            }
        }

        // 注册用户需先发送邮箱验证码
        [HttpPost("send")]
        public JsonResult Send(UserSendForm sendForm)
        {
            Regex r1 = new Regex("^\\w+([-+.]\\w+)*@\\w+([-.]\\w+)*\\.\\w+([-.]\\w+)*$");
            Regex r2 = new Regex("^\\w{6,18}$");
            if (!r1.IsMatch(sendForm.Email))
            {
                return Fail("邮箱格式不正确", 1001);
            }

            if (!r2.IsMatch(sendForm.Pwd))
            {
                return Fail("密码只能包含字母、数字和下划线，长度在6~18之间", 1002);
            }

            string sql = "select count(*) from user where email=@Email";
            MySqlConnection con = DataOperate.GetCon();
            MySqlCommand cmd = new MySqlCommand(sql, con);
            cmd.Parameters.Add(new MySqlParameter("@Email", MySqlDbType.VarChar, 16));
            cmd.Parameters["@Email"].Value = sendForm.Email;
            if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
            {
                String to = sendForm.Email;
                Random random = new Random(Guid.NewGuid().GetHashCode());
                int code = random.Next(1000, 10000);
                string content = "您正在使用邮箱安全验证服务，您本次操作的验证码是：" + code;
                string strSmtpServer = SmtpSettings.Server;
                string strFrom = SmtpSettings.From;
                string strFromPass = SmtpSettings.Password;
                SendEmail(strSmtpServer, strFrom, strFromPass, to, "激活邮箱", content);
                HttpContext.Session.SetInt32("code", code);
                con.Close();

                return Success("true");
            }
            con.Close();
            return Fail("该用户已存在，请选择新账号注册..", 1003);
        }

        private static void SendEmail(string strSmtpServer, string strFrom, string strFromPass, string strto,
            string strSubject, string strBody)
        {
            BodyBuilder builder = new BodyBuilder();
            MimeMessage mail = new MimeMessage();
            mail.From.Add(new MailboxAddress("", strFrom));
            mail.To.Add(new MailboxAddress("", strto));
            mail.Subject = strSubject;
            builder.HtmlBody = "<html><body>" + strBody;
            builder.HtmlBody += "</body></html>";
            mail.Body = builder.ToMessageBody();

            MailKit.Net.Smtp.SmtpClient client = new MailKit.Net.Smtp.SmtpClient();
            client.Connect(strSmtpServer, 465, true);
            client.AuthenticationMechanisms.Remove("XOAUTH2");
            client.Authenticate(strFrom, strFromPass);
            client.Send(mail);
            client.Disconnect(true);
        }

        // 用户登录
        [HttpPost("login")]
        public JsonResult Login(UserLoginForm loginForm)
        {
            var sql = "select * from user where email=@Email and pwd=@Pwd";

            var mySqlConnection = DataOperate.GetCon();
            var cmd = new MySqlCommand(sql, mySqlConnection);
            cmd.Parameters.Add(new MySqlParameter("Email", MySqlDbType.VarChar, 16));
            cmd.Parameters["Email"].Value = loginForm.Email;
            cmd.Parameters.Add(new MySqlParameter("Pwd", MySqlDbType.VarChar, 32));
            cmd.Parameters["Pwd"].Value = loginForm.Pwd;
            
            var user = DataOperate.FindOne(cmd);
            if (user == null)
            {
                mySqlConnection.Close();
                return Fail("用户或密码输入有误，登录失败..", 1006);
            }
            
            HttpContext.Session.SetString("userEmail", loginForm.Email);
            HttpContext.Session.SetString("userId", user["id"].ToString());
            
            mySqlConnection.Close();
            return Success(user);

        }

        // 用户登录后 获取当前一户个人信息 渲染页面header
        [HttpGet("getUserInfo")]
        public JsonResult GetUserInfo()
        {
            string sql = "select * from user where id = " + GetUserId();

            return Success(DataOperate.FindOne(sql));
        }

        // 同上: 拿自我描述信息渲染个人信息页
        [HttpGet("selfDescTags")]
        public JsonResult SelfDescTags()
        {
            string sql = "select * from user inner join selfDesc on user.id = selfDesc.selfId where user.id = " +
                         GetUserId();
            return Success(DataOperate.FindAll(sql));
        }

        // 用户注册并登录成功后 弹出对话框 完善用户个人基本信息
        [HttpPost("perfectInfo")]
        public JsonResult PerfectInfo(CurrentUser currentUser)
        {
            string sql = "update user set name='" + currentUser.Name + "', sex='" + currentUser.Sex + "', cardNum='" +
                         currentUser.CardNum + "', phone='" + currentUser.Phone + "', native='" + currentUser.Native +
                         "', job='" + currentUser.Job +
                         "', avatar='" + currentUser.Avatar + "' where id= " + GetUserId();
            return Success(DataOperate.Update(sql));
        }

        // 申请房主资格
        [HttpPost("toBeOwner")]
        public JsonResult ToBeOwner(ToBeOwner toBeOwner)
        {
            string sql = "INSERT INTO applyTobeowner (userId, houseCity, houseCommunity) VALUES (" + GetUserId() +
                         ", '" +
                         toBeOwner.HouseCity + "', '" + toBeOwner.HouseCommunity + "');";

            return Success(DataOperate.Create(sql));
        }

        // 修改个人基本信息
        [HttpPost("updateInfo")]
        public JsonResult UpdateInfo(UpdateData updateData)
        {
            var sqls = new List<string>();

            sqls.Add("update user set name='" + updateData.Name + "', sex='" + updateData.Sex + "', phone='" +
                     updateData.Phone + "', native='" + updateData.Native + "', job='" + updateData.Job +
                     "', avatar='" + updateData.Avatar +
                     "' where id= '" + GetUserId() + "'");
            sqls.Add("delete from selfDesc where selfId=" + GetUserId());

            foreach (var updateDataSelfDescTag in updateData.SelfDescTags)
            {
                sqls.Add("insert into selfDesc (selfId,tag) values (" + GetUserId() + ",'" +
                         updateDataSelfDescTag + "')");
            }

            return Success(DataOperate.ExecTransaction(sqls.ToArray()));
        }
        
        // 修改登录密码
        [HttpPost("updatePwd")]
        public JsonResult UpdatePwd(UpdateUserPwd updatePwd)
        {
            string sql1 = "select * from user where id=" + GetUserId();
            var r = DataOperate.FindOne(sql1);
            if (r == null)
            {
                return Fail(false, 2001);
            }
            else
            {
                if (r["pwd"].ToString() == updatePwd.Pwd)
                {
                    string sql2 = "update user set pwd='" + updatePwd.NewPwd + "' where id=" + GetUserId();
                    return Success(DataOperate.Update(sql2));
                }
                else
                {
                    return Fail(false, 2002);
                }
            }
        }
    }

    public class UpdateUserPwd
    {
        public string Pwd { get; set; }
        public string NewPwd { get; set; }
    }

    public class UpdateData
    {
        public string Name { get; set; }
        public string Sex { get; set; }
        public string Phone { get; set; }
        public string Native { get; set; }
        public string Avatar { get; set; }
        public string Job { get; set; }

        public string[] SelfDescTags { get; set; }
    }


    public class ToBeOwner
    {
        public string HouseCity { get; set; }
        public string HouseCommunity { get; set; }
    }


    public class CurrentUser
    {
        public string Name { get; set; }
        public string Sex { get; set; }
        public string CardNum { get; set; }
        public string Phone { get; set; }
        public string Native { get; set; }
        public string Avatar { get; set; }
        public string Job { get; set; }
    }

    public class UserRegisterForm
    {
        public string Email { get; set; }
        public string Pwd { get; set; }
        public string Code { get; set; }
    }

    public class UserSendForm
    {
        public string Email { get; set; }
        public string Pwd { get; set; }
    }

    public class UserLoginForm
    {
        public string Email { get; set; }
        public string Pwd { get; set; }
    }
}