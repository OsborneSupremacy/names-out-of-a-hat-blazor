using System.Text;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace GiftExchange.Library.Services;

internal class StartOrganizerVerificationService : IApiGatewayHandler
{
    private readonly ApiGatewayAdapter _adapter;

    private readonly GiftExchangeProvider _giftExchangeProvider;

    private readonly JsonService _jsonService;

    private readonly IAmazonSQS _sqsClient;

    private readonly string _queueUrl;

    public StartOrganizerVerificationService(
        GiftExchangeProvider giftExchangeProvider,
        ApiGatewayAdapter adapter,
        JsonService jsonService,
        IAmazonSQS sqsClient
    )
    {
        _giftExchangeProvider = giftExchangeProvider ?? throw new ArgumentNullException(nameof(giftExchangeProvider));
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        _jsonService = jsonService ?? throw new ArgumentNullException(nameof(jsonService));
        _sqsClient = sqsClient ?? throw new ArgumentNullException(nameof(sqsClient));
        _queueUrl = EnvReader.GetStringValue("INVITATIONS_QUEUE_URL");
    }

    public Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        => _adapter.AdaptAsync<StartOrganizerVerificationRequest, StatusCodeOnlyResponse>(request, InitiateOrganizerVerificationAsync);

    internal async Task<Result<StatusCodeOnlyResponse>> InitiateOrganizerVerificationAsync(
        StartOrganizerVerificationRequest request
        )
    {
        var (hatExists, hat) = await _giftExchangeProvider
            .GetHatAsync(request.OrganizerEmail, request.HatId)
            .ConfigureAwait(false);

        if(!hatExists)
            return new Result<StatusCodeOnlyResponse>(new KeyNotFoundException($"HatId {hat.Id} not found"), HttpStatusCode.NotFound);

        var verificationCode = new Random().Next(1000, 9999).ToString();

        var jsonInvitation = _jsonService.SerializeDefault(new GiftExchangeEmailRequest
        {
            HatId = request.HatId,
            OrganizerEmail = request.OrganizerEmail,
            HtmlBody =  ComposeVerificationEmailAsync(hat, verificationCode),
            RecipientEmail = hat.Organizer.Email,
            Subject = "Your Names Out Of A Hat Verification Code"
        });

        await _giftExchangeProvider
            .CreateVerificationCodeAsync(request.OrganizerEmail, request.HatId, verificationCode)
            .ConfigureAwait(false);

        await _sqsClient
            .SendMessageAsync(new SendMessageRequest
            {
                QueueUrl = _queueUrl,
                MessageBody = jsonInvitation,
                MessageGroupId = $"group-organizer-verification-{hat.Id}-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}"
            }).
            ConfigureAwait(false);

        return new Result<StatusCodeOnlyResponse>(new StatusCodeOnlyResponse { StatusCode = HttpStatusCode.OK}, HttpStatusCode.OK);
    }

    private string ComposeVerificationEmailAsync(Hat hat, string code)
    {
        List<string> e =
        [
            $"Dear {hat.Organizer.Name},",
            "Your 🎩 Names Out Of A Hat 🎩 verification code is:",
            $"<b>{code}</b>",
            $"-Names Out Of A Hat"
        ];

        StringBuilder s = new();
        foreach (var i in e)
        {
            s.Append(i);
            s.AppendLine("<br /><br />");
        }
        return s.ToString();
    }
}
