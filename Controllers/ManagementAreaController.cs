using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.Features;
using Npgsql;
using roadwork_portal_service.Configuration;
using System.Numerics;

namespace roadwork_portal_service.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ManagementAreaController : ControllerBase
    {
        private readonly ILogger<ManagementAreaController> _logger;
        private IConfiguration Configuration { get; }

        public ManagementAreaController(ILogger<ManagementAreaController> logger,
                        IConfiguration configuration)
        {
            _logger = logger;
            this.Configuration = configuration;
        }

        // GET managementarea/
        [HttpGet]
        [Authorize(Roles = "territorymanager,administrator")]
        public ActionResult<FeatureCollection> GetManagementAreas()
        {
            FeatureCollection managementAreas = new FeatureCollection();
            // get data of current user from database:
            using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
            {
                pgConn.Open();
                NpgsqlCommand selectComm = pgConn.CreateCommand();
                selectComm.CommandText = @"SELECT m.uuid, m.name, am.uuid, am.first_name || ' ' || am.last_name,
                            sam.uuid, sam.first_name || ' ' || sam.last_name, m.color_fill, m.color_stroke, m.geom
                            FROM ""managementareas"" m
                            LEFT JOIN ""users"" am ON m.manager = am.uuid
                            LEFT JOIN ""users"" sam ON m.substitute_manager = sam.uuid";

                using (NpgsqlDataReader reader = selectComm.ExecuteReader())
                {
                    GeometryFactory geomFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 2056);
                    Feature managementAreaFeatureFromDb;
                    while (reader.Read())
                    {
                        managementAreaFeatureFromDb = new Feature();
                        managementAreaFeatureFromDb.Attributes = new AttributesTable();
                        managementAreaFeatureFromDb.Attributes.Add("uuid", reader.IsDBNull(0) ? "" : reader.GetGuid(0).ToString());
                        managementAreaFeatureFromDb.Attributes.Add("name", reader.IsDBNull(1) ? "" : reader.GetString(1));
                        managementAreaFeatureFromDb.Attributes.Add("manager_uuid", reader.IsDBNull(2) ? "" : reader.GetGuid(2).ToString());
                        managementAreaFeatureFromDb.Attributes.Add("managername", reader.IsDBNull(3) ? "" : reader.GetString(3));
                        managementAreaFeatureFromDb.Attributes.Add("substitutemanager_uuid", reader.IsDBNull(4) ? "" : reader.GetGuid(4).ToString());
                        managementAreaFeatureFromDb.Attributes.Add("substitutemanager_name", reader.IsDBNull(5) ? "" : reader.GetString(5));
                        managementAreaFeatureFromDb.Attributes.Add("color_fill", reader.IsDBNull(6) ? "" : reader.GetString(6));
                        managementAreaFeatureFromDb.Attributes.Add("color_stroke", reader.IsDBNull(7) ? "" : reader.GetString(7));
                        managementAreaFeatureFromDb.Geometry = reader.IsDBNull(8) ? Polygon.Empty : reader.GetValue(8) as Polygon;

                        managementAreas.Add(managementAreaFeatureFromDb);
                    }
                }
                pgConn.Close();
            }

            return Ok(managementAreas);
        }

    }
}