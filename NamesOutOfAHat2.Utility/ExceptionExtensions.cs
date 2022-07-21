namespace NamesOutOfAHat2.Utility
{
    public static class ExceptionExtensions
    {
        public static List<string> GetErrors(this Exception exception)
        {
            if (exception is MultiException multiException)
                return multiException.Errors.ToList();

            throw new NotImplementedException($"Cannot get errors from exception type, {exception.GetType()}");
        }
    }
}