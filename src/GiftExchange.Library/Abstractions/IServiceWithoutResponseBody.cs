namespace GiftExchange.Library.Abstractions;

public interface IServiceWithoutResponseBody<in TRequest>
{
    Task<Result> ExecuteAsync(TRequest t, ILambdaContext context);
}
