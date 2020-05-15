using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using RentServer.Models;

namespace RentServer.Controllers.Backend
{
    [Route("backend/[controller]")]
    [ApiController]
    public class ContractController : BaseController
    {
        // 获取该管理员所经办的合同信息
        [HttpGet("getContractListOfAdmin")]
        public JsonResult GetContractListOfAdmin()
        {
            string sql = "select * from contract inner join admin on admin.id = contract.adminId where admin.id=" +
                         GetAdminId() + " order by startAt desc";
            return Success(DataOperate.FindAll(sql));
        }

        // 通过房源Id查找合同Id列表 填充房源信息项 发布房源
        [HttpGet("getContractIdByHouseId")]
        public JsonResult GetContractIdByHouseId(int houseId)
        {
            var sql = "select * from contract where type = 'withOwner' and houseId=" + houseId;
            return Success(DataOperate.FindAll(sql));
        }

        // 获取所有未到期的房主合同 将其合同id列表 填充下拉框选项
        [HttpGet("getContractList")]
        public JsonResult GetContractList()
        {
            string sql = "select * from contract where type = 'withOwner' and contractStatus = 'undue'";

            return Success(DataOperate.FindAll(sql));
        }

        // 获取所有房主合同
        [HttpGet("getOwnerContractList")]
        public JsonResult GetOwnerContractList()
        {
            var sql = "select * from contract where type='withOwner' and contractStatus != 'invalid'";
            DataSet allContract = DataOperate.FindAll(sql);

            // 检查合同状态并修改
            foreach (DataRow row in allContract.Tables[0].Rows)
            {
                var expired = (DateTime.Parse(row["endAt"].ToString()) <
                               DateTime.Parse(DateTime.Now.ToString()));
                var contractStatus = expired ? "fallDue" : "undue";
                var newSql = "update contract set contractStatus='" + contractStatus + "' where id=" +
                             row["id"].ToString();
                DataOperate.Update(newSql);
            }

            var sqlParent = "select * from contract where type = 'withOwner' and parentNum is null and contractStatus != 'invalid'";
            var sqlChildren = "select * from contract where type = 'withOwner' and parentNum is not null and contractStatus != 'invalid'";

            var parentList = new List<OwnerContract>();

            DataSet parentRows = DataOperate.FindAll(sqlParent);
            DataSet childrenRows = DataOperate.FindAll(sqlChildren);

            foreach (DataRow row in parentRows.Tables[0].Rows)
            {
                var ownerContract = new OwnerContract();
                foreach (var dataColumn in row.Table.Columns)
                {
                    ownerContract.SetAttribute(dataColumn.ToString(), row[dataColumn.ToString()].ToString());
                }

                ownerContract.Children = GetChildrenByParentId(childrenRows, ownerContract.Id.ToString());

                parentList.Add(ownerContract);
            }

            return Success(parentList);
        }

        // 获取所有租户合同
        [HttpGet("getTenantContractList")]
        public JsonResult GetTenantContractList()
        {
            var sql = "select * from contract where type='withTenant' and contractStatus != 'invalid'";
            DataSet allContract = DataOperate.FindAll(sql);

            // 检查合同状态并修改
            foreach (DataRow row in allContract.Tables[0].Rows)
            {
                var expired = (DateTime.Parse(row["endAt"].ToString()) <
                               DateTime.Parse(DateTime.Now.ToString()));
                var contractStatus = expired ? "fallDue" : "undue";
                string[] sqlT = new string[2];
                int i = 0;
                sqlT[0] = "update contract set contractStatus='" + contractStatus + "' where id=" +
                             row["id"].ToString();
                
                sqlT[1] = "update house set rentStatus='empty' where id=" + row["houseId"].ToString();
                if (expired)
                {
                    DataOperate.ExecTransaction(sqlT);
                }
                else
                {
                    DataOperate.Update(sqlT[0]);
                }
            }

            var sqlParent = "select * from contract where type = 'withTenant' and parentNum is null and contractStatus != 'invalid'";
            var sqlChildren = "select * from contract where type = 'withTenant' and parentNum is not null and contractStatus != 'invalid'";

            var parentList = new List<OwnerContract>();

            DataSet parentRows = DataOperate.FindAll(sqlParent);
            DataSet childrenRows = DataOperate.FindAll(sqlChildren);

            foreach (DataRow row in parentRows.Tables[0].Rows)
            {
                var ownerContract = new OwnerContract();
                foreach (var dataColumn in row.Table.Columns)
                {
                    ownerContract.SetAttribute(dataColumn.ToString(), row[dataColumn.ToString()].ToString());
                }

                ownerContract.Children = GetChildrenByParentId(childrenRows, ownerContract.Id.ToString());

                parentList.Add(ownerContract);
            }

            return Success(parentList);
        }

