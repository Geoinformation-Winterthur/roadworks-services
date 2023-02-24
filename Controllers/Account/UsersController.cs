// <copyright company="Vermessungsamt Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Vermessungsamt Winterthur. All rights reserved.
// </copyright>
// <copyright company="Vermessungsamt Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Vermessungsamt Winterthur. All rights reserved.
// </copyright>
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using roadwork_portal_service.Model;
using roadwork_portal_service.Configuration;
using System.Numerics;
using System.Globalization;

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
    [HttpGet]
    [Authorize(Roles = "administrator")]
    public ActionResult<User[]> GetUsers(string email)
    {
        List<User> usersFromDb = new List<User>();
        // get data of current user from database:
        using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
        {
            pgConn.Open();
            NpgsqlCommand selectComm = pgConn.CreateCommand();
            selectComm.CommandText = @"SELECT uuid, last_name, first_name,
                        trim(lower(e_mail)), role FROM ""users""";
            if (email != null)
            {
                email = email.ToLower().Trim();
                if (email != "")
                {
                    selectComm.CommandText += " WHERE trim(lower(e_mail))=@email";
                    selectComm.Parameters.AddWithValue("email", email);
                }
            }

            using (NpgsqlDataReader reader = selectComm.ExecuteReader())
            {
                User userFromDb;
                while (reader.Read())
                {
                    userFromDb = new User();
                    userFromDb.mailAddress = reader.GetString(3);
                    userFromDb.passPhrase = reader.GetString(4);

                    userFromDb.lastName = reader.GetString(1);
                    userFromDb.firstName = reader.GetString(2);
                    userFromDb.role = reader.GetString(4);

                    if (userFromDb.lastName == null || userFromDb.lastName.Trim().Equals(""))
                    {
                        userFromDb.lastName = "Nachname unbekannt";
                    }

                    if (userFromDb.firstName == null || userFromDb.firstName.Trim().Equals(""))
                    {
                        userFromDb.firstName = "Vorname unbekannt";
                    }

                    usersFromDb.Add(userFromDb);
                }
            }
            pgConn.Close();
        }

        return usersFromDb.ToArray<User>();
    }

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
        ActionResult<User[]> usersInDbResult = this.GetUsers(user.mailAddress);
        User[]? usersInDb = usersInDbResult.Value;
        if(usersInDb != null && usersInDb.Length > 0)
        {
            userInDb = usersInDb[0];
        }

        if (userInDb.mailAddress == "")
        {
            return BadRequest("Adding user not possible");
        }

        using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
        {
            pgConn.Open();
            NpgsqlCommand insertComm = pgConn.CreateCommand();
            insertComm.CommandText = @"INSERT INTO ""users""(uuid,
                    last_name, first_name, e_mail, role, pwd
                    VALUES(@uuid, @last_name, @first_name, @e_mail, @role, @pwd)";

            string newUserGuid = Guid.NewGuid().ToString();
            newUserGuid = $"0{newUserGuid:N}"; // add leading zero, no dashes
            user.uuid = BigInteger.Parse(newUserGuid, NumberStyles.HexNumber);

            insertComm.Parameters.AddWithValue("uuid", user.uuid);
            insertComm.Parameters.AddWithValue("last_name", user.lastName);
            insertComm.Parameters.AddWithValue("first_name", user.firstName);
            insertComm.Parameters.AddWithValue("e_mail", user.mailAddress);
            insertComm.Parameters.AddWithValue("role", user.role);
            insertComm.Parameters.AddWithValue("pwd", user.passPhrase);

            int noAffectedRows = insertComm.ExecuteNonQuery();

            pgConn.Close();

            if(noAffectedRows == 1)
            {
                user.passPhrase = "";
                return Ok(user);
            }

        }

        return BadRequest("Something went wrong");
    }


    [HttpPut]
    [Authorize(Roles = "administrator")]
    public IActionResult UpdateUser([FromBody] User user)
    {
        // TODO implement
        return Ok();
    }

}

