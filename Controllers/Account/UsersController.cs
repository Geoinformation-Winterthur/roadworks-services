// <copyright company="Vermessungsamt Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Vermessungsamt Winterthur. All rights reserved.
// </copyright>
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using roadwork_portal_service.Model;
using roadwork_portal_service.Configuration;
using roadwork_portal_service.Helper;
using System.Security.Claims;
using System.Net.Mail;

namespace roadwork_portal_service.Controllers;

[ApiController]
[Route("Account/[controller]")]
public class UsersController : ControllerBase
{
    private readonly ILogger<UsersController> _logger;

    public UsersController(ILogger<UsersController> logger)
    {
        _logger = logger;
    }


    // GET /account/users/
    // GET /account/users/?email=...
    // GET /account/users/?uuid=...
    [HttpGet]
    [Authorize]
    public ActionResult<User[]> GetUsers(string? email, string? uuid, string? role)
    {
        List<User> usersFromDb = new List<User>();
        // get data of current user from database:
        using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
        {
            pgConn.Open();
            NpgsqlCommand selectComm = pgConn.CreateCommand();
            selectComm.CommandText = @"SELECT u.uuid, u.last_name, u.first_name,
                                trim(lower(u.e_mail)), wtb_ssp_roles.code,
                                wtb_ssp_roles.name, u.org_unit, o.name,
                                o.abbreviation, o.is_civil_eng, u.active,
                                u.pref_table_view
                            FROM ""wtb_ssp_users"" u
                            LEFT JOIN ""wtb_ssp_roles"" ON u.role = wtb_ssp_roles.code
                            LEFT JOIN ""wtb_ssp_organisationalunits"" o ON u.org_unit = o.uuid";

            if (email != null)
            {
                email = email.ToLower().Trim();
            }

            if (email != null && email != "")
            {
                selectComm.CommandText += " WHERE trim(lower(u.e_mail))=@email";
                selectComm.Parameters.AddWithValue("email", email);
            }
            else if (uuid != null)
            {
                uuid = uuid.Trim().ToLower();
                if (uuid != "")
                {
                    selectComm.CommandText += " WHERE u.uuid=@uuid";
                    selectComm.Parameters.AddWithValue("uuid", new Guid(uuid));
                }
            }
            else if (role != null)
            {
                role = role.Trim().ToLower();
                if (role != "")
                {
                    selectComm.CommandText += " WHERE wtb_ssp_roles.code=@role";
                    selectComm.Parameters.AddWithValue("role", role);
                }
            }
            selectComm.CommandText += " ORDER BY u.first_name, u.last_name";

            using (NpgsqlDataReader reader = selectComm.ExecuteReader())
            {
                User userFromDb;
                while (reader.Read())
                {
                    userFromDb = new User();
                    userFromDb.uuid = reader.IsDBNull(0) ? "" :
                                reader.GetGuid(0).ToString();
                    userFromDb.mailAddress =
                            reader.IsDBNull(3) ? "" :
                                    reader.GetString(3).ToLower().Trim();
                    if (userFromDb.mailAddress != null &&
                            userFromDb.mailAddress != "" &&
                            userFromDb.uuid != null && userFromDb.uuid != "")
                    {
                        userFromDb.lastName = reader.GetString(1);
                        userFromDb.firstName = reader.GetString(2);
                        Role roleObj = new Role
                        {
                            code = reader.GetString(4),
                            name = reader.GetString(5)
                        };
                        userFromDb.role = roleObj;
                        OrganisationalUnit orgUnit = new OrganisationalUnit
                        {
                            uuid = reader.GetGuid(6).ToString(),
                            name = reader.GetString(7),
                            abbreviation = reader.GetString(8),
                            isCivilEngineering = reader.GetBoolean(9)
                        };
                        userFromDb.organisationalUnit = orgUnit;
                        userFromDb.active = reader.GetBoolean(10);

                        if (userFromDb.lastName == null || userFromDb.lastName.Trim().Equals(""))
                        {
                            userFromDb.lastName = "unbekannt";
                        }

                        if (userFromDb.firstName == null || userFromDb.firstName.Trim().Equals(""))
                        {
                            userFromDb.firstName = "unbekannt";
                        }
                        userFromDb.prefTableView = reader.GetBoolean(11);

                        usersFromDb.Add(userFromDb);
                    }
                }
            }
            pgConn.Close();
        }

        return usersFromDb.ToArray<User>();
    }

