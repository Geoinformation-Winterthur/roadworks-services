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


    // GET /appconfig/?pastdates=false
    [HttpGet]
    [Authorize(Roles = "administrator, territorymanager")]
    public ActionResult<ConfigurationData> GetConfiguration(bool? pastDates = false)
    {
        ConfigurationData result;

        try
        {
            bool pastDatesLocal = false;
            if(pastDates != null)
                pastDatesLocal = (bool)pastDates;
            result = AppConfigController.getConfigurationFromDb(pastDatesLocal);
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
    public ActionResult<ConfigurationData> UpdateConfiguration([FromBody] ConfigurationData? configData)
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
                updateComm.ExecuteNonQuery();

                using NpgsqlTransaction trans = pgConn.BeginTransaction();

                NpgsqlCommand deleteDatesComm = pgConn.CreateCommand();
                deleteDatesComm.CommandText = @"DELETE FROM ""wtb_ssp_config_dates"" WHERE
                        planneddate::timestamp >= CURRENT_DATE";
                deleteDatesComm.ExecuteNonQuery();

                DateTime today = DateTime.Today;
                DateTime todayDateOnly = new DateTime(today.Year, today.Month, today.Day, 0,0,0);

                foreach (DateTime? plannedDateSks in configData.plannedDatesSks)
                {
                    if (plannedDateSks != null &&  plannedDateSks >= todayDateOnly)
                    {
                        NpgsqlCommand insertDatesComm = pgConn.CreateCommand();
                        insertDatesComm.CommandText = @"INSERT INTO ""wtb_ssp_config_dates""
                        (date_type, planneddate, sks_no)
                        VALUES (@date_type, @planneddate, COALESCE(MAX(sks_no),0) +1)";
                        insertDatesComm.Parameters.AddWithValue("date_type", "SKS");
                        insertDatesComm.Parameters.AddWithValue("planneddate", plannedDateSks);
                        insertDatesComm.ExecuteNonQuery();
                    }
                }

                foreach (DateTime? plannedDateKap in configData.plannedDatesKap)
                {
                    if (plannedDateKap != null && plannedDateKap >= todayDateOnly)
                    {
                        NpgsqlCommand insertDatesComm = pgConn.CreateCommand();
                        insertDatesComm.CommandText = @"INSERT INTO ""wtb_ssp_config_dates""
                        (date_type, planneddate)
                        VALUES (@date_type, @planneddate)";
                        insertDatesComm.Parameters.AddWithValue("date_type", "KAP");
                        insertDatesComm.Parameters.AddWithValue("planneddate", plannedDateKap);
                        insertDatesComm.ExecuteNonQuery();
                    }
                }

                foreach (DateTime? plannedDateOks in configData.plannedDatesOks)
                {
                    if (plannedDateOks != null && plannedDateOks >= todayDateOnly)
                    {
                        NpgsqlCommand insertDatesComm = pgConn.CreateCommand();
                        insertDatesComm.CommandText = @"INSERT INTO ""wtb_ssp_config_dates""
                        (date_type, planneddate)
                        VALUES (@date_type, @planneddate)";
                        insertDatesComm.Parameters.AddWithValue("date_type", "OKS");
                        insertDatesComm.Parameters.AddWithValue("planneddate", plannedDateOks);
                        insertDatesComm.ExecuteNonQuery();
                    }
                }

                trans.Commit();
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

    public static ConfigurationData getConfigurationFromDb(bool pastDates)
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

            if (pastDates)
            {
                selectComm.CommandText = @"SELECT planneddate, sks_no
                            FROM ""wtb_ssp_config_dates""
                            WHERE date_type='SKS' AND planneddate < current_timestamp
                            ORDER BY planneddate DESC
                            LIMIT 5";
                List<long?> sksNos = new List<long?>();

                using (NpgsqlDataReader reader = selectComm.ExecuteReader())
                {
                    List<DateTime?> plannedDatesSks = new List<DateTime?>();
                    while (reader.Read())
                    {
                        DateTime? dateSks = reader.IsDBNull(0) ? null : reader.GetDateTime(0);

                        if (dateSks != null)
                            plannedDatesSks.Add((DateTime)dateSks);

                        long? sksNo = reader.IsDBNull(1) ? (long?)null : reader.GetInt64(1);
                        if (sksNo != null)
                            sksNos.Add((long)sksNo);
                    }
                    result.plannedDatesSks = plannedDatesSks.ToArray();
                    result.sksNos = sksNos.ToArray();
                }
                selectComm.CommandText = @"SELECT planneddate
                            FROM ""wtb_ssp_config_dates""
                            WHERE date_type='KAP' AND planneddate < current_timestamp
                            ORDER BY planneddate DESC
                            LIMIT 5";
                using (NpgsqlDataReader reader = selectComm.ExecuteReader())
                {
                    List<DateTime?> plannedDatesKap = new List<DateTime?>();
                    while (reader.Read())
                    {
                        DateTime? dateKap = reader.IsDBNull(0) ? null : reader.GetDateTime(0);
                        if (dateKap != null)
                            plannedDatesKap.Add((DateTime)dateKap);
                    }
                    result.plannedDatesKap = plannedDatesKap.ToArray();
                }
                selectComm.CommandText = @"SELECT planneddate
                            FROM ""wtb_ssp_config_dates""
                            WHERE date_type='OKS' AND planneddate < current_timestamp
                            ORDER BY planneddate DESC
                            LIMIT 5";
                using (NpgsqlDataReader reader = selectComm.ExecuteReader())
                {
                    List<DateTime?> plannedDatesOks = new List<DateTime?>();
                    while (reader.Read())
                    {
                        DateTime? dateOks = reader.IsDBNull(0) ? null : reader.GetDateTime(0);
                        if (dateOks != null)
                            plannedDatesOks.Add((DateTime)dateOks);
                    }
                    result.plannedDatesOks = plannedDatesOks.ToArray();
                }
            }
            else
            {
                selectComm.CommandText = @"SELECT date_type, planneddate, sks_no
                            FROM ""wtb_ssp_config_dates""
                            WHERE planneddate::timestamp >= CURRENT_DATE
                            ORDER BY planneddate ASC";
                using (NpgsqlDataReader reader = selectComm.ExecuteReader())
                {
                    List<DateTime?> plannedDatesSks = new List<DateTime?>();
                    List<DateTime?> plannedDatesKap = new List<DateTime?>();
                    List<DateTime?> plannedDatesOks = new List<DateTime?>();
                    List<long?> sksNos = new List<long?>();
                    while (reader.Read())
                    {
                        string dateType = reader.IsDBNull(0) ? "" : reader.GetString(0);
                        DateTime? plannedDate = reader.IsDBNull(1) ? null : reader.GetDateTime(1);
                        if (plannedDate != null)
                        {
                            if (dateType == "SKS")
                            {
                                plannedDatesSks.Add((DateTime)plannedDate);
                                long? sksNo = reader.IsDBNull(2) ? (long?)null : reader.GetInt64(2);
                                if (sksNo != null)
                                    sksNos.Add((long)sksNo);
                            }
                            if (dateType == "OKS")
                                plannedDatesOks.Add((DateTime)plannedDate);
                            if (dateType == "KAP")
                                plannedDatesKap.Add((DateTime)plannedDate);
                        }

                    }
                    result.plannedDatesSks = plannedDatesSks.ToArray();
                    result.plannedDatesOks = plannedDatesOks.ToArray();
                    result.plannedDatesKap = plannedDatesKap.ToArray();
                    result.sksNos = sksNos.ToArray();
                }

            }


            pgConn.Close();
        }
        return result;
    }

}

