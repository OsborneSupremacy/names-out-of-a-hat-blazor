namespace GiftExchange.Library.Abstractions;

public interface IApiGatewayHandler
{
    public Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context);
}