    // POST /account/users/
    [HttpPost]
    [Authorize(Roles = "administrator")]
    public ActionResult<User> AddUser([FromBody] User user)
    {
        string userPassphrase = user.passPhrase;
        user.passPhrase = "";
        if (user == null || user.mailAddress == null)
        {
            _logger.LogWarning("Not enough user data provided in add user process.");
            User resultUser = new User();
            resultUser.errorMessage = "SSP-0";
            return Ok(resultUser);
        }

        user.mailAddress = user.mailAddress.ToLower().Trim();

        if (user.mailAddress == "" || user.mailAddress.Any(Char.IsWhiteSpace))
        {
            _logger.LogWarning("Not enough user data provided in add user process.");
            user.errorMessage = "SSP-0";
            return Ok(user);
        }

        if (user.mailAddress == "new")
        {
            _logger.LogWarning("User mail address 'new' not allowed.");
            user.errorMessage = "SSP-12";
            return Ok(user);
        }

        if (userPassphrase == null)
        {
            userPassphrase = "";
        }

        userPassphrase = userPassphrase.Trim();
        if (userPassphrase.Length < 8)
        {
            _logger.LogWarning("Not enough user data provided in add user process.");
            user.errorMessage = "SSP-0";
            return Ok(user);
        }

        User userInDb = new User();
        ActionResult<User[]> usersInDbResult = this.GetUsers(user.mailAddress, "", "");
        User[]? usersInDb = usersInDbResult.Value;
        if (usersInDb != null && usersInDb.Length > 0)
        {
            userInDb = usersInDb[0];
        }

        if (userInDb != null && userInDb.mailAddress != null &&
                    userInDb.mailAddress.Trim() == user.mailAddress)
        {
            _logger.LogWarning("Adding user not possible since user is already in database.");
            user.errorMessage = "SSP-13";
            return Ok(user);
        }

        using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
        {
            pgConn.Open();
            NpgsqlCommand insertComm = pgConn.CreateCommand();
            insertComm.CommandText = @"INSERT INTO ""wtb_ssp_users""(uuid,
                    last_name, first_name, e_mail, role, pwd, org_unit, active,
                    pref_table_view)
                    VALUES(@uuid, @last_name, @first_name, @e_mail, @role, @pwd,
                    @org_unit, @active, false)";
            Guid userUuid = Guid.NewGuid();
            user.uuid = userUuid.ToString();
            insertComm.Parameters.AddWithValue("uuid", new Guid(user.uuid));
            insertComm.Parameters.AddWithValue("last_name", user.lastName);
            insertComm.Parameters.AddWithValue("first_name", user.firstName);
            insertComm.Parameters.AddWithValue("e_mail", user.mailAddress);
            insertComm.Parameters.AddWithValue("role", user.role.code);
            string hashedPassphrase = HelperFunctions.hashPassphrase(userPassphrase);
            insertComm.Parameters.AddWithValue("pwd", hashedPassphrase);
            insertComm.Parameters.AddWithValue("org_unit", new Guid(user.organisationalUnit.uuid));
            insertComm.Parameters.AddWithValue("active", user.active);

            int noAffectedRows = insertComm.ExecuteNonQuery();

            pgConn.Close();

            if (noAffectedRows == 1)
            {
                return Ok(user);
            }

        }

