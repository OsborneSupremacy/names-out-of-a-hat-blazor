using LanguageExt.Common;
using NamesOutOfAHat2.Model;

namespace NamesOutOfAHat2.Utility;

public static class ResultExtensions
{
    private static Func<Exception, List<string>> _getErrorsDelegate = (Exception ex) => {
        return ex.GetErrors();
    };

    private static readonly List<string> _noErrors = Enumerable.Empty<string>().ToList();

    public static List<string> GetErrors(this Result<bool> input) =>
        input.Match
        (
            success =>
            {
                return _noErrors;
            },
            _getErrorsDelegate
        );

    public static List<string> GetErrors(this Result<Hat> input) =>
        input.Match
        (
            success =>
            {
                return _noErrors;
            },
            _getErrorsDelegate
        );
}
