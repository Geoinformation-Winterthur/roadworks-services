using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace roadwork_portal_service.Controllers;

[ApiController]
[Route("Account/[controller]")]
public class OpenIdLoginController : ControllerBase
{
    private readonly ILogger<OpenIdLoginController> _logger;

    public OpenIdLoginController(ILogger<OpenIdLoginController> logger)
    {
        _logger = logger;
    }


    /// Sample request:
    ///     POST /Account/OpenIdLogin
    ///     {
    ///        [idToken]
    ///     }
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] string idToken, bool dryRun = false)
    {
        if (string.IsNullOrEmpty(idToken))
        {
            // login data is missing something important, thus:
            _logger.LogWarning("No or bad idToken provided in a login attempt.");
            return BadRequest("No or bad idToken provided.");
        }

        bool tokenValid = true;
        // bool tokenValid = await _isTokenValid(idToken);

        if(tokenValid)
        {
            return Ok(new { idToken });
        }
        else
        {
            return Unauthorized("Sie sind entweder nicht als Benutzer " +
                "erfasst oder Sie haben keine Zugriffsberechtigung.");
        }

    }

    private static async Task<bool> _isTokenValid(string idToken)
    {
        const string tokenIssuer = "https://accounts.google.com";

        ConfigurationManager<OpenIdConnectConfiguration> oidManager
                = new ConfigurationManager<OpenIdConnectConfiguration>(
                    tokenIssuer,
                    new OpenIdConnectConfigurationRetriever(),
                    new HttpDocumentRetriever());

        OpenIdConnectConfiguration oidConf = await oidManager.GetConfigurationAsync();
        ICollection<SecurityKey> secKeys = oidConf.SigningKeys;

        TokenValidationParameters valParams = new TokenValidationParameters();
        valParams.RequireExpirationTime = true;
        valParams.RequireSignedTokens = true;
        valParams.ValidateIssuer = true;
        valParams.ValidIssuer = tokenIssuer;
        valParams.ValidateIssuerSigningKey = true;
        valParams.IssuerSigningKeys = secKeys;
        valParams.ValidateLifetime = true;
        valParams.ClockSkew = TimeSpan.FromMinutes(3);

        SecurityToken validatedToken;
        new JwtSecurityTokenHandler().ValidateToken(idToken, valParams, out validatedToken);
        return validatedToken != null;
    }

}