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
    public ActionResult<OrganisationalUnit[]> GetOrganisations(bool withContactPerson = false)
    {
        List<OrganisationalUnit> orgsFromDb = new List<OrganisationalUnit>();
        using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
        {
            pgConn.Open();
            NpgsqlCommand selectComm = pgConn.CreateCommand();
            selectComm.CommandText = @"SELECT uuid, name, abbreviation, is_civil_eng FROM ""wtb_ssp_organisationalunits""";
            if(withContactPerson){
                selectComm.CommandText = @"SELECT o.uuid, o.name, o.abbreviation, o.is_civil_eng,
                                u.first_name, u.last_name
                            FROM ""wtb_ssp_organisationalunits"" o
                            LEFT JOIN (
                                SELECT DISTINCT ON (org_unit) org_unit, first_name, last_name
                                FROM ""wtb_ssp_users""
                                GROUP BY org_unit, last_name, first_name
                                )
                            u ON u.org_unit = o.uuid";

            }
            using (NpgsqlDataReader reader = selectComm.ExecuteReader())
            {
                OrganisationalUnit orgFromDb;
                while (reader.Read())
                {
                    orgFromDb = new OrganisationalUnit
                    {
                        uuid = reader.IsDBNull(0) ? "" :
                                reader.GetGuid(0).ToString(),
                        name =
                            reader.IsDBNull(1) ? "" :
                                    reader.GetString(1),
                        abbreviation =
                            reader.IsDBNull(2) ? "" :
                                    reader.GetString(2),
                        isCivilEngineering =
                            reader.IsDBNull(3) ? false :
                                    reader.GetBoolean(3)
                    };
                    if (withContactPerson){
                        orgFromDb.contactPerson =
                            reader.IsDBNull(4) ? "" :
                                    "" + reader.GetString(4);

                        orgFromDb.contactPerson +=
                            reader.IsDBNull(5) ? "" :
                                    " " + reader.GetString(5);
                    }
                    orgsFromDb.Add(orgFromDb);
                }
            }
            pgConn.Close();
        }

        return orgsFromDb.ToArray();
    }

    // POST /account/organisations/
    [HttpPost]
    [Authorize(Roles = "administrator")]
    public ActionResult<OrganisationalUnit> AddOrganisation([FromBody] OrganisationalUnit org)
    {
        try
        {
            if (org == null || org.name == null)
            {
                _logger.LogWarning("No organisation data provided in add organisation process.");
                OrganisationalUnit resultOrg = new OrganisationalUnit();
                resultOrg.errorMessage = "SSP-21";
                return Ok(resultOrg);
            }

            org.name = org.name.Trim();

            if (org.name == "")
            {
                _logger.LogWarning("No organisation data provided in add organisation process.");
                org.errorMessage = "SSP-21";
                return Ok(org);
            }

            using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
            {
                pgConn.Open();
                NpgsqlCommand insertComm = pgConn.CreateCommand();
                insertComm.CommandText = @"INSERT INTO ""wtb_ssp_organisationalunits""
                                        (uuid, name, abbreviation, is_civil_eng)
                                        VALUES(@uuid, @name, @abbreviation, @is_civil_eng)";
                Guid orgUuid = Guid.NewGuid();
                org.uuid = orgUuid.ToString();
                insertComm.Parameters.AddWithValue("uuid", new Guid(org.uuid));
                insertComm.Parameters.AddWithValue("name", org.name);
                insertComm.Parameters.AddWithValue("abbreviation", org.abbreviation);
                insertComm.Parameters.AddWithValue("is_civil_eng", org.isCivilEngineering);
                int noAffectedRows = insertComm.ExecuteNonQuery();

                pgConn.Close();

                return Ok(org);
            }

        }
        catch (Exception ex)
        {
            _logger.LogError("Something went wrong.");
            org.errorMessage = "SSP-3";
            return Ok(org);
        }
    }

    // PUT /account/organisations/
    [HttpPut]
    [Authorize(Roles = "administrator")]
    public ActionResult<ErrorMessage> UpdateOrganisation([FromBody] OrganisationalUnit org)
    {
        ErrorMessage errorResult = new ErrorMessage();
        if (org == null || org.uuid == null || org.name == null)
        {
            _logger.LogInformation("No organisation data provided by user in update organisation process.");
            errorResult.errorMessage = "SSP-21";
            return Ok(errorResult);
        }

        org.uuid = org.uuid.Trim();
        org.name = org.name.Trim();
        if (org.uuid == "" || org.name == "")
        {
            _logger.LogWarning("No organisation data provided by user in update organisation process.");
            errorResult.errorMessage = "SSP-21";
            return Ok(errorResult);
        }

        using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
        {
            pgConn.Open();
            NpgsqlCommand updateComm = pgConn.CreateCommand();
            updateComm.CommandText = @"UPDATE ""wtb_ssp_organisationalunits"" SET
                                        name=@name, abbreviation=@abbreviation, is_civil_eng=@is_civil_eng WHERE uuid=@uuid";
            updateComm.Parameters.AddWithValue("name", org.name);
            updateComm.Parameters.AddWithValue("abbreviation", org.abbreviation);
            updateComm.Parameters.AddWithValue("is_civil_eng", org.isCivilEngineering);
            updateComm.Parameters.AddWithValue("uuid", new Guid(org.uuid));
            int noAffectedRowsStep1 = updateComm.ExecuteNonQuery();

            pgConn.Close();
        }
        return Ok(org);
    }

}

