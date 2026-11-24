namespace DevTrack.WebApp.Auth;

public partial class AuthApiClient
{
    #region Shared transport dependencies

    private readonly IHttpClientFactory clientFactory;
    private readonly ILogger<AuthApiClient> logger;

    public AuthApiClient(IHttpClientFactory clientFactory, ILogger<AuthApiClient> logger)
    {
        #region Store API client dependencies

        this.clientFactory = clientFactory;
        this.logger = logger;

        #endregion
    }

    #endregion
}
