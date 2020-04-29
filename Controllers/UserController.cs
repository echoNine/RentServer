using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
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
    public class UserLoginForm
    {
        public string Email { get; set; }
        public string Pwd { get; set; }
    }

    public class UserRegisterForm
    {
        public string Email { get; set; }
        public string Pwd { get; set; }
        public string Code { get; set; }

        public string NowTime { get; set; }
    }

    public class UserSendForm
    {
        public string Email { get; set; }
        public string Pwd { get; set; }
    }

    public class CurrentUser
    {
        public string Email { get; set; }
        public string Name { get; set; }
        public string Sex { get; set; }
        public string CardNum { get; set; }
        public string Phone { get; set; }
        public string Native { get; set; }
        public string Avatar { get; set; }
    }

    public class UpdateData
    {
        public string Email { get; set; }
        public string Name { get; set; }
        public string Sex { get; set; }
        public string Phone { get; set; }
        public string Native { get; set; }
        public string Avatar { get; set; }
        public string[] SelfDescTags { get; set; }
    }

    [Route("[controller]")]
    [ApiController]
    public class UserController : BaseController
    {
        private SmtpSettings SmtpSettings { get; set; }

        public UserController(IOptions<SmtpSettings> settings)
        {
            SmtpSettings = settings.Value;
        }

        // POST login
        [HttpPost("login")]
        public JsonResult Login(UserLoginForm loginForm)
        {
            string sql = "select * from user where email=@Email and pwd=@Pwd";
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
                HttpContext.Session.SetString("userEmail", loginForm.Email);
                HttpContext.Session.SetString("userId", sdr["id"].ToString());
                var user = new Dictionary<string, object>();
                for (var i = 0; i < sdr.FieldCount; i++)
                {
                    user[sdr.GetName(i)] = sdr[sdr.GetName(i)];
                }

                return Success(user);
            }

            return Fail("用户或密码输入有误，登录失败..", 1006);
        }

        // POST register
        [HttpPost("register")]
        public JsonResult Register(UserRegisterForm registerForm)
        {
            string MailCode = registerForm.Code;
            string str = HttpContext.Session.GetInt32("code").ToString();
            if (str == MailCode)
            {
                string sql = "insert into user(email,pwd,createdAt) values(@Email,@Pwd,'" + registerForm.NowTime + "')";
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
                    // HttpContext.Session.SetString("email",registerForm.Email);
                    return Success("true");
                }
                else
                {
                    return Fail("注册失败，请重试..", 1004);
                }
            }
            else
            {
                return Fail("验证码输入有误，请重试..", 1005);
            }
        }

        // POST send
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
                HttpContext.Session.SetInt32("code", code);
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

        // GET userInfo
        [HttpGet("userInfo")]
        public JsonResult UserInfo()
        {
            string sql = "select * from user where email = '" + GetUserEmail() + "'";

            var userReader = DataOperate.FindOne(sql);

            var user = new Dictionary<string, object>();
            for (var i = 0; i < userReader.FieldCount; i++)
            {
                user[userReader.GetName(i)] = userReader[userReader.GetName(i)];
            }

            return Success(user);
        }

        // GET selfDescTags
        [HttpGet("selfDescTags")]
        public JsonResult SelfDescTags()
        {
            string sql = "select * from user inner join selfdesc on user.email = selfdesc.email where user.email = '" + GetUserEmail() + "'";
            return Success(DataOperate.FindAll(sql));
        }

        // Post perfectInfo
        [HttpPost("perfectInfo")]
        public JsonResult PerfectInfo(CurrentUser currentUser)
        {
            string sql = "update user set name='" + currentUser.Name + "', sex='" + currentUser.Sex + "', cardNum='" +
                         currentUser.CardNum + "', phone='" + currentUser.Phone + "', native='" + currentUser.Native +
                         "', avatar='" + currentUser.Avatar + "' where email= '" + currentUser.Email + "'";
            return Success(DataOperate.Update(sql));
        }

        // Post updateInfo
        [HttpPost("updateInfo")]
        public JsonResult UpdateInfo(UpdateData updateData)
        {
            var sqls = new List<string>();

            sqls.Add("update user set name='" + updateData.Name + "', sex='" + updateData.Sex + "', phone='" +
                     updateData.Phone + "', native='" + updateData.Native + "', avatar='" + updateData.Avatar +
                     "' where email= '" + GetUserEmail() + "'");
            sqls.Add("delete from selfdesc where email='" + GetUserEmail() + "'");

            foreach (var updateDataSelfDescTag in updateData.SelfDescTags)
            {
                sqls.Add("insert into selfdesc (email,tag) values ('" + GetUserEmail() + "','" +
                         updateDataSelfDescTag + "')");
            }

            return Success(DataOperate.ExecTransaction(sqls.ToArray()));
        }

        // GET userList
        [HttpGet("userList")]
        public JsonResult GetUserList(int id, int pageNum, int pageSize, string type)
        {
            int totalCount;
            pageSize = pageSize == 0 ? 1 : pageSize;
            pageNum = pageNum == 0 ? 1 : pageNum;
            string sql = "";

            if (id == 0)
            {
                totalCount =
                    DataOperate.Sele(
                        "select count(*) from user where type = '" + type + "'");
                sql = "select * from user where type = '" + type + "' limit " +
                      (pageNum - 1) * pageSize + "," + pageSize;
                ;
            }
            else
            {
                totalCount =
                    DataOperate.Sele(
                        "select count(*) from user where type ='" + type + "' and id=" +
                        id);
                sql =
                    "select * from user where type = '" + type + "' and id=" +
                    id + " limit " + (pageNum - 1) * pageSize + "," + pageSize;
            }

            return Success(new {totalCount = totalCount, data = DataOperate.FindAll(sql)});
        }

        // Post toBeOwner
        [HttpPost("toBeOwner")]
        public JsonResult ToBeOwner(ToBeOwner toBeOwner)
        {
            string sql = "INSERT INTO tobeowner (userId, city, community) VALUES (" + GetUserId() + ", '" +
                         toBeOwner.City + "', '" + toBeOwner.Community + "');";

            return Success(DataOperate.Create(sql));
        }

        // GET toBeOwnerList
        [HttpGet("toBeOwnerList")]
        public JsonResult ToBeOwnerList(int pageNum, int pageSize)
        {
            int totalCount;
            pageSize = pageSize == 0 ? 1 : pageSize;
            pageNum = pageNum == 0 ? 1 : pageNum;

            totalCount = DataOperate.Sele("select count(*) from tobeowner");

            string sql = "select * from tobeowner limit " + (pageNum - 1) * pageSize + "," + pageSize;
            ;

            return Success(new {totalCount = totalCount, data = DataOperate.FindAll(sql)});
        }

        // POST passOwnerApply
        [HttpPost("passOwnerApply")]
        public JsonResult PassOwnerApply(PassOwnerApply passOwnerApply)
        {
            string sql = "select * from tobeowner where id = " + passOwnerApply.Id + "";
            var record = DataOperate.FindOne(sql);

            string updateSql = "UPDATE tobeowner SET status = 'done' WHERE id = " + passOwnerApply.Id + ";";

            string updateUserSql = "UPDATE user SET type = 'owner' WHERE id = " + record["userId"].ToString() + ";";

            return Success(DataOperate.ExecTransaction(new[] {updateSql, updateUserSql}));
        }
    }

    public class PassOwnerApply
    {
        public int Id { get; set; }
    }

    public class ToBeOwner
    {
        public string City { get; set; }
        public string Community { get; set; }
    }
}