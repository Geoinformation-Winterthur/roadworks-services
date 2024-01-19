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
                        min_area_size=@min_area_size, max_area_size=@max_area_size,
                        date_sks1=@date_sks1, date_sks2=@date_sks2,
                        date_sks3=@date_sks3, date_sks4=@date_sks4,
                        date_kap1=@date_kap1, date_kap2=@date_kap2,
                        date_oks1=@date_oks1, date_oks2=@date_oks2,
                        date_oks3=@date_oks3, date_oks4=@date_oks4,
                        date_oks5=@date_oks5, date_oks6=@date_oks6,
                        date_oks7=@date_oks7, date_oks8=@date_oks8,
                        date_oks9=@date_oks9, date_oks10=@date_oks10,
                        date_oks11=@date_oks11";
                updateComm.Parameters.AddWithValue("min_area_size", configData.minAreaSize);
                updateComm.Parameters.AddWithValue("max_area_size", configData.maxAreaSize);
                if(configData.dateSks1 != null) updateComm.Parameters.AddWithValue("date_sks1", configData.dateSks1);
                else updateComm.Parameters.AddWithValue("date_sks1", DBNull.Value);
                if(configData.dateSks2 != null) updateComm.Parameters.AddWithValue("date_sks2", configData.dateSks2);
                else updateComm.Parameters.AddWithValue("date_sks2", DBNull.Value);
                if(configData.dateSks3 != null) updateComm.Parameters.AddWithValue("date_sks3", configData.dateSks3);
                else updateComm.Parameters.AddWithValue("date_sks3", DBNull.Value);
                if(configData.dateSks4 != null) updateComm.Parameters.AddWithValue("date_sks4", configData.dateSks4);
                else updateComm.Parameters.AddWithValue("date_sks4", DBNull.Value);

                if(configData.dateKap1 != null) updateComm.Parameters.AddWithValue("date_kap1", configData.dateKap1);
                else updateComm.Parameters.AddWithValue("date_kap1", DBNull.Value);
                if(configData.dateKap2 != null) updateComm.Parameters.AddWithValue("date_kap2", configData.dateKap2);
                else updateComm.Parameters.AddWithValue("date_kap2", DBNull.Value);

                if(configData.dateOks1 != null) updateComm.Parameters.AddWithValue("date_oks1", configData.dateOks1);
                else updateComm.Parameters.AddWithValue("date_oks1", DBNull.Value);
                if(configData.dateOks2 != null) updateComm.Parameters.AddWithValue("date_oks2", configData.dateOks2);
                else updateComm.Parameters.AddWithValue("date_oks2", DBNull.Value);
                if(configData.dateOks3 != null) updateComm.Parameters.AddWithValue("date_oks3", configData.dateOks3);
                else updateComm.Parameters.AddWithValue("date_oks3", DBNull.Value);
                if(configData.dateOks4 != null) updateComm.Parameters.AddWithValue("date_oks4", configData.dateOks4);
                else updateComm.Parameters.AddWithValue("date_oks4", DBNull.Value);
                if(configData.dateOks5 != null) updateComm.Parameters.AddWithValue("date_oks5", configData.dateOks5);
                else updateComm.Parameters.AddWithValue("date_oks5", DBNull.Value);
                if(configData.dateOks6 != null) updateComm.Parameters.AddWithValue("date_oks6", configData.dateOks6);
                else updateComm.Parameters.AddWithValue("date_oks6", DBNull.Value);
                if(configData.dateOks7 != null) updateComm.Parameters.AddWithValue("date_oks7", configData.dateOks7);
                else updateComm.Parameters.AddWithValue("date_oks7", DBNull.Value);
                if(configData.dateOks8 != null) updateComm.Parameters.AddWithValue("date_oks8", configData.dateOks8);
                else updateComm.Parameters.AddWithValue("date_oks8", DBNull.Value);
                if(configData.dateOks9 != null) updateComm.Parameters.AddWithValue("date_oks9", configData.dateOks9);
                else updateComm.Parameters.AddWithValue("date_oks9", DBNull.Value);
                if(configData.dateOks10 != null) updateComm.Parameters.AddWithValue("date_oks10", configData.dateOks10);
                else updateComm.Parameters.AddWithValue("date_oks10", DBNull.Value);
                if(configData.dateOks11 != null) updateComm.Parameters.AddWithValue("date_oks11", configData.dateOks11);
                else updateComm.Parameters.AddWithValue("date_oks11", DBNull.Value);

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
            selectComm.CommandText = @"SELECT min_area_size, max_area_size,
                            date_sks1, date_sks2, date_sks3, date_sks4,
                            date_kap1, date_kap2, date_oks1, date_oks2,
                            date_oks3, date_oks4, date_oks5, date_oks6,
                            date_oks7, date_oks8, date_oks9, date_oks10,
                            date_oks11
                            FROM ""wtb_ssp_configuration""";

            using (NpgsqlDataReader reader = selectComm.ExecuteReader())
            {
                if (reader.Read())
                {
                    result.minAreaSize = reader.IsDBNull(0) ? 0 :
                                reader.GetInt32(0);
                    result.maxAreaSize = reader.IsDBNull(1) ? 0 :
                                reader.GetInt32(1);
                    result.dateSks1 = reader.IsDBNull(2) ? null :
                                reader.GetDateTime(2);
                    result.dateSks2 = reader.IsDBNull(3) ? null :
                                reader.GetDateTime(3);
                    result.dateSks3 = reader.IsDBNull(4) ? null :
                                reader.GetDateTime(4);
                    result.dateSks4 = reader.IsDBNull(5) ? null :
                                reader.GetDateTime(5);
                    result.dateKap1 = reader.IsDBNull(6) ? null :
                                reader.GetDateTime(6);
                    result.dateKap2 = reader.IsDBNull(7) ? null :
                                reader.GetDateTime(7);
                    result.dateOks1 = reader.IsDBNull(8) ? null :
                                reader.GetDateTime(8);
                    result.dateOks2 = reader.IsDBNull(9) ? null :
                                reader.GetDateTime(9);
                    result.dateOks3 = reader.IsDBNull(10) ? null :
                                reader.GetDateTime(10);
                    result.dateOks4 = reader.IsDBNull(11) ? null :
                                reader.GetDateTime(11);
                    result.dateOks5 = reader.IsDBNull(12) ? null :
                                reader.GetDateTime(12);
                    result.dateOks6 = reader.IsDBNull(13) ? null :
                                reader.GetDateTime(13);
                    result.dateOks7 = reader.IsDBNull(14) ? null :
                                reader.GetDateTime(14);
                    result.dateOks8 = reader.IsDBNull(15) ? null :
                                reader.GetDateTime(15);
                    result.dateOks9 = reader.IsDBNull(16) ? null :
                                reader.GetDateTime(16);
                    result.dateOks10 = reader.IsDBNull(17) ? null :
                                reader.GetDateTime(17);
                    result.dateOks11 = reader.IsDBNull(18) ? null :
                                reader.GetDateTime(18);
                }
            }
            pgConn.Close();
        }
        return result;
    }

}

