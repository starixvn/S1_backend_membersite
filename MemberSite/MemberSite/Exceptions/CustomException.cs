namespace MemberSite.Exceptions;

public class CustomException : Exception
{
    public CustomException()
    {
    }

    public CustomException(string message)
        : base(message)
    {
    }

    public CustomException(string message, Exception inner)
        : base(message, inner)
    {
    }

    public CustomException(string message, int code = 9000)
        : base(message)
    {
        Code = code;
    }

    public int Code { get; set; } = 9000;
}