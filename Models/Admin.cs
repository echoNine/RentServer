namespace RentServer.Models
{
    public class Admin : BaseModel
    {
        public new static string TableName = "admin";
        
        public string MgrId { get; set; }
        public string MgrUserName { get; set; }
        public string MgrPwd { get; set; }
        public string MgrTrueName { get; set; }
        public string MgrSex { get; set; }
        public string MgrCardNum { get; set; }
        public string MgrPhone { get; set; }
        public string MgrNative { get; set; }
        public string CreatedAt { get; set; }
    }
}