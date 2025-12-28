using Amazon;
using Amazon.Extensions.NETCore.Setup;
using Amazon.SimpleEmail;
using Amazon.SQS;

namespace GiftExchange.Library.Services;

internal static class ServiceProviderBuilder
{
    public static IServiceProvider Build() =>
        new ServiceCollection()
            .AddUtilities()
            .AddVendorServices()
            .AddBusinessServices()
            .BuildServiceProvider();

    private static IServiceCollection AddVendorServices(this IServiceCollection services) =>
        services
            .AddDefaultAWSOptions(new AWSOptions
            {
                Region = RegionEndpoint.GetBySystemName(EnvReader.GetStringValue("AWS_REGION")),
            })
            .AddAWSService<IAmazonDynamoDB>()
            .AddSingleton<IAmazonSimpleEmailService>()
            .AddSingleton<IAmazonSQS>();

    internal static IServiceCollection AddUtilities(this IServiceCollection services) =>
        services
            .AddLogging(builder => builder.AddLambdaLogger())
            .AddSingleton<JsonService>()
            .AddSingleton<ApiGatewayAdapter>();

    internal static IServiceCollection AddBusinessServices(this IServiceCollection services) =>
        services
            .AddSingleton<GiftExchangeProvider>()

            .AddKeyedSingleton<IApiGatewayHandler, GetHatService>("get/hat/{email}/{id}")
            .AddKeyedSingleton<IApiGatewayHandler, GetHatsService>("get/hats/{email}")

            .AddKeyedSingleton<IApiGatewayHandler, CreateHatService>("post/hat")
            .AddKeyedSingleton<IApiGatewayHandler, EditHatService>("put/hat")

            .AddKeyedSingleton<IApiGatewayHandler, AddParticipantService>("post/participant")
            .AddKeyedSingleton<IApiGatewayHandler, EditParticipantService>("put/participant")
            .AddKeyedSingleton<IApiGatewayHandler, GetParticipantService>("get/participant/{organizeremail}/{hatid}/{participantemail}")
            .AddKeyedSingleton<IApiGatewayHandler, RemoveParticipantService>("delete/participant")

            .AddKeyedSingleton<IApiGatewayHandler, AssignRecipientsService>("post/recipients")

            .AddKeyedSingleton<IApiGatewayHandler, ValidationService>("post/hat/validate")

            .AddSingleton<GetParticipantService>()

            .AddSingleton<RemoveParticipantService>()

            .AddSingleton<DeleteHatService>()

            .AddSingleton<ValidationService>()

            .AddSingleton<VerifyOrganizerService>()
            .AddSingleton<EmailCompositionService>()
            .AddSingleton<EnqueueInvitationsService>()
            .AddSingleton<InvitationQueueHandlerService>();
}
