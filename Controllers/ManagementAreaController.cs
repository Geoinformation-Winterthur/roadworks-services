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
                selectComm.CommandText = @"SELECT m.uuid, m.name, am.first_name || ' ' || am.last_name,
                            sam.first_name || ' ' || sam.last_name, m.color_fill, m.color_stroke, m.geom
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
                        managementAreaFeatureFromDb.Attributes.Add("managername", reader.IsDBNull(2) ? "" : reader.GetString(2));
                        managementAreaFeatureFromDb.Attributes.Add("substitutemanager_name", reader.IsDBNull(3) ? "" : reader.GetString(3));
                        managementAreaFeatureFromDb.Attributes.Add("color_fill", reader.IsDBNull(4) ? "" : reader.GetString(4));
                        managementAreaFeatureFromDb.Attributes.Add("color_stroke", reader.IsDBNull(5) ? "" : reader.GetString(5));
                        managementAreaFeatureFromDb.Geometry = reader.IsDBNull(6) ? Polygon.Empty : reader.GetValue(6) as Polygon;

                        managementAreas.Add(managementAreaFeatureFromDb);
                    }
                }
                pgConn.Close();
            }

            return Ok(managementAreas);
        }

    }
}