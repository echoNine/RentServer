using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Text.RegularExpressions;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MimeKit;
using MySql.Data.MySqlClient;
using Renci.SshNet;
using RentServer.Models;
using RentServer.Setting;

namespace RentServer.Controllers
{
    public class AdminLoginForm
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
        public string NowTime { get; set; }
    }
    
    public class AdminSendForm
    {
        public string Email { get; set; }
        public string Pwd { get; set; }
    }
    
    public class CurrentAdmin
    {
        public string Email { get; set; } 
        public string Name { get; set; } 
        public string Sex { get; set; } 
        
        public string CardNum { get; set; }
        public string Phone { get; set; } 
        public string Native { get; set; } 
        public string Major { get; set; } 
        public string Avatar { get; set; }
    }
    
    public class UpdateForm
    {
        public string Email { get; set; } 
        public string Name { get; set; } 
        public string Sex { get; set; }
        public string Phone { get; set; } 
        public string Native { get; set; } 
        public string Avatar { get; set; }
        public string Major { get; set; }
        public string[] SelfDescTags { get; set; }
    }
    
    [Route("[controller]")]
    [ApiController]
    public class AdminController : BaseController
    {
        private SmtpSettings SmtpSettings { get; set; }
  
        public AdminController(IOptions<SmtpSettings> settings)
        {
            SmtpSettings = settings.Value;
        }
        
        // POST user
        [HttpPost("login")]
        public JsonResult Login (AdminLoginForm loginForm)
        {
            string sql = "select * from admin where email=@Email and pwd=@Pwd";
            MySqlConnection con = DataOperate.GetCon();
            con.Open();
            MySqlCommand cmd = new MySqlCommand(sql, con);
            cmd.Parameters.Add(new MySqlParameter("Email", MySqlDbType.VarChar, 16));
            cmd.Parameters["Email"].Value = loginForm.Email;
            cmd.Parameters.Add(new MySqlParameter("Pwd", MySqlDbType.VarChar, 32));
            cmd.Parameters["Pwd"].Value = loginForm.Pwd;
            MySqlDataReader sdr = cmd.ExecuteReader();
            if (sdr.Read())
            {
                return LoginSuccess("true");
            }
            return Fail("用户或密码输入有误，登录失败..",1006);
        }

        // POST user
        [HttpPost("register")]
        public JsonResult Register (AdminRegisterForm registerForm)
        {
            string MailCode = registerForm.Code;
            string LicenseKey = registerForm.LicenseKey;
            string str = HttpContext.Session.GetInt32("code").ToString();
            if (str == MailCode && LicenseKey == "fox072065yat")
            {
                string sql = "insert into admin(email,pwd,createdAt) values(@Email,@Pwd,'" + registerForm.NowTime + "')";
                MySqlConnection con = DataOperate.GetCon();
                con.Open();
                MySqlCommand com = new MySqlCommand(sql, con);
                com = new MySqlCommand(sql, con);
                com.Parameters.Add(new MySqlParameter("@Email", MySqlDbType.VarChar, 16));
                com.Parameters["@Email"].Value = registerForm.Email;
                com.Parameters.Add(new MySqlParameter("@Pwd", MySqlDbType.VarChar, 32));
                com.Parameters["@Pwd"].Value = registerForm.Pwd;
                if (com.ExecuteNonQuery() > 0)
                {
                    HttpContext.Session.SetString("email",registerForm.Email);
                    return Success("true");
                }
                else
                {
                    return Fail("注册失败，请重试..",1004);
                }
            }
            else
            {
                return Fail("验证码输入有误，请重试..",1005);
            }
        }
        
        // POST user
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
            con.Open();
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
                HttpContext.Session.SetInt32("code",code);
                return Success("true");
            }
            return Fail("该用户已存在，请选择新账号注册..",1003);
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
        
        // GET adminList
        [HttpGet("adminList")]
        public JsonResult AdminList(string email)
        {
            string sql = "select * from admin where email = '" + email + "'";
            return Success(DataOperate.FindAll(sql));
        }
        
        // GET selfDescTags
        [HttpGet("selfDescTags")]
        public JsonResult SelfDescTags(string email)
        {
            string sql = "select * from admin inner join selfdesc on admin.email = selfdesc.email where admin.email = '" + email + "'";
            return Success(DataOperate.FindAll(sql));
        }
        
        // Post perfectInfo
        [HttpPost("perfectInfo")]
        public JsonResult PerfectInfo(CurrentAdmin currentAdmin)
        {
            string sql = "update admin set username='" + currentAdmin.Name + "', sex='" + currentAdmin.Sex +
                         "', cardNum='" + currentAdmin.CardNum + "', phone='" + currentAdmin.Phone + "', native='" +
                         currentAdmin.Native + "', major='" + currentAdmin.Major + "', avatar='" + currentAdmin.Avatar +
                         "' where email= '" + currentAdmin.Email + "'";
            return Success(DataOperate.Update(sql));
        }
        
        // Post updateInfo
        [HttpPost("updateInfo")]
        public JsonResult UpdateInfo(UpdateForm updateForm)
        {
            var sqls = new List<string>();
            
            sqls.Add("update admin set username='"+updateForm.Name+"', sex='"+updateForm.Sex+"', phone='"+updateForm.Phone+"', native='"+updateForm.Native+"', major='"+updateForm.Major+"', avatar='"+updateForm.Avatar+"' where email= '"+updateForm.Email+"'");
            sqls.Add("delete from selfdesc where email='" + updateForm.Email + "'");
            
            foreach (var updateDataSelfDescTag in updateForm.SelfDescTags)
            {
                sqls.Add("insert into selfdesc (email,tag) values ('" + updateForm.Email + "','" +
                         updateDataSelfDescTag + "')");
            }
            return Success(DataOperate.ExecTransaction(sqls.ToArray()));
        }
        
        // GET houseOfAdmin
        [HttpGet("houseOfAdmin")]
        public JsonResult houseOfAdmin(string email)
        {
            string sql =
                "select * from house inner join ownercontract on house.id = ownercontract.houseId inner join admin on admin.id = ownercontract.adminId where admin.email='" +
                email + "'";
            return Success(DataOperate.FindAll(sql));
        }
        
        // Get contractOfAdmin
        [HttpGet("contractOfAdmin")]
        public JsonResult contractOfAdmin(string email)
        {
            string sql =
                "select * from ownercontract inner join admin on admin.id = ownercontract.adminId union all select * from tenantcontract inner join admin on admin.id = tenantcontract.adminId where admin.email='"+email+"' order by startAt desc";
            return Success(DataOperate.FindAll(sql));
        }
    }
}