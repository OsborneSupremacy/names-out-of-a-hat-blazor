using System.Text.Json;
using Amazon;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Scheduler;
using Amazon.SimpleEmail;
using Amazon.SimpleNotificationService;
using Amazon.SimpleSystemsManagement;
using Amazon.AuroraDsql.Npgsql;
using Amazon.XRay.Recorder.Handlers.AwsSdk;
using Amazon.XRay.Recorder.Handlers.EntityFramework;
using GiftExchange.Library.Interceptors;
using Amazon.S3;
using Amazon.SQS;
using GiftExchange.Library.Validators;

namespace GiftExchange.Library.Builders;

internal static class ServiceProviderBuilder
{
    public static IServiceProvider Build()
    {
        // Global, static, and therefore here rather than in AddVendorServices: it installs a
        // handler into the AWS SDK's pipeline for every client constructed afterwards, so it has
        // to run before anything resolves one. Once per cold start, which is what this method is.
        //
        // Registering for all services rather than naming them keeps this from being another list
        // that has to be remembered when a client is added -- the kind of per-item bookkeeping the
        // LIVE_MODE comment in locals.tf describes going wrong.
        //
        // Only inside Lambda. Outside it there is no segment to attach a subsegment to and no
        // daemon to receive one, so a local run or a test would be doing bookkeeping for traces
        // nothing collects. The tests never call this method -- they compose AddUtilities and
        // AddBusinessServices directly -- but that is their choice to reverse, not a guarantee to
        // build on.
        if (IsRunningInLambda) AWSSDKHandler.RegisterXRayForAllServices();

        return new ServiceCollection()
            .AddUtilities()
            .AddVendorServices()
            .AddBusinessServices()
            .AddValidators()
            .BuildServiceProvider();
    }

    // Set by the Lambda runtime itself and by nothing else, which is what makes it a reliable
    // answer to "is a trace going anywhere?"
    private static bool IsRunningInLambda =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME"));

