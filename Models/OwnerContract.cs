using System.Collections.Generic;

namespace RentServer.Models
{
    public class OwnerContract
    {
        public int Id { get; set; }
        public int AdminId { get; set; }
        public int UserId { get; set; }
        public int? HouseId { get; set; }
        public string Type { get; set; }
        public string StartAt { get; set; }
        public string EndAt { get; set; }
        public string RentPrice { get; set; }
        public string PayForm { get; set; }
        public string ContractPic { get; set; }
        public string ContractStatus { get; set; }
        public string ParentNum { get; set; }
        public string CreatedAt { get; set; }
        public int CommissionId { get; set; }
        public List<OwnerContract> Children { get; set; }
        
        public void SetAttribute(string name, object value)
        {
            if (value == null || (string)value == "")
            {
                return;
            }
            switch (name)
            {
                case "id":
                    Id = int.Parse((string)value);
                    break;
                case "adminId":
                    AdminId = int.Parse((string)value);
                    break;
                case "userId":
                    UserId = int.Parse((string)value);
                    break;
                case "houseId":
                    HouseId = int.Parse((string)value);
                    break;
                case "type":
                    Type = (string)value;
                    break;
                case "startAt":
                    StartAt = (string)value;
                    break;
                case "endAt":
                    EndAt = (string)value;
                    break;
                case "rentPrice":
                    RentPrice = (string)value;
                    break;
                case "payForm":
                    PayForm = (string)value;
                    break;
                case "contractPic":
                    ContractPic = (string)value;
                    break;
                case "contractStatus":
                    ContractStatus = (string)value;
                    break;
                case "parentNum":
                    ParentNum = (string)value;
                    break;
                case "createdAt":
                    CreatedAt = (string)value;
                    break;
                case "commissionId":
                    CommissionId = int.Parse((string)value);
                    break;
            }
        }
    }
}