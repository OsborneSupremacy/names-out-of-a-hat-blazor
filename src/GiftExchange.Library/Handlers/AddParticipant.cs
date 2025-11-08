
namespace GiftExchange.Library.Handlers;

public class AddParticipant
{
    public static async Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest request,
        ILambdaContext context
    )
    {
        var innerRequest = JsonService.DeserializeDefault<AddParticipantRequest>(request.Body);
        if (innerRequest == null)
            return ApiGatewayProxyResponses.BadRequest;

        var personId = Guid.NewGuid();
        /*
         real logic coming later
         */

        return new APIGatewayProxyResponse
        {
            StatusCode = (int)HttpStatusCode.OK,
            Headers = CorsHeaderService.GetCorsHeaders(),
            Body = JsonService.SerializeDefault(new AddParticipantResponse
            {
                PersonId = personId
            })
        };
    }
}
