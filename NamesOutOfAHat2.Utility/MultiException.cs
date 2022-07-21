namespace NamesOutOfAHat2.Utility
{
    public class MultiException : Exception
    {
        public IList<string> Errors { get; set; }

        public MultiException(IList<string> errors)
        {
            Errors = errors ?? throw new ArgumentNullException(nameof(errors));
        }

        public MultiException(string error)
        {
            Errors = new List<string>
        {
            error
        };
        }
    }
}