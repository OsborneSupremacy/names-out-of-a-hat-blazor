namespace GiftExchange.Library.Abstractions;

public interface IServiceWithResponseBody<in TRequest, TResponse>
{
    Task<Result<TResponse>> ExecuteAsync(TRequest t, ILambdaContext context);
}
