using LanguageExt.Common;
using NamesOutOfAHat2.Model.DomainModels;

namespace NamesOutOfAHat2.Utility;

public static class ResultExtensions
{
    private static Func<Exception, List<string>> _getErrorsDelegate = (Exception ex) =>
    {
        return ex.GetErrors();
    };

    private static readonly List<string> NoErrors = Enumerable.Empty<string>().ToList();

    public static List<string> GetErrors(this Result<bool> input) =>
        input.Match
        (
            _ =>
            {
                return NoErrors;
            },
            _getErrorsDelegate
        );

    public static List<string> GetErrors(this Result<Hat> input) =>
        input.Match
        (
            _ =>
            {
                return NoErrors;
            },
            _getErrorsDelegate
        );
}
