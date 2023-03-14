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
public class UserRolesController : ControllerBase
{
    private readonly ILogger<UserRolesController> _logger;

    public UserRolesController(ILogger<UserRolesController> logger)
    {
        _logger = logger;
    }


    // GET /account/userroles/
    [HttpGet]
    [Authorize]
    public ActionResult<Role[]> GetUserRoles()
    {
        List<Role> rolesFromDb = new List<Role>();
        using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
        {
            pgConn.Open();
            NpgsqlCommand selectComm = pgConn.CreateCommand();
            selectComm.CommandText = @"SELECT uuid, code, name FROM ""roles""";
            using (NpgsqlDataReader reader = selectComm.ExecuteReader())
            {
                Role roleFromDb;
                while (reader.Read())
                {
                    roleFromDb = new Role();
                    roleFromDb.uuid = reader.IsDBNull(0) ? "" :
                                reader.GetGuid(0).ToString();
                    roleFromDb.code =
                            reader.IsDBNull(1) ? "" :
                                    reader.GetString(1);
                    roleFromDb.name =
                            reader.IsDBNull(2) ? "" :
                                    reader.GetString(2);
                    rolesFromDb.Add(roleFromDb);
                }
            }
            pgConn.Close();
        }

        return rolesFromDb.ToArray();
    }

}