        private static List<OwnerContract> GetChildrenByParentId(DataSet childrenRows, string parentNum)
        {
            var childrenList = new List<OwnerContract>();

            foreach (DataRow row in childrenRows.Tables[0].Rows)
            {
                if (row["parentNum"].ToString() == parentNum)
                {
                    var ownerContract = new OwnerContract();
                    foreach (var dataColumn in row.Table.Columns)
                    {
                        ownerContract.SetAttribute(dataColumn.ToString(), row[dataColumn.ToString()].ToString());
                    }

                    childrenList.Add(ownerContract);
                }
            }

            return childrenList;
        }

        // 签订与租户的合同
        [HttpPost("createTenantContract")]
        public JsonResult CreateTenantContract(CreateTenantContract createTenantContract)
        {
            var selSql = "select * from house where id=" + createTenantContract.HouseId;
            if (DataOperate.FindOne(selSql) == null)
            {
                return Success(false);
            }

            string parentNum = null;

            var contractOneSql = "select * from contract where type = 'withTenant' and houseId=" +
                                 createTenantContract.HouseId;
            var contractOne = DataOperate.FindOne(contractOneSql);
            if (contractOne != null)
            {
                parentNum = contractOne["id"].ToString();
            }

            var addSql =
                "insert into contract(adminId,type,startAt,endAt,contractPic,houseId,userId,parentNum,rentPrice,payForm) values(@AdminId, @Type, @StartAt, @EndAt, @ContractPic, @HouseId, @UserId, @ParentNum, @RentPrice, @PayForm)";
            var upSql = "update house set rentPrice='" + createTenantContract.RentPrice + "' , payForm='" +
                        createTenantContract.PayForm + "', rentStatus='rented' where id=" +
                        createTenantContract.HouseId;

            MySqlConnection con = DataOperate.GetCon();
            var tenantContractCmd = new MySqlCommand(addSql, con);
            tenantContractCmd.Parameters.Add(new MySqlParameter("@AdminId", MySqlDbType.Int32, 11)).Value = GetAdminId();
            tenantContractCmd.Parameters.Add(new MySqlParameter("@Type", MySqlDbType.Enum)).Value = "withTenant";
            tenantContractCmd.Parameters.Add(new MySqlParameter("@StartAt", MySqlDbType.Date)).Value =
                createTenantContract.StartAt;
            tenantContractCmd.Parameters.Add(new MySqlParameter("@EndAt", MySqlDbType.Date)).Value =
                createTenantContract.EndAt;
            tenantContractCmd.Parameters.Add(new MySqlParameter("@RentPrice", MySqlDbType.Decimal, 11)).Value =
                createTenantContract.RentPrice;
            tenantContractCmd.Parameters.Add(new MySqlParameter("@PayForm", MySqlDbType.Enum)).Value =
                createTenantContract.PayForm;
            tenantContractCmd.Parameters.Add(new MySqlParameter("@HouseId", MySqlDbType.Int32, 11)).Value =
                createTenantContract.HouseId;
            tenantContractCmd.Parameters.Add(new MySqlParameter("@ContractPic", MySqlDbType.VarChar, 100)).Value =
                createTenantContract.ContractPic;
            tenantContractCmd.Parameters.Add(new MySqlParameter("@UserId", MySqlDbType.Int32, 11)).Value =
                createTenantContract.UserId;
            tenantContractCmd.Parameters.Add(new MySqlParameter("@ParentNum", MySqlDbType.Int32, 11)).Value = parentNum;

            var sTransaction = con.BeginTransaction();

            try
            {
                tenantContractCmd.Transaction = sTransaction;
                tenantContractCmd.ExecuteNonQuery();
                var contractId = tenantContractCmd.LastInsertedId;

                // 生成交易账单
                var transactionTenantCmds = new List<MySqlCommand>();
                DateTimeFormatInfo dtFormat = new DateTimeFormatInfo {ShortDatePattern = "yyyy-MM-dd"};
                DateTime startAt = Convert.ToDateTime(createTenantContract.StartAt, dtFormat);
                DateTime endAt = Convert.ToDateTime(createTenantContract.EndAt, dtFormat);
                var first = true;
                while (startAt < endAt)
                {
                    var transactionSql =
                        "INSERT INTO transactions (contractId, userId, account, payForm, tranDate, startDate, endDate, tranStatus, tranType, adminId) VALUES (@ContractId, @UserId, @Account, @PayForm, @TranDate, @StartDate, @EndDate, @TranStatus, @TranType, @AdminId);";
                
                    var transactionCmd = new MySqlCommand(transactionSql, con) {Transaction = sTransaction};
                    transactionCmd.Parameters.Add(new MySqlParameter("@ContractId", MySqlDbType.Int32, 11)).Value = contractId;
                    transactionCmd.Parameters.Add(new MySqlParameter("@UserId", MySqlDbType.Int32, 11)).Value = createTenantContract.UserId;
                    transactionCmd.Parameters.Add(new MySqlParameter("@PayForm", MySqlDbType.Enum)).Value = createTenantContract.PayForm;
                    transactionCmd.Parameters.Add(new MySqlParameter("@StartDate", MySqlDbType.Date)).Value = startAt.ToString("yyyy-MM-dd");

                    DateTime endDate;
                    
                    if (createTenantContract.PayForm == "byYear")
                    {
                        endDate = startAt.AddYears(1);
                        
                        if (endDate > endAt)
                        {
                            // 当年总天数
                            var days = (float)(endDate - startAt).Days;
                            // ReSharper disable once PossibleLossOfFraction
                            var realDays = (float)(endAt - startAt).Days;

                            var rentPrice = Math.Floor(realDays / days * createTenantContract.RentPrice * 12);
                            transactionCmd.Parameters.Add(new MySqlParameter("@Account", MySqlDbType.Decimal)).Value = rentPrice;
                            endDate = endAt;
                        }
                        else
                        {
                            transactionCmd.Parameters.Add(new MySqlParameter("@Account", MySqlDbType.Decimal)).Value = createTenantContract.RentPrice * 12;
                        }
                    }
                    else
                    {
                        endDate = startAt.AddMonths(1);
                        if (endDate > endAt)
                        {
                            // 当月总天数
                            var days = (float)(endDate - startAt).Days;
                            // ReSharper disable once PossibleLossOfFraction
                            var realDays = (float)(endAt - startAt).Days;

                            var rentPrice = Math.Floor(realDays / days * createTenantContract.RentPrice);
                            transactionCmd.Parameters.Add(new MySqlParameter("@Account", MySqlDbType.Decimal)).Value = rentPrice;
                            endDate = endAt;
                        }
                        else
                        {
                            transactionCmd.Parameters.Add(new MySqlParameter("@Account", MySqlDbType.Decimal)).Value = createTenantContract.RentPrice;
                        }
                    }

                    transactionCmd.Parameters.Add(new MySqlParameter("@EndDate", MySqlDbType.Date)).Value = endDate.ToString("yyyy-MM-dd");
                    transactionCmd.Parameters.Add(new MySqlParameter("@TranType", MySqlDbType.Enum)).Value = "withTenant";
                    transactionCmd.Parameters.Add(new MySqlParameter("@AdminId", MySqlDbType.Int32, 100)).Value = GetAdminId();
                
                    if (first)
                    {
                        transactionCmd.Parameters.Add(new MySqlParameter("@TranDate", MySqlDbType.Date)).Value = DateTime.Now.ToString("yyyy-MM-dd");
                        transactionCmd.Parameters.Add(new MySqlParameter("@TranStatus", MySqlDbType.Enum)).Value = "paid";
                        first = false;
                    }
                    else
                    {
                        transactionCmd.Parameters.Add(new MySqlParameter("@TranDate", MySqlDbType.Date)).Value = null;
                        transactionCmd.Parameters.Add(new MySqlParameter("@TranStatus", MySqlDbType.Enum)).Value = "unpaid";
                    }
                    
                    transactionTenantCmds.Add(transactionCmd);
                    startAt = createTenantContract.PayForm == "byYear" ? startAt.AddYears(1) : startAt.AddMonths(1);
                }
                foreach (var mySqlCommand in transactionTenantCmds)
                {
                    mySqlCommand.ExecuteNonQuery();
                }

                var upCmd = new MySqlCommand(upSql, con) {Transaction = sTransaction};
                upCmd.ExecuteNonQuery();
                sTransaction.Commit();
            }
            catch (System.Exception e)
            {
                sTransaction.Rollback();
                return Success(false);
            }
            
            return Success(true);
        }

