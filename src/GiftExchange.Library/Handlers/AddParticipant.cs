
namespace GiftExchange.Library.Handlers;

public class AddParticipant
{
    public static async Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest request,
        ILambdaContext context
    ) =>
        await PayloadHandler
            .FunctionHandler<AddParticipantRequest, AddParticipantResponse>(
                request,
                InnerHandler,
                context
            );

    private static Task<Result<AddParticipantResponse>> InnerHandler(AddParticipantRequest request)
    {
        var response = new AddParticipantResponse { PersonId = Guid.NewGuid() };
        var result = new Result<AddParticipantResponse>(response, HttpStatusCode.Created);
        return Task.FromResult(result);
    }
}
