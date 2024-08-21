// <copyright company="Geoinformation Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Geoinformation Winterthur. All rights reserved.
// </copyright>
using roadwork_portal_service.Configuration;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using roadwork_portal_service.Model;

namespace energieportal_service.Controllers;

[ApiController]
[Route("[controller]")]
public class AddressController : ControllerBase
{
    private readonly ILogger<AddressController> _logger;

    public AddressController(ILogger<AddressController> logger)
    {
        _logger = logger;
    }


    /// <summary>
    /// Retrieves a collection of all official addresses of Winterthur that
    /// match the search string
    /// </summary>
    /// <remarks>
    /// The search string has to be <i>at least 3 characters long</i>.
    /// This search is <i>case insensitive</i>. It means, it doesn't matter,
    /// if the provided search string is upper or lower case, the search
    /// results will be the same. If the search string is less than 3
    /// characters, then an <i>empty array</i> is returned.
    /// </remarks>
    /// <response code="200">
    /// The data is returned as an array of strings.
    /// </response>
    [HttpGet]
    [ProducesResponseType(typeof(string[]), 200)]
    public async Task<ActionResult<Address[]>> GetAddresses(string? search = "")
    {
        try
        {
            search = search != null ? search.Trim().ToLower() : "";

            List<Address> addressesFromDb = new List<Address>();
            if (search != "")
            {
                search = search.Trim();
                if (search.Length > 3)
                {
                    using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                    {
                        pgConn.Open();
                        NpgsqlCommand selectComm = pgConn.CreateCommand();
                        selectComm.CommandText = @"SELECT egaid, address, ST_X(geom), ST_Y(geom)
                                        FROM ""addresses""
                                        WHERE LOWER(address) LIKE @search
                                        ORDER BY address
                                        LIMIT 5";
                        selectComm.Parameters.AddWithValue("search", search + "%");

                        using (NpgsqlDataReader reader = await selectComm.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                Address addressFromDb = new Address();
                                if (!reader.IsDBNull(0)) addressFromDb.egaid = reader.GetInt32(0);
                                addressFromDb.address = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                if (!reader.IsDBNull(2)) addressFromDb.x = reader.GetDouble(2);
                                if (!reader.IsDBNull(3)) addressFromDb.y = reader.GetDouble(3);
                                addressesFromDb.Add(addressFromDb);
                            }
                        }
                        await pgConn.CloseAsync();
                    }
                }
            }
            return addressesFromDb.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return BadRequest();
        }

    }

}

