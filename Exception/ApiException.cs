namespace RentServer.Exception
{
    public class ApiException: System.Exception
    {
        public ApiException()
        {
        }
        
        public ApiException(int errorCode)
        {
            ErrorCode = errorCode;
        }

        public ApiException(int errorCode, string errorMsg)
        {
            ErrorCode = errorCode;
            ErrorMsg = errorMsg;
        }
        
        public int Status { get; set; } = 500;
        public string ErrorMsg { get; set; } = "server error";
        public int ErrorCode { get; set; } = -10000;
    }
}