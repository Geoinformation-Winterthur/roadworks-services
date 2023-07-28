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
public class AppConfigController : ControllerBase
{
    private readonly ILogger<AppConfigController> _logger;

    public AppConfigController(ILogger<AppConfigController> logger)
    {
        _logger = logger;
    }


    // GET /appconfig/
    [HttpGet]
    [Authorize(Roles = "administrator")]
    public ActionResult<ConfigurationData> GetConfiguration()
    {
        ConfigurationData result = new ConfigurationData();

        try
        {
            result = AppConfigController.getConfigurationFromDb();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            result = new ConfigurationData();
            result.errorMessage = "SSP-3";
            return Ok(result);
        }
        return Ok(result);
    }

    // PUT /appconfig/
    [HttpPut]
    [Authorize(Roles = "administrator")]
    public ActionResult<ConfigurationData> UpdateConfiguration([FromBody] ConfigurationData configData)
    {
        if (configData == null)
        {
            ConfigurationData errorResult = new ConfigurationData();
            _logger.LogInformation("No configuration data provided by user in update configuration process.");
            errorResult.errorMessage = "SSP-0";
            return Ok(errorResult);
        }

        try
        {
            using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
            {
                pgConn.Open();
                NpgsqlCommand updateComm = pgConn.CreateCommand();
                updateComm.CommandText = @"UPDATE ""wtb_ssp_configuration"" SET
                        min_area_size=@min_area_size, max_area_size=@max_area_size";
                updateComm.Parameters.AddWithValue("min_area_size", configData.minAreaSize);
                updateComm.Parameters.AddWithValue("max_area_size", configData.maxAreaSize);
                int noAffectedRowsStep1 = updateComm.ExecuteNonQuery();

                pgConn.Close();
                return Ok(configData);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            ConfigurationData errorResult = new ConfigurationData();
            errorResult.errorMessage = "SSP-3";
            return Ok(errorResult);
        }

    }

    public static ConfigurationData getConfigurationFromDb()
    {
        ConfigurationData result = new ConfigurationData();
        // get configuration data from database:
        using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
        {
            pgConn.Open();
            NpgsqlCommand selectComm = pgConn.CreateCommand();
            selectComm.CommandText = @"SELECT min_area_size, max_area_size
                            FROM ""wtb_ssp_configuration""";

            using (NpgsqlDataReader reader = selectComm.ExecuteReader())
            {
                if (reader.Read())
                {
                    result.minAreaSize = reader.IsDBNull(0) ? 0 :
                                reader.GetInt32(0);
                    result.maxAreaSize = reader.IsDBNull(1) ? 0 :
                                reader.GetInt32(1);
                }
            }
            pgConn.Close();
        }
        return result;
    }

}

