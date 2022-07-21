using LanguageExt.Common;
using Microsoft.Extensions.DependencyInjection;
using NamesOutOfAHat2.Interface;
using NamesOutOfAHat2.Model;
using NamesOutOfAHat2.Utility;

namespace NamesOutOfAHat2.Service;

[ServiceLifetime(ServiceLifetime.Scoped)]
public class ValidationService
{
    private const int _max = 30;

    private readonly IComponentModelValidationService _componentModelValidationService;

    private readonly IServiceProvider _serviceProvider;

    public ValidationService(IComponentModelValidationService componentModelValidationService, IServiceProvider serviceProvider)
    {
        _componentModelValidationService = componentModelValidationService ?? throw new ArgumentNullException(nameof(componentModelValidationService));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public Result<bool> Validate(Hat hat)
    {
        var result = _componentModelValidationService.Validate(hat);
        if (!result.IsSuccess) return result;

        return Validate(hat.Participants);
    }

    public Result<bool> Validate(IList<Participant> participants)
    {
        var result = _componentModelValidationService.ValidateList(participants);
        if (!result.IsSuccess) return result;

        // validate count
        var (isValid, error) = participants.Count switch
        {
            0 => (false, "A gift exchange like this needs at least three people"),
            1 => (false, "One person makes for a lonely gift exchange. Add at least two more people."),
            2 => (false, "If your gift exchange has exactly two people, they're going to get each other's name. No reason to pick names out of a hat! Add at least one more person."),
            > _max => (false, $"{_max} people is the maximum. How did this get past frontend validation? Are you trying to hack this app?"),
            _ => (true, string.Empty)
        };

        if (!isValid)
            return new Result<bool>(new MultiException(error));

        var duplicateCheckServices = _serviceProvider.GetServices<IDuplicateCheckService>();

        foreach (var duplicateCheckService in duplicateCheckServices)
        {
            var duplicatesCheckResult = duplicateCheckService.Execute(participants.Select(x => x.Person).ToList());
            if (!duplicatesCheckResult.IsSuccess)
                return duplicatesCheckResult;
        }

        return true;
    }
}
