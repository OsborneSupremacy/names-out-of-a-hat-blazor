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

    private static IServiceCollection AddVendorServices(this IServiceCollection services) =>
        services
            .AddSingleton<IAmazonDynamoDB, AmazonDynamoDBClient>()
            .AddSingleton<IAmazonSimpleEmailService>(
                new AmazonSimpleEmailServiceClient(
                    Amazon.RegionEndpoint.GetBySystemName(Environment.GetEnvironmentVariable("AWS_REGION")!)))
            .AddSingleton<IAmazonSQS, AmazonSQSClient>();

    internal static IServiceCollection AddUtilities(this IServiceCollection services) =>
        services
            .AddLogging(builder => builder.AddLambdaLogger())
            .AddSingleton<JsonService>();

    internal static IServiceCollection AddBusinessServices(this IServiceCollection services) =>
        services
            .AddSingleton<GiftExchangeDataProvider>()

            .AddSingleton<AddParticipantService>()
            .AddSingleton<GetParticipantService>()
            .AddSingleton<EditParticipantService>()
            .AddSingleton<RemoveParticipantService>()

            .AddSingleton<CreateHatService>()
            .AddSingleton<DeleteHatService>()
            .AddSingleton<EditHatService>()
            .AddSingleton<GetHatService>()
            .AddSingleton<GetHatsService>()

            .AddSingleton<EligibilityValidationService>()
            .AddSingleton<ValidationService>()
            .AddSingleton<AssignRecipientsService>()

            .AddSingleton<VerifyOrganizerService>()
            .AddSingleton<EmailCompositionService>()
            .AddSingleton<EnqueueInvitationsService>()
            .AddSingleton<InvitationQueueHandlerService>();
}
