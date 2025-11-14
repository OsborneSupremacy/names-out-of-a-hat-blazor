using Amazon.DynamoDBv2;
using Amazon.SimpleEmail;
using Amazon.SQS;

namespace GiftExchange.Library.Services;

internal static class ServiceProviderBuilder
{
    public static IServiceProvider Build()
    {
        return new ServiceCollection()
            .AddUtilities()
            .AddVendorServices()
            .AddBusinessServices()
            .BuildServiceProvider();
    }

    public static IServiceCollection AddVendorServices(this IServiceCollection services) =>
        services
            .AddSingleton<IAmazonDynamoDB, AmazonDynamoDBClient>()
            .AddSingleton<IAmazonSimpleEmailService>(
                new AmazonSimpleEmailServiceClient(
                    Amazon.RegionEndpoint.GetBySystemName(Environment.GetEnvironmentVariable("AWS_REGION")!)))
            .AddSingleton<IAmazonSQS, AmazonSQSClient>();

    public static IServiceCollection AddUtilities(this IServiceCollection services) =>
        services
            .AddLogging(builder => builder.AddLambdaLogger())
            .AddSingleton<JsonService>();

    public static IServiceCollection AddBusinessServices(this IServiceCollection services) =>
        services
            .AddSingleton<AddParticipantService>()
            .AddSingleton<AssignRecipientsService>()
            .AddSingleton<CreateHatService>()
            .AddSingleton<DeleteHatService>()
            .AddSingleton<EditHatService>()
            .AddSingleton<EligibilityValidationService>()
            .AddSingleton<EmailCompositionService>()
            .AddSingleton<GetHatService>()
            .AddSingleton<InvitationQueueHandlerService>()
            .AddSingleton<RemoveParticipantService>()
            .AddSingleton<ValidateHatService>()
            .AddSingleton<VerifyOrganizerService>();
}
