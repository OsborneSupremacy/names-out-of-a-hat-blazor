using Microsoft.Extensions.DependencyInjection;
using NamesOutOfAHat2.Interface;
using NamesOutOfAHat2.Model;
using NamesOutOfAHat2.Utility;

namespace NamesOutOfAHat2.Service
{
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

        public (bool isValid, IList<string> errors) Validate(Hat hat)
        {
            var (isValid, errors) = _componentModelValidationService.Validate(hat);
            if (!isValid) return (false, errors);

            return Validate(hat);
        }

        public (bool isValid, IList<string> errors) Validate(IList<Participant> givers)
        {
            var (isValid, errors) = _componentModelValidationService.ValidateList(givers);
            if (!isValid) return (false, errors);

            // validate count
            (isValid, errors) = givers.Count switch
            {
                0 => (false, errorToList("A gift exchange like this needs at least three people")),
                1 => (false, errorToList("One person makes for a lonely gift exchange. Add at least two more people.")),
                2 => (false, errorToList("If your gift exchange has exactly two people, they're going to get each other's name. No reason to pick names out of a hat! Add at least one more person.")),
                > _max => (false, errorToList($"{_max} people is the maximum. How did this get past frontend validation ? Are you trying to hack this app?")),
                _ => (true, Enumerable.Empty<string>().ToList())
            };

            if (!isValid) return (false, errors);

            var duplicateCheckServices = _serviceProvider.GetServices<IDuplicateCheckService>();

            foreach (var duplicateCheckService in duplicateCheckServices)
            {
                (isValid, errors) = duplicateCheckService.Execute(givers.Select(x => x.Person).ToList());
                if (!isValid) return (false, errors);
            }

            return (true, Enumerable.Empty<string>().ToList());

            static IList<string> errorToList(string error) =>
                new List<string>() { error };
        }
    }
}