        // 签订与房主的合同
        [HttpPost("createHouseContract")]
        public JsonResult CreateHouseContract(CreateHouseContract createHouseContract)
        {
            string parentNum = null;

            var contractOneSql = "select * from contract where type = 'withOwner' and houseId=" +
                                 createHouseContract.HouseId;
            var contractOne = DataOperate.FindOne(contractOneSql);
            if (contractOne != null)
            {
                parentNum = contractOne["id"].ToString();
            }

            var addSql =
                "insert into contract(adminId,type,startAt,endAt,contractPic,houseId,userId,parentNum,rentPrice,payForm) values(@AdminId,@Type,@StartAt,@EndAt,@ContractPic,@HouseId,@UserId,@ParentNum,@RentPrice,@PayForm)";
            var upSql = "update house set rentPrice='" + Convert.ToInt32(createHouseContract.RentPrice) * 1.2 + "' , payForm='" +
                        createHouseContract.PayForm + "', rentStatus='activated' where id=" +
                        createHouseContract.HouseId;


            MySqlConnection con = DataOperate.GetCon();
            var ownerContractCmd = new MySqlCommand(addSql, con);
            ownerContractCmd.Parameters.Add(new MySqlParameter("@AdminId", MySqlDbType.Int32, 11)).Value = GetAdminId();
            ownerContractCmd.Parameters.Add(new MySqlParameter("@Type", MySqlDbType.Enum)).Value = "withOwner";
            ownerContractCmd.Parameters.Add(new MySqlParameter("@StartAt", MySqlDbType.Date)).Value = createHouseContract.StartAt;
            ownerContractCmd.Parameters.Add(new MySqlParameter("@EndAt", MySqlDbType.Date)).Value = createHouseContract.EndAt;
            ownerContractCmd.Parameters.Add(new MySqlParameter("@RentPrice", MySqlDbType.Decimal, 11)).Value =
                createHouseContract.RentPrice;
            ownerContractCmd.Parameters.Add(new MySqlParameter("@PayForm", MySqlDbType.Enum)).Value = createHouseContract.PayForm;
            ownerContractCmd.Parameters.Add(new MySqlParameter("@HouseId", MySqlDbType.Int32, 11)).Value =
                createHouseContract.HouseId;
            ownerContractCmd.Parameters.Add(new MySqlParameter("@ContractPic", MySqlDbType.VarChar, 100)).Value =
                createHouseContract.ContractPic;
            ownerContractCmd.Parameters.Add(new MySqlParameter("@UserId", MySqlDbType.Int32, 11)).Value = createHouseContract.UserId;
            ownerContractCmd.Parameters.Add(new MySqlParameter("@ParentNum", MySqlDbType.Int32, 11)).Value = parentNum;
            
            var sTransaction = con.BeginTransaction();

            try
            {
                ownerContractCmd.Transaction = sTransaction;
                ownerContractCmd.ExecuteNonQuery();
                var contractId = ownerContractCmd.LastInsertedId;

                // 生成交易账单
                var transactionOwnerCmds = new List<MySqlCommand>();
                DateTimeFormatInfo dtFormat = new DateTimeFormatInfo {ShortDatePattern = "yyyy-MM-dd"};
                DateTime startAt = Convert.ToDateTime(createHouseContract.StartAt, dtFormat);
                DateTime endAt = Convert.ToDateTime(createHouseContract.EndAt, dtFormat);
                var first = true;
                while (startAt < endAt)
                {
                    var transactionSql =
                        "INSERT INTO transactions (contractId, userId, account, payForm, tranDate, startDate, endDate, tranStatus, tranType, adminId) VALUES (@ContractId, @UserId, @Account, @PayForm, @TranDate, @StartDate, @EndDate, @TranStatus, @TranType, @AdminId);";
                
                    var transactionCmd = new MySqlCommand(transactionSql, con) {Transaction = sTransaction};
                    transactionCmd.Parameters.Add(new MySqlParameter("@ContractId", MySqlDbType.Int32, 11)).Value = contractId;
                    transactionCmd.Parameters.Add(new MySqlParameter("@UserId", MySqlDbType.Int32, 11)).Value = createHouseContract.UserId;
                    transactionCmd.Parameters.Add(new MySqlParameter("@PayForm", MySqlDbType.Enum)).Value = createHouseContract.PayForm;
                    transactionCmd.Parameters.Add(new MySqlParameter("@StartDate", MySqlDbType.Date)).Value = startAt.ToString("yyyy-MM-dd");

                    DateTime endDate;
                    
                    if (createHouseContract.PayForm == "byYear")
                    {
                        endDate = startAt.AddYears(1);
                        
                        if (endDate > endAt)
                        {
                            // 当年总天数
                            var days = (float)(endDate - startAt).Days;
                            // ReSharper disable once PossibleLossOfFraction
                            var realDays = (float)(endAt - startAt).Days;

                            var rentPrice = Math.Floor(realDays / days * createHouseContract.RentPrice * 12);
                            transactionCmd.Parameters.Add(new MySqlParameter("@Account", MySqlDbType.Decimal)).Value = rentPrice;
                            endDate = endAt;
                        }
                        else
                        {
                            transactionCmd.Parameters.Add(new MySqlParameter("@Account", MySqlDbType.Decimal)).Value = createHouseContract.RentPrice * 12;
                        }
                    }
                    else
                    {
                        endDate = startAt.AddMonths(1);
                        if (endDate > endAt)
                        {
                            // 当月总天数
                            var days = (float)(endDate - startAt).Days;
                            // ReSharper disable once PossibleLossOfFraction
                            var realDays = (float)(endAt - startAt).Days;

                            var rentPrice = Math.Floor(realDays / days * createHouseContract.RentPrice);
                            transactionCmd.Parameters.Add(new MySqlParameter("@Account", MySqlDbType.Decimal)).Value = rentPrice;

                            endDate = endAt;
                        }
                        else
                        {
                            transactionCmd.Parameters.Add(new MySqlParameter("@Account", MySqlDbType.Decimal)).Value = createHouseContract.RentPrice;
                        }
                    }

                    transactionCmd.Parameters.Add(new MySqlParameter("@EndDate", MySqlDbType.Date)).Value = endDate.ToString("yyyy-MM-dd");
                    transactionCmd.Parameters.Add(new MySqlParameter("@TranType", MySqlDbType.Enum)).Value = "withOwner";
                    transactionCmd.Parameters.Add(new MySqlParameter("@AdminId", MySqlDbType.Int32, 100)).Value = GetAdminId();
                
                    if (first)
                    {
                        transactionCmd.Parameters.Add(new MySqlParameter("@TranDate", MySqlDbType.Date)).Value = DateTime.Now.ToString("yyyy-MM-dd");
                        transactionCmd.Parameters.Add(new MySqlParameter("@TranStatus", MySqlDbType.Enum)).Value = "paid";
                        first = false;
                    }
                    else
                    {
                        transactionCmd.Parameters.Add(new MySqlParameter("@TranDate", MySqlDbType.Date)).Value = null;
                        transactionCmd.Parameters.Add(new MySqlParameter("@TranStatus", MySqlDbType.Enum)).Value = "unpaid";
                    }
                    
                    startAt = createHouseContract.PayForm == "byYear" ? startAt.AddYears(1) : startAt.AddMonths(1);
                    
                    transactionOwnerCmds.Add(transactionCmd);
                }
                foreach (var mySqlCommand in transactionOwnerCmds)
                {
                    mySqlCommand.ExecuteNonQuery();
                }

                var upCmd = new MySqlCommand(upSql, con) {Transaction = sTransaction};
                upCmd.ExecuteNonQuery();
                sTransaction.Commit();
            }
            catch (System.Exception e)
            {
                sTransaction.Rollback();
                return Success(false);
            }

            return Success(true);
        }

