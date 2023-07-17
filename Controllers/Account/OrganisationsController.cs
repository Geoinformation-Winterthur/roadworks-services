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
public class OrganisationsController : ControllerBase
{
    private readonly ILogger<OrganisationsController> _logger;

    public OrganisationsController(ILogger<OrganisationsController> logger)
    {
        _logger = logger;
    }


    // GET /account/organisations/
    [HttpGet]
    [Authorize]
    public ActionResult<OrganisationalUnit[]> GetOrganisations()
    {
        List<OrganisationalUnit> orgsFromDb = new List<OrganisationalUnit>();
        using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
        {
            pgConn.Open();
            NpgsqlCommand selectComm = pgConn.CreateCommand();
            selectComm.CommandText = @"SELECT uuid, name FROM ""wtb_ssp_organisationalunits""";
            using (NpgsqlDataReader reader = selectComm.ExecuteReader())
            {
                OrganisationalUnit orgFromDb;
                while (reader.Read())
                {
                    orgFromDb = new OrganisationalUnit();
                    orgFromDb.uuid = reader.IsDBNull(0) ? "" :
                                reader.GetGuid(0).ToString();
                    orgFromDb.name =
                            reader.IsDBNull(1) ? "" :
                                    reader.GetString(1);
                    orgsFromDb.Add(orgFromDb);
                }
            }
            pgConn.Close();
        }

        return orgsFromDb.ToArray();
    }

}

