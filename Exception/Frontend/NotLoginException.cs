namespace RentServer.Exception.Frontend
{
    public class NotLoginException: ApiException
    {
        public NotLoginException()
        {
            ErrorCode = 90002;
            ErrorMsg = "user not login";
            Status = 401;
        }
    }
}