        // 续签房主合同 获取原合同的相关信息
        [HttpGet("renewalHouseContract")]
        public JsonResult RenewalHouseContract(int contractId)
        {
            var sql = "select * from contract where type='withOwner' and id=" + contractId;
            return Success(DataOperate.FindAll(sql));
        }

        // 解除与房主的合同
        [HttpPost("dissolveHouseContract")]
        public JsonResult DissolveHouseContract(DissolveHouseContract dissolveHouseContract)
        {
            string sql =
                "select count(*) from contract where type='withTenant' and contractStatus='undue' and houseId=" +
                dissolveHouseContract.HouseId;
            string[] sqlT = new string[4];
            int i = 0;
            sqlT[i++] = "update contract set contractStatus='invalid' where id=" + dissolveHouseContract.ContractId;
            sqlT[i++] = "update transactions set tranStatus='invalid' where tranStatus='unpaid' and contractId=" + dissolveHouseContract.ContractId;
            sqlT[i++] = "update house set rentStatus='invalid' where id="+dissolveHouseContract.HouseId;
            sqlT[i] = "delete from resOfHouse where houseId=" + dissolveHouseContract.HouseId;
            if (DataOperate.Sele(sql) == 0)
            {
                return Success(DataOperate.ExecTransaction(sqlT));
            }
            else
            {
                return Fail("该合同关联房源已被租用，不可解约..", 3001);
            }
        }

