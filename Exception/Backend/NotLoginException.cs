namespace RentServer.Exception.Backend
{
    public class NotLoginException: ApiException
    {
        public NotLoginException()
        {
            ErrorCode = 90001;
            ErrorMsg = "admin not login";
            Status = 401;
        }
    }
}