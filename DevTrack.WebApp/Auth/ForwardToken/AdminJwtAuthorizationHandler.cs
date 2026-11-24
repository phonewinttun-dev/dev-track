using System.Net;

namespace DevTrack.WebApp.Auth;

public partial class AdminJwtAuthorizationHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly ILogger<AdminJwtAuthorizationHandler> logger;

    public AdminJwtAuthorizationHandler(
        IHttpContextAccessor httpContextAccessor,
        ILogger<AdminJwtAuthorizationHandler> logger)
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
            #region Read stored token

            var tokenResult = await ReadAccessTokenAsync();

            if (tokenResult.IsFailure)
            {
                logger.LogInformation(
                    "SendAsync => Read stored token stopped. Reason={Reason}, NextAction=return server failure",
                    tokenResult.Message);

                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    RequestMessage = request
                };
            }

            logger.LogInformation(
                "SendAsync => Token lookup completed, Reason={Reason}, NextAction=prepare optional header",
                tokenResult.Message);

            #endregion

            #region Prepare authorization header

            var headerResult = PrepareAuthorizationHeader(tokenResult.Data);

            if (headerResult.IsFailure)
            {
                logger.LogInformation(
                    "SendAsync => Prepare authorization header stopped. Reason={Reason}, NextAction=return server failure",
                    headerResult.Message);

                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    RequestMessage = request
                };
            }

            var header = headerResult.Data;

            logger.LogInformation(
                "SendAsync => Optional header prepared, HasStoredToken={HasStoredToken}, Reason={Reason}, NextAction=forward request",
                header is not null,
                headerResult.Message);

            #endregion

            if (header is not null)
            {
                request.Headers.Authorization = header;
            }

            #region Forward request

            var responseResult = await ForwardRequestAsync(request, cancellationToken);

            if (responseResult.IsFailure)
            {
                logger.LogInformation(
                    "SendAsync => Forward request stopped. Reason={Reason}, NextAction=return server failure",
                    responseResult.Message);

                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    RequestMessage = request
                };
            }

            var response = responseResult.Data!;

            logger.LogInformation(
                "SendAsync => Request forwarded, StatusCode={StatusCode}, Reason={Reason}, NextAction=return caller-owned response",
                (int)response.StatusCode,
                responseResult.Message);

            #endregion

            return response;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "SendAsync => API request forwarding failed.");

            throw;
        }
    }
}
