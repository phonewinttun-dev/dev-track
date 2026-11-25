namespace DevTrack.WebApp.Auth;

public partial class TokenSendHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly ILogger<TokenSendHandler> logger;

    public TokenSendHandler(
        IHttpContextAccessor httpContextAccessor,
        ILogger<TokenSendHandler> logger)
    {
        #region Store forwarding dependencies

        this.httpContextAccessor = httpContextAccessor;
        this.logger = logger;

        #endregion
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            #region Attach the optional bearer credential and forward the request

            var token = await ReadAccessTokenAsync();
            var header = PrepareAuthorizationHeader(token);

            if (header is not null)
            {
                request.Headers.Authorization = header;
            }

            var response = await base.SendAsync(request, cancellationToken);
            logger.LogInformation("SendAsync => API request forwarded. StatusCode={StatusCode}", (int)response.StatusCode);

            return response;

            #endregion
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "SendAsync => API request forwarding failed.");

            throw;
        }
    }
}