        _logger.LogError("Something went wrong.");
        user.errorMessage = "SSP-3";
        return Ok(user);
    }

    // PUT /account/users/?changepassphrase=false
    [HttpPut]
    [Authorize]
    public ActionResult<ErrorMessage> UpdateUser([FromBody] User user, bool changePassphrase = false)
    {
        ErrorMessage errorResult = new ErrorMessage();
        try
        {
            string userPassphrase = user.passPhrase;
            user.passPhrase = "";
            if (user == null || user.uuid == null)
            {
                _logger.LogInformation("No user data provided by user in update user process.");
                errorResult.errorMessage = "SSP-0";
                return Ok(errorResult);
            }

            user.uuid = user.uuid.ToLower().Trim();

            if (user.uuid == "")
            {
                _logger.LogWarning("No user data provided by user in update user process.");
                errorResult.errorMessage = "SSP-0";
                return Ok(errorResult);
            }

            if (user.mailAddress == null)
            {
                user.mailAddress = "";
            }

            user.mailAddress = user.mailAddress.Trim().ToLower();

            try
            {
                MailAddress userMailAddress = new MailAddress(user.mailAddress);
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex.Message);
                return BadRequest("Bad e-mail address provided.");
            }

            string loggedInUserMail = "";
            Claim? loggedInUserMailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
            if (loggedInUserMailClaim != null)
            {
                loggedInUserMail = loggedInUserMailClaim.Value.Trim().ToLower();
            }

            if (!User.IsInRole("administrator") && user.mailAddress != loggedInUserMail)
            {
                return Unauthorized();
            }

            User userInDb = new User();
            ActionResult<User[]> usersInDbResult = this.GetUsers("", user.uuid, "");
            User[]? usersInDb = usersInDbResult.Value;
            if (usersInDb == null || usersInDb.Length != 1 || usersInDb[0] == null)
            {
                _logger.LogWarning("Updating user " + user.uuid + " is not possible since user is not in the database.");
                errorResult.errorMessage = "SSP-4";
                return Ok(errorResult);
            }

            userInDb = usersInDb[0];
            if (userInDb.role.code == "administrator")
            {
                int noOfActiveAdmins = _countNumberOfActiveAdmins();
                if (noOfActiveAdmins == 1)
                {
                    if (user.role.code != "administrator")
                    {
                        _logger.LogWarning("User tried to change role of last administrator. " +
                                "Role cannot be changed since there would be no administrator anymore.");
                        errorResult.errorMessage = "SSP-5";
                        return Ok(errorResult);
                    }

                    if (!user.active)
                    {
                        _logger.LogWarning("Administrator tried to set last administrator inactive. " +
                                "This is not allowed.");
                        errorResult.errorMessage = "SSP-6";
                        return Ok(errorResult);
                    }
                }
            }

            if (userInDb.role.code == "territorymanager" && user.role.code != "territorymanager")
            {
                if (_isAreaManagerAssigned(user.uuid))
                {
                    _logger.LogWarning("Administrator tried to change role of a territory manager " +
                            "who is in active charge of a territory. This is not allowed thus ignored.");
                    errorResult.errorMessage = "SSP-18";
                    return Ok(errorResult);
                }
            }

            if (user.role.code == "projectmanager")
            {
                user.active = false;
            }

            using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
            {
                pgConn.Open();
                NpgsqlCommand updateComm = pgConn.CreateCommand();
                updateComm.CommandText = "UPDATE \"wtb_ssp_users\" SET ";

                if (User.IsInRole("administrator"))
                {
                    updateComm.CommandText += @"last_name=@last_name,
                        first_name=@first_name, e_mail=@e_mail,
                        role=@role, org_unit=@org_unit, ";
                }

                updateComm.CommandText += @"active=@active,
                        pref_table_view=@pref_table_view
                        WHERE uuid=@uuid";

                if (User.IsInRole("administrator"))
                {
                    updateComm.Parameters.AddWithValue("last_name", user.lastName);
                    updateComm.Parameters.AddWithValue("first_name", user.firstName);
                    updateComm.Parameters.AddWithValue("e_mail", user.mailAddress);
                    updateComm.Parameters.AddWithValue("role", user.role.code);
                    updateComm.Parameters.AddWithValue("org_unit", new Guid(user.organisationalUnit.uuid));
                }

                updateComm.Parameters.AddWithValue("active", user.active);
                updateComm.Parameters.AddWithValue("pref_table_view", user.prefTableView);
                updateComm.Parameters.AddWithValue("uuid", new Guid(user.uuid));
                int noAffectedRowsStep1 = updateComm.ExecuteNonQuery();

                if (changePassphrase == true)
                {
                    userPassphrase = userPassphrase.Trim();
                    if (userPassphrase.Length < 8)
                    {
                        _logger.LogWarning("Not enough user data provided in add user process.");
                        errorResult.errorMessage = "SSP-0";
                        return Ok(errorResult);
                    }
                }

                int noAffectedRowsStep2 = 0;
                if (changePassphrase == true)
                {
                    string hashedPassphrase = HelperFunctions.hashPassphrase(userPassphrase);
                    updateComm.CommandText = @"UPDATE ""wtb_ssp_users"" SET
                                    pwd=@pwd WHERE uuid=@uuid";
                    updateComm.Parameters.AddWithValue("pwd", hashedPassphrase);
                    updateComm.Parameters.AddWithValue("uuid", new Guid(user.uuid));
                    noAffectedRowsStep2 = updateComm.ExecuteNonQuery();
                }

                pgConn.Close();

                if (noAffectedRowsStep1 == 1 &&
                    (!changePassphrase || noAffectedRowsStep2 == 1))
                {
                    return Ok(user);
                }

            }

        }
        catch (Exception ex)
        {
            _logger.LogError("Fatal error");
            errorResult.errorMessage = "SSP-3";
            return Ok(errorResult);
        }

        _logger.LogError("Fatal error");
        errorResult.errorMessage = "SSP-3";
        return Ok(errorResult);
    }


    // DELETE /users?email=...
    [HttpDelete]
    [Authorize(Roles = "administrator")]
    public ActionResult<ErrorMessage> DeleteUser(string email)
    {
        ErrorMessage errorResult = new ErrorMessage();

        if (email == null)
        {
            _logger.LogWarning("No user data provided by user in delete user process. " +
                        "Thus process is canceled, no user is deleted.");
            errorResult.errorMessage = "SSP-0";
            return Ok(errorResult);
        }

        email = email.ToLower().Trim();

        if (email == "")
        {
            _logger.LogWarning("No user data provided by user in delete user process. " +
                        "Thus process is canceled, no user is deleted.");
            errorResult.errorMessage = "SSP-0";
            return Ok(errorResult);
        }

        User userInDb = new User();
        ActionResult<User[]> usersInDbResult = this.GetUsers(email, "", "");
        User[]? usersInDb = usersInDbResult.Value;
        if (usersInDb == null || usersInDb.Length != 1 || usersInDb[0] == null)
        {
            _logger.LogWarning("User " + email + " cannot be deleted since this user is not in the database.");
            errorResult.errorMessage = "SSP-1";
            return Ok(errorResult);
        }
        else
        {
            userInDb = usersInDb[0];
            if (userInDb.role.code == "administrator")
            {
                if (_countNumberOfActiveAdmins() == 1)
                {
                    _logger.LogWarning("User tried to deactivate last administrator. Last administrator cannot be deactivated.");
                    errorResult.errorMessage = "SSP-2";
                    return Ok(errorResult);
                }
            }
            using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
            {
                pgConn.Open();
                NpgsqlCommand deleteComm = pgConn.CreateCommand();
                deleteComm.CommandText = @"UPDATE ""wtb_ssp_users""
                                SET active=false
                                WHERE e_mail=@e_mail";
                deleteComm.Parameters.AddWithValue("e_mail", email);

                int noAffectedRows = deleteComm.ExecuteNonQuery();

                pgConn.Close();

                if (noAffectedRows == 1)
                {
                    return Ok();
                }
            }
        }

        _logger.LogError("Fatal error.");
        errorResult.errorMessage = "SSP-3";
        return Ok(errorResult);
    }

    private static int _countNumberOfActiveAdmins()
    {
        int count = 0;
        using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
        {
            pgConn.Open();
            NpgsqlCommand selectComm = pgConn.CreateCommand();
            selectComm.CommandText = @"SELECT count(*) 
                            FROM ""wtb_ssp_users"" users
                            LEFT JOIN ""wtb_ssp_roles"" roles ON users.role = roles.code
                            WHERE users.active=true AND roles.code='administrator'";

            using (NpgsqlDataReader reader = selectComm.ExecuteReader())
            {
                while (reader.Read())
                {
                    count = reader.IsDBNull(0) ? 0 :
                                reader.GetInt32(0);
                }
            }
            pgConn.Close();
        }
        return count;
    }

    private static bool _isAreaManagerAssigned(string areaManagerUuid)
    {
        bool result = false;
        using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
        {
            pgConn.Open();
            NpgsqlCommand selectComm = pgConn.CreateCommand();
            selectComm.CommandText = @"SELECT count(*) 
                            FROM ""wtb_ssp_managementareas""
                            WHERE manager=@uuid OR substitute_manager=@uuid";
            selectComm.Parameters.AddWithValue("uuid", new Guid(areaManagerUuid));

            using (NpgsqlDataReader reader = selectComm.ExecuteReader())
            {
                while (reader.Read())
                {
                    int count = reader.IsDBNull(0) ? 0 :
                                    reader.GetInt32(0);

                    if (count != 0)
                    {
                        result = true;
                    }
                }
            }
            pgConn.Close();
        }
        return result;
    }

}

