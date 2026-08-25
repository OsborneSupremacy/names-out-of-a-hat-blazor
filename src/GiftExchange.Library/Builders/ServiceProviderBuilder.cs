using System.Text.Json;
using Amazon;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Scheduler;
using Amazon.SimpleEmail;
using Amazon.SimpleSystemsManagement;
using Amazon.AuroraDsql.Npgsql;
using Amazon.SQS;
using GiftExchange.Library.Validators;

namespace GiftExchange.Library.Builders;

internal static class ServiceProviderBuilder
{
    public static IServiceProvider Build() =>
        new ServiceCollection()
            .AddUtilities()
            .AddVendorServices()
            .AddBusinessServices()
            .AddValidators()
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
                .AddAWSService<IAmazonScheduler>()
                .AddAWSService<IAmazonComprehend>()
                .AddAWSService<IAmazonSimpleSystemsManagement>()
                .AddSingleton<IAmazonSimpleEmailService, AmazonSimpleEmailServiceClient>() // AddAWSService fails for SES
                .AddSingleton<DsqlDataSource>(_ => DsqlDataSourceProvider.Create())
                // A factory rather than AddDbContext: this container has no scopes, so a scoped
                // DbContext would be resolved from the root and shared like a singleton. A
                // DbContext is not thread safe, so each unit of work gets its own.
                .AddDbContextFactory<GiftExchangeDbContext>((provider, options) =>
                    options.UseNpgsql(
                        // The connector wraps an NpgsqlDataSource and exposes it for exactly this.
                        provider.GetRequiredService<DsqlDataSource>().DataSource,
                        // DSQL reports write conflicts as serialization failures (SQLSTATE
                        // 40001), which Npgsql already classifies as transient.
                        npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(2), errorCodesToAdd: null)));
        }

        internal IServiceCollection AddUtilities()
        {
            JsonSerializerOptions options = new()
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                TypeInfoResolver = GiftExchangeJsonTypeInfoResolver.Default
            };
            return services
                .AddLogging(builder => builder.AddLambdaLogger())
                .AddSingleton(options)
                .AddSingleton<JsonService>()
                .AddSingleton<ApiGatewayAdapter>();
        }

        internal IServiceCollection AddValidators() =>
            services
                .AddSingleton<IValidator<AddParticipantRequest>, AddParticipantRequestValidator>()
                .AddSingleton<IValidator<RequestMagicLinkRequest>, RequestMagicLinkRequestValidator>()
                .AddSingleton<IValidator<RedeemMagicLinkRequest>, RedeemMagicLinkRequestValidator>()
                .AddSingleton<IValidator<CloseHatRequest>, CloseHatRequestValidator>()
                .AddSingleton<IValidator<CreateHatRequest>, CreateHatRequestValidator>()
                .AddSingleton<IValidator<EditHatRequest>, EditHatRequestValidator>()
                .AddSingleton<IValidator<EditParticipantRequest>, EditParticipantRequestValidator>()
                .AddSingleton<IValidator<PreviewInvitationsRequest>, PreviewInvitationsRequestValidator>()
                .AddSingleton<IValidator<SendInvitationsRequest>, SendInvitationsRequestValidator>()
                .AddSingleton<IValidator<ValidateHatRequest>, ValidateHatRequestValidator>();

        internal IServiceCollection AddBusinessServices() =>
            services
                .AddSingleton<GiftExchangeProvider>()
                .AddSingleton<LoginTokenProvider>()
                .AddSingleton<SigningSecretProvider>()
                .AddSingleton<SessionTokenService>()
                .AddSingleton<IContentModerationService, ContentModerationService>()
                .AddSingleton<HatPreconditionValidator>()

                .AddKeyedSingleton<IApiGatewayHandler, RequestMagicLinkService>("post/auth/requestlink")
                .AddKeyedSingleton<IApiGatewayHandler, RedeemMagicLinkService>("post/auth/redeem")

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

                .AddKeyedSingleton<IApiGatewayHandler, PreviewInvitationsService>("get/hat/{email}/previewinvitations/{id}")
                .AddKeyedSingleton<IApiGatewayHandler, EnqueueInvitationsService>("post/hat/sendinvitations")
                .AddKeyedSingleton<IApiGatewayHandler, CloseHatService>("post/hat/close")

                .AddSingleton<ValidationService>() // registered separately for direct use
                .AddSingleton<EmailCompositionService>()
                .AddSingleton<InvitationQueueHandlerService>()
                .AddSingleton<ISchedulerService, SchedulerService>()
            ;
    }
}
