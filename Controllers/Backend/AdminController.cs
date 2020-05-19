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

namespace RentServer.Controllers.Backend
{
    [Route("backend/[controller]")]
    [ApiController]
    public class AdminController : BaseController
    {
        private SmtpSettings SmtpSettings { get; set; }

        public AdminController(IOptions<SmtpSettings> settings)
        {
            SmtpSettings = settings.Value;
        }
        
        // 注册管理员
        [HttpPost("register")]
        public JsonResult Register(AdminRegisterForm registerForm)
        {
            string MailCode = registerForm.Code;
            string LicenseKey = registerForm.LicenseKey;
            string str = HttpContext.Session.GetInt32("code").ToString();
            if (str == MailCode && LicenseKey == "fox072065yat")
            {
                string sql = "insert into admin(email,pwd) values(@Email,@Pwd)";
                MySqlConnection con = DataOperate.GetCon();
                MySqlCommand com = new MySqlCommand(sql, con);
                com = new MySqlCommand(sql, con);
                com.Parameters.Add(new MySqlParameter("@Email", MySqlDbType.VarChar, 16));
                com.Parameters["@Email"].Value = registerForm.Email;
                com.Parameters.Add(new MySqlParameter("@Pwd", MySqlDbType.VarChar, 32));
                com.Parameters["@Pwd"].Value = registerForm.Pwd;
                if (com.ExecuteNonQuery() > 0)
                {
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
        
        // 注册管理员需先发送邮箱验证码
        [HttpPost("send")]
        public JsonResult Send(AdminSendForm sendForm)
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

            string sql = "select count(*) from admin where email=@Email";
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
        
        // 管理员登录
        [HttpPost("login")]
        public JsonResult Login(AdminLoginForm loginForm)
        {
            string sql = "select * from admin where email=@Email and pwd=@Pwd";
            var mySqlConnection = DataOperate.GetCon();
            var cmd = new MySqlCommand(sql, mySqlConnection);
            cmd.Parameters.Add(new MySqlParameter("Email", MySqlDbType.VarChar, 16));
            cmd.Parameters["Email"].Value = loginForm.Email;
            cmd.Parameters.Add(new MySqlParameter("Pwd", MySqlDbType.VarChar, 32));
            cmd.Parameters["Pwd"].Value = loginForm.Pwd;

            var admin = DataOperate.FindOne(cmd);
            if (admin == null) return Fail("用户或密码输入有误，登录失败..", 1006);
            
            HttpContext.Session.SetString("adminEmail", loginForm.Email);
            HttpContext.Session.SetString("adminId", admin["id"].ToString());

            mySqlConnection.Close();
            return LoginSuccess("true");

        }
        
        // 管理员登录后 获取当前管理员个人信息 渲染页面header
        [HttpGet("getAdminInfo")]
        public JsonResult GetAdminInfo()
        {
            string sql = "select * from admin where id = " + GetAdminId();
            return Success(DataOperate.FindAll(sql));
        }
        
        // 同上: 拿自我描述信息渲染个人信息页
        [HttpGet("selfDescTags")]
        public JsonResult SelfDescTags()
        {
            string sql = "select * from admin inner join selfDesc on admin.id = selfDesc.selfId where admin.id = " +
                         GetAdminId();
            return Success(DataOperate.FindAll(sql));
        }
        
        // 管理员注册并登录成功后 弹出对话框 完善管理员个人基本信息
        [HttpPost("perfectInfo")]
        public JsonResult PerfectInfo(CurrentAdmin currentAdmin)
        {
            string sql = "update admin set name='" + currentAdmin.Name + "', sex='" + currentAdmin.Sex +
                         "', cardNum='" + currentAdmin.CardNum + "', phone='" + currentAdmin.Phone + "', native='" +
                         currentAdmin.Native + "', major='" + currentAdmin.Major + "', avatar='" + currentAdmin.Avatar +
                         "' where id= " + GetAdminId();
            return Success(DataOperate.Update(sql));
        }
        
        // 修改个人基本信息
        [HttpPost("updateInfo")]
        public JsonResult UpdateInfo(UpdateForm updateForm)
        {
            var sqls = new List<string>();

            sqls.Add("update admin set name='" + updateForm.Name + "', sex='" + updateForm.Sex + "', phone='" +
                     updateForm.Phone + "', native='" + updateForm.Native + "', major='" + updateForm.Major +
                     "', avatar='" + updateForm.Avatar + "' where id= " + GetAdminId());
            sqls.Add("delete from selfDesc where selfId=" + GetAdminId());

            foreach (var updateDataSelfDescTag in updateForm.SelfDescTags)
            {
                sqls.Add("insert into selfDesc (selfId,tag) values (" + GetAdminId() + ",'" +
                         updateDataSelfDescTag + "')");
            }

            return Success(DataOperate.ExecTransaction(sqls.ToArray()));
        }
        
        // 修改登录密码
        [HttpPost("updatePwd")]
        public JsonResult UpdatePwd(UpdatePwd updatePwd)
        {
            string sql1 = "select * from admin where id=" + GetAdminId();
            var r = DataOperate.FindOne(sql1);
            if (r == null)
            {
                return Fail(false, 2001);
            }
            else
            {
                if (r["pwd"].ToString() == updatePwd.Pwd)
                {
                    string sql2 = "update admin set pwd='" + updatePwd.NewPwd + "' where id=" + GetAdminId();
                    return Success(DataOperate.Update(sql2));
                }
                else
                {
                    return Fail(false, 2002);
                }
            }
        }
    }
    
    public class UpdatePwd
    {
        public string Pwd { get; set; }
        public string NewPwd { get; set; }
    }

    public class UpdateForm
    {
        public string Name { get; set; }
        public string Sex { get; set; }
        public string Phone { get; set; }
        public string Native { get; set; }
        public string Avatar { get; set; }
        public string Major { get; set; }
        public string[] SelfDescTags { get; set; }
    }
    
    public class AdminSendForm
    {
        public string Email { get; set; }
        public string Pwd { get; set; }
    }
    
    public class AdminRegisterForm
    {
        public string Email { get; set; }
        public string Pwd { get; set; }
        public string Code { get; set; }
        public string LicenseKey { get; set; }
    }

    public class CurrentAdmin
    {
        public string Name { get; set; }
        public string Sex { get; set; }
        public string CardNum { get; set; }
        public string Phone { get; set; }
        public string Native { get; set; }
        public string Major { get; set; }
        public string Avatar { get; set; }
    }

    public class AdminLoginForm
    {
        public string Email { get; set; }
        public string Pwd { get; set; }
    }
}