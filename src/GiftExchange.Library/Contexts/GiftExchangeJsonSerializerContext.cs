using System.Text.Json.Serialization;

namespace GiftExchange.Library.Contexts;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true
)]
[JsonSerializable(typeof(AddParticipantRequest))]
[JsonSerializable(typeof(AssignRecipientsRequest))]
[JsonSerializable(typeof(CloseHatRequest))]
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
[JsonSerializable(typeof(HatPreconditionRequest))]
[JsonSerializable(typeof(HatPreconditionResponse))]
[JsonSerializable(typeof(RemoveParticipantRequest))]
[JsonSerializable(typeof(SendInvitationsRequest))]
[JsonSerializable(typeof(StatusCodeOnlyResponse))]
[JsonSerializable(typeof(ValidateHatRequest))]
[JsonSerializable(typeof(ValidateHatResponse))]
internal partial class GiftExchangeJsonSerializerContext : JsonSerializerContext
{
}
