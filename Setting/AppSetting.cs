using Microsoft.Extensions.Configuration;

namespace RentServer.Setting
{
    public class Mysql
    {
        public string ConnectString { get; set; }
    }
    
    public class AppSetting
    {
        public static Mysql Mysql;
        
        public void Initial(IConfiguration configuration)
        {
            Mysql = new Mysql {ConnectString = configuration["Mysql:ConnectString"]};
        }
    }
}