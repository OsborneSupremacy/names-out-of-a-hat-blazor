namespace NamesOutOfAHat2.Interface
{
    public interface IComponentModelValidationService
    {
        public (bool isValid, IList<string> errors) Validate<T>(T item);

        public (bool isValid, IList<string> errors) ValidateList<T>(IList<T> items);

    }
}