        // 解除与租户的合同
        [HttpPost("dissolveTenantContract")]
        public JsonResult DissolveTenantContract(DissolveTenantContract dissolveTenantContract)
        {
            string[] sqlT = new string[3];
            int i = 0;
            sqlT[i++] = "update contract set contractStatus='invalid' where id=" + dissolveTenantContract.ContractId;
            sqlT[i++] = "update transactions set tranStatus='invalid' where tranStatus='unpaid' and contractId=" + dissolveTenantContract.ContractId;
            sqlT[i] = "update house set rentStatus='empty' where id="+dissolveTenantContract.HouseId;
            return Success(DataOperate.ExecTransaction(sqlT));
        }

        // 获取房主、租户续签申请列表 
        [HttpGet("getApplyRenewalList")]
        public JsonResult GetApplyRenewalList(string type)
        {
            var sql =
                "select * from renewalContractApply inner join house on renewalContractApply.houseId = house.id where type='" +
                type + "'";
            return Success(DataOperate.FindAll(sql));
        }

        // 通过房主的续签申请
        [HttpPost("passOwnerRenewalApply")]
        public JsonResult PassOwnerRenewalApply(PassOwnerRenewalApply passOwnerRenewalApply)
        {
            string[] sqlT = new string[2];
            int i = 0;
            sqlT[i++] = "update renewalContractApply set applyStatus='pass', adminId=" + GetAdminId() + " where id=" +
                        passOwnerRenewalApply.Id;
            sqlT[i] = "update house set rentStatus='unactivated' where id=" + passOwnerRenewalApply.HouseId;
            return Success(DataOperate.ExecTransaction(sqlT));
        }

