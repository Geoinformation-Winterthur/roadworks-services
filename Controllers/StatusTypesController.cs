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
[Route("[controller]")]
public class StatusTypesController : ControllerBase
{
    private readonly ILogger<StatusTypesController> _logger;

    public StatusTypesController(ILogger<StatusTypesController> logger)
    {
        _logger = logger;
    }


    // GET statustypes/
    [HttpGet]
    [Authorize]
    public ActionResult<Status[]> GetStatusTypes()
    {
        User userFromDb = LoginController.getAuthorizedUserFromDb(this.User);
        List<Status> statusTypesFromDb = new List<Status>();
        using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
        {
            pgConn.Open();
            NpgsqlCommand selectComm = pgConn.CreateCommand();
            selectComm.CommandText = @"SELECT st.code, st.name
                            FROM ""status"" st";

            using (NpgsqlDataReader reader = selectComm.ExecuteReader())
            {
                Status statusTypeFromDb;
                while (reader.Read())
                {
                    statusTypeFromDb = new Status();
                    statusTypeFromDb.code =
                            reader.IsDBNull(0) ? "" :
                                    reader.GetString(0);
                    statusTypeFromDb.name =
                            reader.IsDBNull(1) ? "" :
                                    reader.GetString(1);
                    statusTypesFromDb.Add(statusTypeFromDb);
                }
            }
            pgConn.Close();
        }

        return statusTypesFromDb.ToArray();
    }

}

