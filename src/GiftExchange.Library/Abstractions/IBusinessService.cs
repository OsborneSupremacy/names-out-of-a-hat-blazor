namespace GiftExchange.Library.Abstractions;

public interface IBusinessService<in TRequest, TResponse>
{
    Task<Result<TResponse>> ExecuteAsync(TRequest request, ILambdaContext context);
}
