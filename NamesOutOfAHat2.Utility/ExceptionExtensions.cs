namespace NamesOutOfAHat2.Utility;

public static class ExceptionExtensions
{
    public static List<string> GetErrors(this Exception exception)
    {
        if (exception is AggregateException aggregateException)
            return aggregateException.InnerExceptions.Select(x => x.Message).ToList();

        throw new NotImplementedException($"Cannot get errors from exception type, {exception.GetType()}");
    }
}
