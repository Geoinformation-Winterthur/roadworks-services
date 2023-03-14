// <copyright company="Vermessungsamt Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Vermessungsamt Winterthur. All rights reserved.
// </copyright>
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using roadwork_portal_service.Model;
using roadwork_portal_service.Configuration;

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
    [Authorize(Roles = "administrator")]
    public ActionResult<User[]> GetUsers(string? email, string? uuid)
    {
        List<User> usersFromDb = new List<User>();
        // get data of current user from database:
        using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
        {
            pgConn.Open();
            NpgsqlCommand selectComm = pgConn.CreateCommand();
            selectComm.CommandText = @"SELECT u.uuid, u.last_name, u.first_name,
                                trim(lower(u.e_mail)), u.role, roles.code,
                                roles.name, u.org_unit, o.name
                            FROM ""users"" u
                            LEFT JOIN ""roles"" ON u.role = roles.uuid
                            LEFT JOIN ""organisationalunits"" o ON u.org_unit = o.uuid";

            if (email != null)
            {
                email = email.ToLower().Trim();
            }

            if (email != null && email != "")
            {
                selectComm.CommandText += " WHERE trim(lower(u.e_mail))=@email";
                selectComm.Parameters.AddWithValue("email", email);
            }
            else
            {
                if (uuid != null)
                {
                    uuid = uuid.Trim().ToLower();
                    if (uuid != "")
                    {
                        selectComm.CommandText += " WHERE u.uuid=@uuid";
                        selectComm.Parameters.AddWithValue("uuid", new Guid(uuid));
                    }
                }
            }

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
                        Role role = new Role();
                        role.uuid = reader.GetGuid(4).ToString();
                        role.code = reader.GetString(5);
                        role.name = reader.GetString(6);
                        userFromDb.role = role;
                        OrganisationalUnit orgUnit = new OrganisationalUnit();
                        orgUnit.uuid = reader.GetGuid(7).ToString();
                        orgUnit.name = reader.GetString(8);
                        userFromDb.organisationalUnit = orgUnit;

                        if (userFromDb.lastName == null || userFromDb.lastName.Trim().Equals(""))
                        {
                            userFromDb.lastName = "unbekannt";
                        }

                        if (userFromDb.firstName == null || userFromDb.firstName.Trim().Equals(""))
                        {
                            userFromDb.firstName = "unbekannt";
                        }

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
        if (user == null || user.mailAddress == null)
        {
            return BadRequest("No user data provided");
        }

        user.mailAddress = user.mailAddress.ToLower().Trim();

        if (user.mailAddress == "")
        {
            return BadRequest("No user data provided");
        }

        User userInDb = new User();
        ActionResult<User[]> usersInDbResult = this.GetUsers(user.mailAddress, "");
        User[]? usersInDb = usersInDbResult.Value;
        if (usersInDb != null && usersInDb.Length > 0)
        {
            userInDb = usersInDb[0];
        }

        if (userInDb != null && userInDb.mailAddress != null &&
                    userInDb.mailAddress.Trim() == user.mailAddress)
        {
            return BadRequest("Adding user not possible");
        }

        using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
        {
            pgConn.Open();
            NpgsqlCommand insertComm = pgConn.CreateCommand();
            insertComm.CommandText = @"INSERT INTO ""users""(uuid,
                    last_name, first_name, e_mail, role, pwd, org_unit)
                    VALUES(@uuid, @last_name, @first_name, @e_mail, @role, @pwd, @org_unit)";
            Guid userUuid = Guid.NewGuid();
            user.uuid = userUuid.ToString();
            insertComm.Parameters.AddWithValue("uuid", new Guid(user.uuid));
            insertComm.Parameters.AddWithValue("last_name", user.lastName);
            insertComm.Parameters.AddWithValue("first_name", user.firstName);
            insertComm.Parameters.AddWithValue("e_mail", user.mailAddress);
            insertComm.Parameters.AddWithValue("role", new Guid(user.role.uuid));
            insertComm.Parameters.AddWithValue("pwd", user.passPhrase);
            insertComm.Parameters.AddWithValue("org_unit", new Guid(user.organisationalUnit.uuid));

            int noAffectedRows = insertComm.ExecuteNonQuery();

            pgConn.Close();

            if (noAffectedRows == 1)
            {
                user.passPhrase = "";
                return Ok(user);
            }

        }

        return BadRequest("Something went wrong");
    }

    // PUT /account/users/
    [HttpPut]
    [Authorize(Roles = "administrator")]
    public IActionResult UpdateUser([FromBody] User user)
    {
        if (user == null || user.uuid == null)
        {
            return BadRequest("No user data provided");
        }

        user.uuid = user.uuid.ToLower().Trim();

        if (user.uuid == "")
        {
            return BadRequest("No user data provided");
        }

        User userInDb = new User();
        ActionResult<User[]> usersInDbResult = this.GetUsers("", user.uuid);
        User[]? usersInDb = usersInDbResult.Value;
        if (usersInDb == null || usersInDb.Length != 1)
        {
            return BadRequest("Editing user not possible");
        }
        else
        {
            userInDb = usersInDb[0];
            if (userInDb != null)
            {
                if(userInDb.role.code == "administrator" && user.role.code != "administrator")
                {
                    if(_countNumberOfAdmins() == 1)
                    {
                        return BadRequest("Last administrator cannot be removed");
                    }
                }
                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {
                    pgConn.Open();
                    NpgsqlCommand updateComm = pgConn.CreateCommand();
                    updateComm.CommandText = @"UPDATE ""users"" SET
                        last_name=@last_name, first_name=@first_name, e_mail=@e_mail,
                        role=@role, org_unit=@org_unit WHERE uuid=@uuid";
                    updateComm.Parameters.AddWithValue("last_name", user.lastName);
                    updateComm.Parameters.AddWithValue("first_name", user.firstName);
                    updateComm.Parameters.AddWithValue("e_mail", user.mailAddress);
                    updateComm.Parameters.AddWithValue("role", new Guid(user.role.uuid));
                    updateComm.Parameters.AddWithValue("org_unit", new Guid(user.organisationalUnit.uuid));
                    updateComm.Parameters.AddWithValue("uuid", new Guid(user.uuid));
                    int noAffectedRowsStep1 = updateComm.ExecuteNonQuery();

                    user.passPhrase = user.passPhrase.Trim();
                    bool needToUpdatePassphrase = user.passPhrase.Length != 0;
                    int noAffectedRowsStep2 = 0;
                    if (needToUpdatePassphrase)
                    {
                        updateComm.CommandText = @"UPDATE ""users"" SET
                                    pwd=@pwd WHERE uuid=@uuid";
                        updateComm.Parameters.AddWithValue("pwd", user.passPhrase);
                        updateComm.Parameters.AddWithValue("uuid", new Guid(user.uuid));
                        noAffectedRowsStep2 = updateComm.ExecuteNonQuery();
                    }

                    pgConn.Close();

                    if (noAffectedRowsStep1 == 1 && 
                        (!needToUpdatePassphrase || noAffectedRowsStep2 == 1))
                    {
                        user.passPhrase = "";
                        return Ok(user);
                    }

                }

            }
            else
            {
                return BadRequest("Editing user not possible");
            }
        }

        return BadRequest("Something went wrong");
    }

    // DELETE /users?email=...
    [HttpDelete]
    [Authorize(Roles = "administrator")]
    public ActionResult DeleteUser(string email)
    {
        if (email == null)
        {
            return BadRequest("No user data provided");
        }

        email = email.ToLower().Trim();

        if (email == "")
        {
            return BadRequest("No user data provided");
        }

        User userInDb = new User();
        ActionResult<User[]> usersInDbResult = this.GetUsers(email, "");
        User[]? usersInDb = usersInDbResult.Value;
        if (usersInDb == null || usersInDb.Length != 1)
        {
            return BadRequest("Deleting user not possible");
        }
        else
        {
            userInDb = usersInDb[0];
            if (userInDb != null)
            {
                if(userInDb.role.code == "administrator")
                {
                    if(_countNumberOfAdmins() == 1)
                    {
                        return BadRequest("Last administrator cannot be removed");
                    }
                }
                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {
                    pgConn.Open();
                    NpgsqlCommand deleteComm = pgConn.CreateCommand();
                    deleteComm.CommandText = @"DELETE FROM ""users""
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
            else
            {
                return BadRequest("Deleting user not possible");
            }
        }

        return BadRequest("Something went wrong");
    }

    private static int _countNumberOfAdmins(){
        int count = 0;
        using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
        {
            pgConn.Open();
            NpgsqlCommand selectComm = pgConn.CreateCommand();
            selectComm.CommandText = @"SELECT count(*) 
                            FROM ""users""
                            LEFT JOIN ""roles"" ON users.role = roles.uuid
                            WHERE roles.code='administrator'";

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

}

