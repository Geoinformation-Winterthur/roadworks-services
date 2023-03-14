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
        [Authorize]
        public ActionResult<FeatureCollection> GetManagementAreas()
        {
            FeatureCollection managementAreas = new FeatureCollection();
            // get data of current user from database:
            using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
            {
                pgConn.Open();
                NpgsqlCommand selectComm = pgConn.CreateCommand();
                selectComm.CommandText = @"SELECT uuid, manager, geom
                            FROM ""managementareas""";

                using (NpgsqlDataReader reader = selectComm.ExecuteReader())
                {
                    GeometryFactory geomFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 2056);
                    Feature managementAreaFeatureFromDb;
                    while (reader.Read())
                    {
                        managementAreaFeatureFromDb = new Feature();
                        managementAreaFeatureFromDb.Attributes = new AttributesTable();
                        managementAreaFeatureFromDb.Attributes.Add("uuid", reader.IsDBNull(0) ? "" : reader.GetGuid(0).ToString());
                        managementAreaFeatureFromDb.Attributes.Add("managername", reader.IsDBNull(1) ? "" : reader.GetGuid(1).ToString());
                        managementAreaFeatureFromDb.Geometry = reader.IsDBNull(2) ? Polygon.Empty : reader.GetValue(2) as Polygon;

                        managementAreas.Add(managementAreaFeatureFromDb);
                    }
                }
                pgConn.Close();
            }

            return Ok(managementAreas);
        }

    }
}