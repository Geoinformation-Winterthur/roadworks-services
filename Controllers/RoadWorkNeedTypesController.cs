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
public class RoadWorkNeedTypesController : ControllerBase
{
    private readonly ILogger<RoadWorkNeedTypesController> _logger;

    public RoadWorkNeedTypesController(ILogger<RoadWorkNeedTypesController> logger)
    {
        _logger = logger;
    }


    // GET roadworkneedtypes/
    [HttpGet]
    [Authorize]
    public ActionResult<RoadWorkNeedEnum[]> GetRoadWorkNeedTypes()
    {
        User userFromDb = LoginController.getAuthorizedUserFromDb(this.User);
        List<RoadWorkNeedEnum> roadWorkNeedEnumsFromDb = new List<RoadWorkNeedEnum>();
        using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
        {
            pgConn.Open();
            NpgsqlCommand selectComm = pgConn.CreateCommand();
            selectComm.CommandText = @"SELECT rwt.code, rwt.name
                            FROM ""wtb_ssp_roadworkneedtypes"" rwt";

            if(userFromDb != null){
                selectComm.CommandText += @" JOIN ""wtb_ssp_organisationalunits"" o ON rwt.organisation = o.uuid
                            WHERE o.uuid=@uuid";
                selectComm.Parameters.AddWithValue("uuid", new Guid(userFromDb.organisationalUnit.uuid));
            }

            using (NpgsqlDataReader reader = selectComm.ExecuteReader())
            {
                RoadWorkNeedEnum roadWorkNeedEnumFromDb;
                while (reader.Read())
                {
                    roadWorkNeedEnumFromDb = new RoadWorkNeedEnum();
                    roadWorkNeedEnumFromDb.code =
                            reader.IsDBNull(0) ? "" :
                                    reader.GetString(0);
                    roadWorkNeedEnumFromDb.name =
                            reader.IsDBNull(1) ? "" :
                                    reader.GetString(1);
                    roadWorkNeedEnumsFromDb.Add(roadWorkNeedEnumFromDb);
                }
            }
            pgConn.Close();
        }

        return roadWorkNeedEnumsFromDb.ToArray();
    }

}

