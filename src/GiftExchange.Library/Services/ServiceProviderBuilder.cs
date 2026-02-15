using Amazon;
using Amazon.Comprehend;
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

    extension(IServiceCollection services)
    {
        private IServiceCollection AddVendorServices()
        {
            var region = RegionEndpoint.GetBySystemName(EnvReader.GetStringValue("AWS_REGION"));
            return services
                .AddDefaultAWSOptions(new AWSOptions { Region = region })
                .AddAWSService<IAmazonDynamoDB>()
                .AddAWSService<IAmazonSQS>()
                .AddAWSService<IAmazonComprehend>()
                .AddSingleton<IAmazonSimpleEmailService, AmazonSimpleEmailServiceClient>(); // AddAWSService fails for SES
        }

        internal IServiceCollection AddUtilities() =>
            services
                .AddLogging(builder => builder.AddLambdaLogger())
                .AddSingleton<JsonService>()
                .AddSingleton<ApiGatewayAdapter>();

        internal IServiceCollection AddBusinessServices() =>
            services
                .AddSingleton<GiftExchangeProvider>()
                .AddSingleton<IContentModerationService, ContentModerationService>()
                .AddSingleton<HatPreconditionValidator>()

                .AddKeyedSingleton<IApiGatewayHandler, GetHatService>("get/hat/{email}/{id}")
                .AddKeyedSingleton<IApiGatewayHandler, GetHatsService>("get/hats/{email}")

                .AddKeyedSingleton<IApiGatewayHandler, CreateHatService>("post/hat")
                .AddKeyedSingleton<IApiGatewayHandler, EditHatService>("put/hat")

                .AddKeyedSingleton<IApiGatewayHandler, DeleteHatService>("delete/hat")

                .AddKeyedSingleton<IApiGatewayHandler, AddParticipantService>("post/participant")
                .AddKeyedSingleton<IApiGatewayHandler, EditParticipantService>("put/participant")
                .AddKeyedSingleton<IApiGatewayHandler, GetParticipantService>("get/participant/{organizeremail}/{hatid}/{participantemail}")
                .AddKeyedSingleton<IApiGatewayHandler, RemoveParticipantService>("delete/participant")

                .AddKeyedSingleton<IApiGatewayHandler, ValidationService>("post/hat/validate")

                .AddKeyedSingleton<IApiGatewayHandler, AssignRecipientsService>("post/recipients")

                .AddKeyedSingleton<IApiGatewayHandler, EnqueueInvitationsService>("post/hat/sendinvitations")
                .AddKeyedSingleton<IApiGatewayHandler, CloseHatService>("post/hat/close")

                .AddSingleton<ValidationService>() // registered separately for direct use
                .AddSingleton<EmailCompositionService>()
                .AddSingleton<InvitationQueueHandlerService>();
    }
}
