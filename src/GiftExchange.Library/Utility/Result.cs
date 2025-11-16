namespace GiftExchange.Library.Utility;

public class Result<T>
{
    public bool IsSuccess { get; }

    public bool IsFaulted => !IsSuccess;

    public HttpStatusCode StatusCode { get; }

    public T Value =>
        IsSuccess
            ? _value!
            : throw new InvalidOperationException("Cannot access value for faulted result");

    private readonly T? _value;

    public Exception Exception =>
        IsFaulted
            ? _exception!
            : throw new InvalidOperationException("Cannot access exception for successful result");

    private readonly Exception? _exception;

    public Result(T value, HttpStatusCode statusCode)
    {
        _value = value;
        IsSuccess = true;
        StatusCode = statusCode;
    }

    public Result(Exception exception, HttpStatusCode statusCode)
    {
        _exception = exception;
        IsSuccess = false;
        StatusCode = statusCode;
    }
}
