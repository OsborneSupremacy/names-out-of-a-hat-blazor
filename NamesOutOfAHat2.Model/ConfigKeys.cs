namespace NamesOutOfAHat2.Model;

/// <summary>
/// A dictionary for accessing configuration keys neeeded by the application in appsettings.
/// 
/// Using this class rather than <see cref="Settings"/> for flexibility. e.g. if an email
/// service requires multiple config values, they can all be added to this dictionary
/// without having modify the class.
/// </summary>
public class ConfigKeys : Dictionary<string, string>
{
    public ConfigKeys() : base(StringComparer.OrdinalIgnoreCase)
    {
    }
}
