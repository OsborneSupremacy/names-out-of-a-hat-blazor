namespace GiftExchange.Library.Abstractions;

/// <summary>
/// Transforms request parameters into a strongly typed request object.
/// </summary>
/// <typeparam name="TTransformed"></typeparam>
public interface IHasRequestParameters<TTransformed>
{
    public Result<TTransformed> Transform(APIGatewayProxyRequest request);
}