    extension(IServiceCollection services)
    {
        private IServiceCollection AddVendorServices()
        {
            var region = RegionEndpoint.GetBySystemName(EnvReader.GetStringValue("AWS_REGION"));
            return services
                .AddDefaultAWSOptions(new AWSOptions { Region = region })
                .AddAWSService<IAmazonDynamoDB>()
                .AddAWSService<IAmazonSQS>()
                .AddAWSService<IAmazonSimpleNotificationService>()
                .AddAWSService<IAmazonScheduler>()
                .AddAWSService<IAmazonComprehend>()
                .AddAWSService<IAmazonS3>()
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
                        npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(2), errorCodesToAdd: null))
                    // DSQL accepts only REPEATABLE READ, including for the transactions EF opens
                    // by itself around a multi-statement SaveChanges.
                    .AddInterceptors(new RepeatableReadTransactionInterceptor())
                    // What the AWS SDK handler cannot see. Every other call this function makes
                    // goes through an AWS client and is traced by the pipeline handler registered
                    // in Build; DSQL is reached over Postgres, so without this a trace shows the
                    // invocation's total time with the database work as an unexplained gap in it.
                    //
                    // false, so the SQL text is recorded without its parameter values. The
                    // parameters here are participant email addresses and hat ids, and a trace is
                    // a place they would be retained on terms nothing else in this application
                    // sets -- see the retention reasoning in cloudwatch-log-groups.tf for the same
                    // argument applied to logs.
                    .AddXRayInterceptor(false));
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
                .AddSingleton<IValidator<UpdateProfileRequest>, UpdateProfileRequestValidator>()
                .AddSingleton<IValidator<RequestMagicLinkRequest>, RequestMagicLinkRequestValidator>()
                .AddSingleton<IValidator<RedeemMagicLinkRequest>, RedeemMagicLinkRequestValidator>()
                .AddSingleton<IValidator<CloseHatRequest>, CloseHatRequestValidator>()
                .AddSingleton<IValidator<CopyHatRequest>, CopyHatRequestValidator>()
                .AddSingleton<IValidator<CreateHatRequest>, CreateHatRequestValidator>()
                .AddSingleton<IValidator<EditHatRequest>, EditHatRequestValidator>()
                .AddSingleton<IValidator<EditParticipantRequest>, EditParticipantRequestValidator>()
                .AddSingleton<IValidator<PreviewInvitationsRequest>, PreviewInvitationsRequestValidator>()
                .AddSingleton<IValidator<SendInvitationsRequest>, SendInvitationsRequestValidator>()
                .AddSingleton<IValidator<EditParticipantAddressRequest>, EditParticipantAddressRequestValidator>()
                .AddSingleton<IValidator<SubmitFeedbackRequest>, SubmitFeedbackRequestValidator>()
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

                // Two methods, one service. The GET renders a confirmation page and the POST
                // behind it performs the Ask, so that a mail scanner following the link in an
                // invitation cannot send it on somebody's behalf.
                .AddKeyedSingleton<IApiGatewayHandler, AskForGiftIdeasService>("get/ask/{token}")
                .AddKeyedSingleton<IApiGatewayHandler, AskForGiftIdeasService>("post/ask/{token}")

                .AddKeyedSingleton<IApiGatewayHandler, GetHatService>("get/hat/{email}/{id}")
                .AddKeyedSingleton<IApiGatewayHandler, GetHatsService>("get/hats/{email}")

                .AddKeyedSingleton<IApiGatewayHandler, CreateHatService>("post/hat")
                .AddKeyedSingleton<IApiGatewayHandler, EditHatService>("put/hat")

                .AddKeyedSingleton<IApiGatewayHandler, UpdateProfileService>("put/profile")

                // Authenticated, because the footer that opens the contact form only renders on
                // signed-in pages. That is what keeps this off the list of things needing a
                // CAPTCHA: there is no anonymous route to it, and the sender's address comes from
                // the session rather than from a box somebody can type anything into.
                .AddKeyedSingleton<IApiGatewayHandler, SubmitFeedbackService>("post/feedback")

                .AddKeyedSingleton<IApiGatewayHandler, DeleteHatService>("delete/hat")

                .AddKeyedSingleton<IApiGatewayHandler, AddParticipantService>("post/participant")
                .AddKeyedSingleton<IApiGatewayHandler, EditParticipantService>("put/participant")
                .AddKeyedSingleton<IApiGatewayHandler, GetParticipantService>("get/participant/{organizeremail}/{hatid}/{participantemail}")
                .AddKeyedSingleton<IApiGatewayHandler, RemoveParticipantService>("delete/participant")

                // Its own endpoint rather than part of put/participant, which edits eligibility
                // and resets the hat to IN_PROGRESS when it does — correct before the draw, and
                // ruinous after invitations have gone out.
                .AddKeyedSingleton<IApiGatewayHandler, EditParticipantAddressService>("put/participant/address")

                .AddKeyedSingleton<IApiGatewayHandler, ValidationService>("post/hat/validate")

                .AddKeyedSingleton<IApiGatewayHandler, AssignRecipientsService>("post/recipients")

                .AddKeyedSingleton<IApiGatewayHandler, PreviewInvitationsService>("get/hat/{email}/previewinvitations/{id}")
                .AddKeyedSingleton<IApiGatewayHandler, EnqueueInvitationsService>("post/hat/sendinvitations")
                .AddKeyedSingleton<IApiGatewayHandler, CloseHatService>("post/hat/close")
                .AddKeyedSingleton<IApiGatewayHandler, CopyHatService>("post/hat/copy")

                .AddSingleton<ValidationService>() // registered separately for direct use
                .AddSingleton<EmailCompositionService>()
                .AddSingleton<CompletionEmailCompositionService>()
                .AddSingleton<GiftIdeaEmailCompositionService>()
                .AddSingleton<GiftIdeaContentPolicy>()
                .AddSingleton<InboundEmailParser>()
                .AddSingleton<IReplyThrottleProvider, ReplyThrottleProvider>()
                .AddSingleton<AutomaticEmailSender>()
                .AddSingleton<IEmailQueue, EmailQueue>()
                .AddSingleton<AskPageComposer>()
                .AddSingleton<InboundGiftIdeasService>()
                .AddSingleton<InvitationQueueHandlerService>()
                .AddSingleton<DeliveryEventsService>()
                .AddSingleton<ISchedulerService, SchedulerService>()
            ;
    }
}
