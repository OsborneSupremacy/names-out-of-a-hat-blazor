namespace NamesOutOfAHat2.Interface;

public interface ILocalStorageService
{
    public Task<string> GetFromLocalStorage(string key);

    public Task SetLocalStorage(string key, string value);
}
