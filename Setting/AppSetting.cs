using Microsoft.Extensions.Configuration;

namespace RentServer.Setting
{
    public class Mysql
    {
        public string ConnectString { get; set; }
    }

    public class Cors
    {
        public string Origins { get; set; }
    }

    public class AppSetting
    {
        public static Mysql Mysql;

        public static Cors Cors;

        public void Initial(IConfiguration configuration)
        {
            Mysql = new Mysql {ConnectString = configuration["Mysql:ConnectString"]};

            Cors = new Cors {Origins = configuration["Cors:Origins"]};
        }
    }
}