        // 拒绝房主的续签申请
        [HttpPost("rejectOwnerRenewalApply")]
        public JsonResult RejectOwnerRenewalApply(RejectOwnerRenewalApply rejectOwnerRenewalApply)
        {
            var sql = "update renewalContractApply set applyStatus='reject', adminId=" + GetAdminId() + " where id=" +
                      rejectOwnerRenewalApply.Id;
            return Success(DataOperate.Update(sql));
        }
    }

    public class DissolveTenantContract
    {
        public int ContractId { get; set; }
        public int HouseId { get; set; }
    }

    public class RejectOwnerRenewalApply
    {
        public int Id { get; set; }
    }

    public class PassOwnerRenewalApply
    {
        public int Id { get; set; }
        public int HouseId { get; set; }
    }

    public class DissolveHouseContract
    {
        public int ContractId { get; set; }
        public int HouseId { get; set; }
    }

    public class CreateHouseContract
    {
        public int HouseId { get; set; }
        public int UserId { get; set; }
        public float RentPrice { get; set; }
        public string PayForm { get; set; }
        public string StartAt { get; set; }
        public string EndAt { get; set; }
        public string ContractPic { get; set; }
    }

    public class CreateTenantContract
    {
        public int HouseId { get; set; }
        public int UserId { get; set; }
        public float RentPrice { get; set; }
        public string PayForm { get; set; }
        public string StartAt { get; set; }
        public string EndAt { get; set; }
        public string ContractPic { get; set; }
    }
}