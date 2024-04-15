// <copyright company="Vermessungsamt Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Vermessungsamt Winterthur. All rights reserved.
// </copyright>
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Npgsql;
using roadwork_portal_service.Model;
using roadwork_portal_service.Configuration;
using roadwork_portal_service.Helper;

namespace roadwork_portal_service.Controllers;

[ApiController]
[Route("Account/[controller]")]
public class LoginController : ControllerBase
{
    private readonly ILogger<LoginController> _logger;

    public LoginController(ILogger<LoginController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Authenticates a user and returns a security token string
    /// </summary>
    /// <param name="receivedUser"></param>
    /// <param name="dryRun"></param>
    /// <returns>A security token string for the authenticated user</returns>
    /// <remarks>
    /// Sample request:
    ///     POST /Account/Login
    ///     {
    ///        "mailAddress": "...",
    ///        "passPhrase": "..."
    ///     }
    /// </remarks>
    /// <response code="200">Returns the security token</response>
    /// <response code="400">If user data is missing</response>
    /// <response code="401">If user could not be authenticated</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Login([FromBody] User receivedUser, bool dryRun = false)
    {
        if (receivedUser == null || receivedUser.mailAddress == null)
        {
            // login data is missing something important, thus:
            _logger.LogWarning("No or bad login credentials provided in a login attempt.");
            return BadRequest("No or bad login credentials provided.");
        }

        receivedUser.mailAddress = receivedUser.mailAddress.ToLower().Trim();

        if (receivedUser.mailAddress == String.Empty || receivedUser.mailAddress.Any(Char.IsWhiteSpace))
        {
            // login data is missing something important, thus:
            _logger.LogWarning("No or bad login credentials provided in a login attempt.");
            return BadRequest("No or bad login credentials provided.");
        }

        _logger.LogInformation("User " + receivedUser.mailAddress + " tries to log in.");

        if (receivedUser.passPhrase == null || receivedUser.passPhrase.Trim().Equals(""))
        {
            // login data is missing something important, thus:
            _logger.LogWarning("No or bad login credentials provided by user.");
            return BadRequest("No or bad login credentials provided.");
        }

        _logger.LogInformation("User " + receivedUser.mailAddress + " provided a non-empty password. Now trying to authenticate...");

        // get corresponding user from database:
        User userFromDb = LoginController._getUserFromDatabase(receivedUser.mailAddress, dryRun);

        LoginController._updateLoginTimestamp(receivedUser.mailAddress, dryRun);

        if (userFromDb != null)
        {
            _logger.LogInformation("User " + receivedUser.mailAddress + " was found in the database.");
            
            if (!userFromDb.active)
            {
                _logger.LogWarning("User " + receivedUser.mailAddress + " was inactivated for login but still tried to login.");
                return BadRequest("The user is inactivated for login.");
            }

            if (userFromDb.lastLoginAttempt != null)
            {
                // prohibit brute force attack:
                DateTime currentDatabaseTime = (DateTime)userFromDb.databaseTime;
                DateTime lastLoginAttemptTime = (DateTime)userFromDb.lastLoginAttempt;
                double diffInSeconds = (currentDatabaseTime - lastLoginAttemptTime).TotalSeconds;
                if(diffInSeconds < 3)
                {
                    Thread.Sleep(3000);
                }
            }

            string hashedPassphrase = HelperFunctions.hashPassphrase(receivedUser.passPhrase);

            if (userFromDb.mailAddress != null && userFromDb.passPhrase != null
                && userFromDb.passPhrase.Equals(hashedPassphrase))
            {
                string securityKey = AppConfig.Configuration.GetValue<string>("SecurityKey");
                byte[] securityKeyByteArray = Encoding.UTF8.GetBytes(securityKey);
                SymmetricSecurityKey key = new SymmetricSecurityKey(securityKeyByteArray);
                SigningCredentials signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                List<Claim> userClaims = new List<Claim>();
                userClaims.Add(new Claim(ClaimTypes.NameIdentifier, userFromDb.uuid));
                userClaims.Add(new Claim(ClaimTypes.Email, userFromDb.mailAddress));
                userClaims.Add(new Claim(ClaimTypes.GivenName, userFromDb.firstName));
                userClaims.Add(new Claim(ClaimTypes.Name, userFromDb.lastName));
                userClaims.Add(new Claim(ClaimTypes.Role, userFromDb.role.code));

                string serviceDomain = AppConfig.Configuration.GetValue<string>("URL:ServiceDomain");
                string serviceBasePath = AppConfig.Configuration.GetValue<string>("URL:ServiceBasePath");

                JwtSecurityToken securityToken = new JwtSecurityToken(
                    issuer: serviceDomain + serviceBasePath,
                    audience: serviceDomain + serviceBasePath,
                    claims: userClaims,
                    signingCredentials: signingCredentials,
                    expires: DateTime.UtcNow.AddDays(2)
                );

                string securityTokenString = new JwtSecurityTokenHandler().WriteToken(securityToken);

                _logger.LogInformation("User " + receivedUser.mailAddress + " has logged in.");
                return Ok(new { securityTokenString });
            }
            else
            {
                _logger.LogWarning("The provided credentials of user " + receivedUser.mailAddress + 
                        " did not match with the credentials in the database.");
            }
        }
        else
        {
            _logger.LogWarning("User " + receivedUser.mailAddress + " could not be found in the database.");
        }
        _logger.LogWarning("User " + receivedUser.mailAddress + " is not authenticated.");
        return Unauthorized("Sie sind entweder nicht als Benutzer " +
                    "erfasst oder Sie haben keine Zugriffsberechtigung.");
    }


    public static User getAuthorizedUserFromDb(ClaimsPrincipal userFromService, bool dryRun)
    {
        Claim userMailAddressClaim = null;
        foreach (Claim userClaim in userFromService.Claims)
        {
            if (userClaim.Type == ClaimTypes.Email)
            {
                userMailAddressClaim = userClaim;
            }
        }
        string userMailAddress = null;
        if (userMailAddressClaim != null)
        {
            userMailAddress = userMailAddressClaim.Value.Trim().ToLower();
        }
        if (userMailAddress == null || userMailAddress.Equals(""))
        {
            return null;
        }
        User userFromDb = LoginController._getUserFromDatabase(userMailAddress, dryRun);
        return userFromDb;
    }

    private static User _getUserFromDatabase(string eMailAddress, bool dryRun)
    {
        if(dryRun) return null;

        User userFromDb = null;

        // get data of current user from database:
        using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
        {
            pgConn.Open();
            NpgsqlCommand selectComm = pgConn.CreateCommand();
            selectComm.CommandText = @"SELECT u.uuid, u.last_name, u.first_name, u.e_mail, u.pwd,
                        u.last_login_attempt, CURRENT_TIMESTAMP(0)::TIMESTAMP,
                        wtb_ssp_roles.code, wtb_ssp_roles.name, u.org_unit, o.name,
                        o.abbreviation, u.active
                        FROM ""wtb_ssp_users"" u
                        LEFT JOIN ""wtb_ssp_roles"" ON u.role = wtb_ssp_roles.code
                        LEFT JOIN ""wtb_ssp_organisationalunits"" o ON u.org_unit = o.uuid
                        WHERE trim(lower(u.e_mail))=@e_mail";
            selectComm.Parameters.AddWithValue("e_mail", eMailAddress);

            using (NpgsqlDataReader reader = selectComm.ExecuteReader())
            {
                Boolean hasUser = reader.Read();
                if (hasUser)
                {
                    userFromDb = new User();
                    userFromDb.uuid = reader.GetGuid(0).ToString();
                    userFromDb.mailAddress = reader.GetString(3);
                    userFromDb.passPhrase = reader.GetString(4);

                    userFromDb.lastName = reader.GetString(1);
                    userFromDb.firstName = reader.GetString(2);
                    userFromDb.lastLoginAttempt = !reader.IsDBNull(5) ? reader.GetDateTime(5) : null;
                    userFromDb.databaseTime = !reader.IsDBNull(6) ? reader.GetDateTime(6) : null;
                    Role role = new Role();
                    role.code = reader.GetString(7);
                    role.name = reader.GetString(8);
                    userFromDb.role = role;
                    OrganisationalUnit orgUnit = new OrganisationalUnit();
                    orgUnit.uuid = reader.GetGuid(9).ToString();
                    orgUnit.name = reader.GetString(10);
                    orgUnit.abbreviation = reader.GetString(11);
                    userFromDb.organisationalUnit = orgUnit;
                    userFromDb.active = reader.GetBoolean(12);

                    if (userFromDb.lastName == null || userFromDb.lastName.Trim().Equals(""))
                    {
                        userFromDb.lastName = "Nachname unbekannt";
                    }

                    if (userFromDb.firstName == null || userFromDb.firstName.Trim().Equals(""))
                    {
                        userFromDb.firstName = "Vorname unbekannt";
                    }

                }
            }
            pgConn.Close();
        }

        return userFromDb;
    }

    private static void _updateLoginTimestamp(string eMailAddress, bool dryRun)
    {
        if(dryRun) return;

        using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
        {
            pgConn.Open();
            NpgsqlCommand updateTimestampComm = pgConn.CreateCommand();
            updateTimestampComm.CommandText = "UPDATE \"wtb_ssp_users\"" +
                        " SET last_login_attempt=CURRENT_TIMESTAMP " +
                        " WHERE trim(lower(e_mail))=@e_mail";
            updateTimestampComm.Parameters.AddWithValue("e_mail", eMailAddress);
            updateTimestampComm.ExecuteNonQuery();

            pgConn.Close();
        }
    }

}

