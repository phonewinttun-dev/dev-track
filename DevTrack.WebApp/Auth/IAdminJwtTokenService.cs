namespace DevTrack.WebApp.Auth;

public interface IAdminJwtTokenService
{
    string CreateToken(IEnumerable<System.Security.Claims.Claim> claims);
    string CreateAdminToken(string username, string displayName);
}
