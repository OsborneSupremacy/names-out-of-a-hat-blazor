namespace GiftExchange.Library.Utility;

/// <summary>
/// Boilerplate code for adapting an ApiGateway request and response to/from a business class
/// request/response.
/// </summary>
internal class ApiGatewayAdapter
{
    private readonly ILogger<AddParticipantService> _logger;

    private readonly JsonService _jsonService;

    public ApiGatewayAdapter(
        ILogger<AddParticipantService> logger,
        JsonService jsonService
        )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jsonService = jsonService ?? throw new ArgumentNullException(nameof(jsonService));
    }

    public async Task<APIGatewayProxyResponse> AdaptAsync<TRequest, TResponse>(
        APIGatewayProxyRequest request,
        Func<TRequest, Task<Result<TResponse>>> handler
    )
    {
        var innerRequest = request.GetInnerRequest<TRequest>(_jsonService);

        if(innerRequest.IsFaulted)
            return ProxyResponseBuilder.Build(innerRequest.StatusCode, innerRequest.Exception.Message);

        var result = await handler(innerRequest.Value);

        if(result.IsFaulted)
            return ProxyResponseBuilder
                .Build(result.StatusCode, BuildSerializedErrorResponse(result.Exception.Message));

        if(result.Value is StatusCodeOnlyResponse)
            return ProxyResponseBuilder.Build(result.StatusCode);

        return ProxyResponseBuilder
            .Build(result.StatusCode, _jsonService.SerializeDefault(result.Value));
    }

    public async Task<APIGatewayProxyResponse> AdaptAsync<TRequest, TResponse>(
        TRequest innerRequest,
        Func<TRequest, Task<Result<TResponse>>> handler
    )
    {
        var result = await handler(innerRequest);

        if(result.IsFaulted)
            return ProxyResponseBuilder.Build(result.StatusCode, BuildSerializedErrorResponse(result.Exception.Message));

        if(result.Value is StatusCodeOnlyResponse)
            return ProxyResponseBuilder.Build(result.StatusCode);

        return ProxyResponseBuilder
            .Build(result.StatusCode, _jsonService.SerializeDefault(result.Value));
    }

    private string BuildSerializedErrorResponse(string errorMessage)
    {
        var errorResponse = new ErrorResponse
        {
            Message = errorMessage
        };
        return _jsonService.SerializeDefault(errorResponse);
    }
}
