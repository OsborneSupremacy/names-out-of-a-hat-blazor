using Amazon.Lambda.SimpleEmailEvents;
using Amazon.Lambda.SimpleEmailEvents.Actions;
using Amazon.Lambda.SQSEvents;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace GiftExchange.Library.Contexts;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true
)]
[JsonSerializable(typeof(APIGatewayProxyRequest))]
[JsonSerializable(typeof(APIGatewayProxyResponse))]
[JsonSerializable(typeof(AddParticipantRequest))]
[JsonSerializable(typeof(AssignRecipientsRequest))]
[JsonSerializable(typeof(CloseHatRequest))]
[JsonSerializable(typeof(CopyHatRequest))]
[JsonSerializable(typeof(CopyHatResponse))]
[JsonSerializable(typeof(CreateHatRequest))]
[JsonSerializable(typeof(CreateHatResponse))]
[JsonSerializable(typeof(DeleteHatRequest))]
[JsonSerializable(typeof(EditHatRequest))]
[JsonSerializable(typeof(EditParticipantRequest))]
[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(GetHatRequest))]
[JsonSerializable(typeof(GetHatsRequest))]
[JsonSerializable(typeof(GetHatsResponse))]
[JsonSerializable(typeof(GetParticipantRequest))]
[JsonSerializable(typeof(GiftExchangeEmailRequest))]
[JsonSerializable(typeof(HatCooledOffScheduleRequest))]
[JsonSerializable(typeof(HatPreconditionRequest))]
[JsonSerializable(typeof(HatPreconditionResponse))]
[JsonSerializable(typeof(PreviewInvitationsRequest))]
[JsonSerializable(typeof(PreviewInvitationsResponse))]
[JsonSerializable(typeof(APIGatewayCustomAuthorizerRequest))]
[JsonSerializable(typeof(APIGatewayCustomAuthorizerResponse))]
[JsonSerializable(typeof(RequestMagicLinkRequest))]
[JsonSerializable(typeof(RedeemMagicLinkRequest))]
[JsonSerializable(typeof(RedeemMagicLinkResponse))]
[JsonSerializable(typeof(RemoveParticipantRequest))]
[JsonSerializable(typeof(SendInvitationsRequest))]
// The assembly-level LambdaSerializer applies to every handler, so each entry point's event type
// has to be here. LambdaHandlerSerializationTests keeps that honest.
[JsonSerializable(typeof(SQSEvent))]
// The SES receipt rule invokes InboundGiftIdeasHandler with this. Closed over LambdaReceiptAction
// because that is the action the rule ends in; the S3 action before it writes the message to the
// bucket and reports nothing back here.
[JsonSerializable(typeof(SimpleEmailEvent<LambdaReceiptAction>))]
[JsonSerializable(typeof(StatusCodeOnlyResponse))]
[JsonSerializable(typeof(UpdateProfileRequest))]
[JsonSerializable(typeof(ValidateHatRequest))]
[JsonSerializable(typeof(ValidateHatResponse))]
internal partial class GiftExchangeJsonSerializerContext : JsonSerializerContext
{
}

public static class GiftExchangeJsonTypeInfoResolver
{
    public static IJsonTypeInfoResolver Default => GiftExchangeJsonSerializerContext.Default;
}
