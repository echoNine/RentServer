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
    

    public class HouseForm
    {
        public string Id { get; set; }
        public string Address { get; set; }
        public string Price { get; set; }
        public string Area { get; set; }
        public string RentType { get; set; }
        public string Floor { get; set; }
        public string Layout { get; set; }
        public string Orientation { get; set; }
        public string BuildTime { get; set; }
        public string Intro { get; set; }
        public string Community { get; set; }
        public string Cover { get; set; }
        public string Owner { get; set; }
        public bool FirstRent { get; set; }
    }

    public class OwnerId
    {
        public string Id { get; set; }
    }

    public class OwnerDialogForm
    {
        public string Id { get; set; }
        public string HouseId { get; set; }
        public string UserId { get; set; }
        public string StartAt { get; set; }
        public string EndAt { get; set; }
        public string ContractStatus { get; set; }
        public string ContractPic { get; set; }
    }

    public class RenewOwnerDialogForm
    {
        public string Id { get; set; }
        public string HouseId { get; set; }
        public string UserId { get; set; }
        public string StartAt { get; set; }
        public string EndAt { get; set; }
        public string ContractStatus { get; set; }
        public string ContractPic { get; set; }
        public string ParentNum { get; set; }
    }

    [Route("[controller]")]
    [ApiController]
    public class ListController : BaseController
    {
        // GET Owner
        [HttpGet("owner")]
        public JsonResult GetOwner()
        {
            string sql = "select * from owner";
            return Success(DataOperate.FindAll(sql));
        }

        // GET Tenant
        [HttpGet("tenant")]
        public JsonResult GetTenant()
        {
            string sql = "select * from tenant";
            return Success(DataOperate.FindAll(sql));
        }

        // GET House
        [HttpGet("house")]
        public JsonResult GetHouse(string userId, string userName, string rentType, string houseStatus,
            string priceStart, string priceEnd)
        {
            string gethouse = "select * from house";
            DataSet allhouse = DataOperate.FindAll(gethouse);
            foreach (DataRow row in allhouse.Tables[0].Rows)
            {
                string getOwnerContractStatus = "select * from ownercontract where houseId='" + row["id"] + "'";
                string getTenantContractStatus = "select * from tenantcontract where houseId='" + row["id"] + "'";
                DataSet allOwnerContractStatus = DataOperate.FindAll(getOwnerContractStatus);
                DataSet allTenantContractStatus = DataOperate.FindAll(getTenantContractStatus);

                bool ownerActivate = false;
                bool tenantActivate = false;
                string defaultStatus = "";
                foreach (DataRow row1 in allOwnerContractStatus.Tables[0].Rows)
                {
                    if (row1["contractStatus"].ToString() == "未到期")
                    {
                        ownerActivate = true; // 已激活=空闲/已租用
                        break;
                    }
                }
                foreach (DataRow row2 in allTenantContractStatus.Tables[0].Rows)
                {
                    if (row2["contractStatus"].ToString() == "未到期")
                    {
                        tenantActivate = true; // 已激活=空闲/已租用
                        break;
                    }
                }

                if (ownerActivate)
                {
                    if (tenantActivate)
                    {
                        defaultStatus = "已租用";
                    }
                    else
                    {
                        defaultStatus = "空闲";
                    }
                }
                else
                {
                    defaultStatus = "未激活";
                }
                
                DataOperate.Update("update house set status='" + defaultStatus + "' where id='" + row["id"] + "'");
            }

            var where = new List<WhereItem>();

            if (userId != null)
            {
                where.Add(new WhereItem("owner.id", "=", userId));
            }

            if (userName != null)
            {
                where.Add(new WhereItem("owner.username", "=", userName));
            }

            if (rentType != null)
            {
                where.Add(new WhereItem("house.rentType", "=", rentType));
            }

            if (houseStatus != null)
            {
                where.Add(new WhereItem("house.status", "=", houseStatus));
            }

            if (priceStart != null)
            {
                where.Add(new WhereItem("house.price", ">=", priceStart));
            }

            if (priceEnd != null)
            {
                where.Add(new WhereItem("house.price", "<=", priceEnd));
            }

            string sql = "";
            if (where.Count > 0)
            {
                sql = "select * from house inner join owner on house.owner = owner.id where ";
                foreach (var whereItem in where)
                {
                    sql = sql + whereItem.Column + " " + whereItem.Condition + " '" + whereItem.Value + "' and ";
                }

                sql = sql.TrimEnd(new char[] {'a', 'n', 'd', ' '});
            }
            else
            {
                sql = "select * from house";
            }

            return Success(DataOperate.FindAll(sql));
        }

        // Post AddHouse
        [HttpPost("addHouse")]
        public JsonResult AddHouse(HouseForm houseForm)
        {
            string sql = "update house set address='" + houseForm.Address + "', price='" + houseForm.Price +
                         "', area='" + houseForm.Area + "', rentType='" + houseForm.RentType + "', floor='" +
                         houseForm.Floor + "', layout='" + houseForm.Layout + "', orientation='" +
                         houseForm.Orientation + "', buildTime='" + houseForm.BuildTime + "', intro='" +
                         houseForm.Intro + "', community='" + houseForm.Community + "', cover='" + houseForm.Cover +
                         "', owner='" + houseForm.Owner +
                         "', firstRent='" + houseForm.FirstRent + "' where id='" + houseForm.Id + "'";
            return Success(DataOperate.Update(sql));
        }

        // GET OwnerContract
        [HttpGet("ownerContract")]
        public JsonResult GetOwnerContract(string seaOwnerId, string seaHouseId)
        {
            string newsql = "";
            string sql = "select * from ownercontract";
            DataSet allContract = DataOperate.FindAll(sql);
            foreach (DataRow row in allContract.Tables[0].Rows)
            {
                var expired = (DateTime.Parse(row["endAt"].ToString()) <
                               DateTime.Parse(DateTime.Now.ToString()));
                if (expired)
                    newsql = "update ownercontract set contractStatus='已到期' where id='" + row["id"] + "'";
                else
                    newsql = "update ownercontract set contractStatus='未到期' where id='" + row["id"] + "'";
                DataOperate.Update(newsql);
            }

            string sqlParent = "";
            string sqlChildren = "";

            if (seaOwnerId != null)
            {
                sqlParent = "select * from ownercontract where parentNum is null and userId=" + seaOwnerId;
                sqlChildren = "select * from ownercontract where parentNum is not null and userId=" + seaOwnerId;
            }

            if (seaHouseId != null)
            {
                sqlParent = "select * from ownercontract where parentNum is null and houseId=" + seaHouseId;
                sqlChildren = "select * from ownercontract where parentNum is not null and houseId=" + seaHouseId;
            }

            if (seaOwnerId == null && seaHouseId == null)
            {
                sqlParent = "select * from ownercontract where parentNum is null";
                sqlChildren = "select * from ownercontract where parentNum is not null";
            }

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

        // Post AddOwnerContract
        [HttpPost("newOwnerContractId")]
        public JsonResult NewOwnerContractId()
        {
            string addSql = "insert into ownercontract default values";
            DataOperate.Update(addSql);
            string idSql = "select id from ownercontract order by id desc limit 1";
            return Success(DataOperate.FindAll(idSql));
        }

        // Post NewHouseId
        [HttpPost("newHouseId")]
        public JsonResult NewHouseId()
        {
            string addSql = "insert into house default values";
            DataOperate.Update(addSql);
            string idSql = "select id from house order by id desc limit 1";
            return Success(DataOperate.FindAll(idSql));
        }

        // Get GetOwnerId
        [HttpGet("getOwnerId")]
        public JsonResult GetOwnerId()
        {
            string sql = "select id from owner";
            return Success(DataOperate.FindAll(sql));
        }

        // Get GetHouseId
        [HttpGet("getHouseId")]
        public JsonResult GetHouseId(int id)
        {
            string sql = "select id,status from house where owner=" + id;
            return Success(DataOperate.FindAll(sql));
        }

        // Post AddOwnerContract
        [HttpPost("addOwnerContract")]
        public bool AddOwnerContract(OwnerDialogForm ownerDialogForm)
        {
            string[] sqlT = new string[2];
            int i = 0;
            sqlT[i++] = "update ownercontract set userId='" + ownerDialogForm.UserId + "', houseId='" + ownerDialogForm.HouseId +
                        "', startAt='" + ownerDialogForm.StartAt + "', endAt='" + ownerDialogForm.EndAt + "', contractPic='" +
                        ownerDialogForm.ContractPic + "', contractStatus='" + ownerDialogForm.ContractStatus + "' where id=" +
                        ownerDialogForm.Id;
            sqlT[i] = "update house set status='空闲' where id='" + ownerDialogForm.HouseId + "'";
            return DataOperate.ExecTransaction(sqlT);
        }

        // Post RenewOwnerContract
        [HttpPost("renewOwnerContract")]
        public bool RenewOwnerContract(RenewOwnerDialogForm renewOwnerDialogForm)
        {
            string[] sqlT = new string[2];
            int i = 0;
            sqlT[i++] = "update ownercontract set userId='" + renewOwnerDialogForm.UserId + "', houseId='" +
                        renewOwnerDialogForm.HouseId +
                        "', startAt='" + renewOwnerDialogForm.StartAt + "', endAt='" + renewOwnerDialogForm.EndAt +
                        "', contractPic='" +
                        renewOwnerDialogForm.ContractPic + "', contractStatus='" + renewOwnerDialogForm.ContractStatus +
                        "', parentNum='" + renewOwnerDialogForm.ParentNum + "' where id=" +
                        renewOwnerDialogForm.Id;
            sqlT[i] = "update house set status='空闲' where id='" + renewOwnerDialogForm.HouseId + "'";
            return DataOperate.ExecTransaction(sqlT);
        }
        

        // Post DeleteOwner
        [HttpPost("deleteOwner")]
        public bool DeleteOwner(OwnerId ownerId)
        {
            string selsql = "select * from ownercontract where userId='" + ownerId.Id + "'";
            string[] sqlT = new string[2];
            int i = 0;
            sqlT[i++] = "delete from house where owner='" + ownerId.Id + "'";
            sqlT[i] = "delete from owner where id='" + ownerId.Id + "'";
            DataSet allContract = DataOperate.FindAll(selsql);
            if (allContract == null)
            {
                DataOperate.Update(sqlT[i]);
                return true;
            } else if (getContractBool(allContract))
            {
                return DataOperate.ExecTransaction(sqlT);
            }
            else
            {
                return false;
            }
        }

        public bool getContractBool(DataSet allContract)
        {
            bool delable = true;
            foreach (DataRow row in allContract.Tables[0].Rows)
            {
                if (row["contractStatus"].ToString() == "未到期")
                {
                    delable = false;
                }
            }
            return delable;
        }

        // GET HouseList
        [HttpGet("houseList")]
        public JsonResult GetHouseList(string id)
        {
            string sql = "select * from house where owner='" + id + "'";
            return Success(DataOperate.FindAll(sql));
        }
        
        // Get ContractOfInfo
        [HttpGet("contractOfInfo")]
        public JsonResult ContractOfInfo()
        {
            string sql =
                "select * from ownercontract inner join owner on owner.id = ownercontract.userId union all select * from tenantcontract inner join tenant on tenant.id = tenantcontract.userId order by startAt desc";
            return Success(DataOperate.FindAll(sql));
        }
    }
}