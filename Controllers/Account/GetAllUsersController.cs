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

namespace roadwork_portal_service.Controllers;

[ApiController]
[Route("Account/[controller]")]
public class GetAllUsersController : ControllerBase
{
    private readonly ILogger<HomeController> _logger;

    public GetAllUsersController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }


    // GET /account/getallusers
    [HttpGet]
    [Authorize(Roles = "administrator")]
    public ActionResult<User[]> Get()
    {
        List<User> usersFromDb = new List<User>();
        // get data of current user from database:
        using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
        {
            pgConn.Open();
            NpgsqlCommand selectComm = pgConn.CreateCommand();
            selectComm.CommandText = @"SELECT uuid, last_name, first_name, e_mail, 
                        role FROM ""users""";

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

}

