namespace GiftExchange.Library.Utility;

internal class Result
{
    public bool IsSuccess { get; }

    public bool IsFaulted => !IsSuccess;

    public HttpStatusCode StatusCode { get; }

    public Exception Exception =>
        IsFaulted
            ? _exception!
            : throw new InvalidOperationException("Cannot access exception for successful result");

    private readonly Exception? _exception;

    public Result(HttpStatusCode statusCode)
    {
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

internal class Result<T>
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
