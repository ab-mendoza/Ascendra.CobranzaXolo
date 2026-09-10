using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Ascendra.CobranzaXolo.Api.Security;

public sealed class BasicAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptions<SimpleAuthenticationOptions> credentials)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string AuthenticationScheme = "AscendraBasic";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderNames.Authorization, out var header))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!AuthenticationHeaderValue.TryParse(header, out var parsed)
            || !"Basic".Equals(parsed.Scheme, StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(parsed.Parameter))
        {
            return Task.FromResult(AuthenticateResult.Fail("El encabezado de acceso no es válido."));
        }

        string rawCredentials;
        try
        {
            rawCredentials = Encoding.UTF8.GetString(Convert.FromBase64String(parsed.Parameter));
        }
        catch (FormatException)
        {
            return Task.FromResult(AuthenticateResult.Fail("Las credenciales no son válidas."));
        }

        var separator = rawCredentials.IndexOf(':');
        if (separator < 0)
        {
            return Task.FromResult(AuthenticateResult.Fail("Las credenciales no son válidas."));
        }

        var username = rawCredentials[..separator];
        var password = rawCredentials[(separator + 1)..];
        var configured = credentials.Value;
        if (string.IsNullOrWhiteSpace(configured.Username)
            || string.IsNullOrWhiteSpace(configured.Password)
            || !Matches(username, configured.Username)
            || !Matches(password, configured.Password))
        {
            return Task.FromResult(AuthenticateResult.Fail("Usuario o contraseña incorrectos."));
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, configured.Username)],
            AuthenticationScheme);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), AuthenticationScheme);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.Append(HeaderNames.WWWAuthenticate, "Basic realm=\"Ascendra\"");
        return Task.CompletedTask;
    }

    private static bool Matches(string actual, string expected) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(actual),
            Encoding.UTF8.GetBytes(expected));
}