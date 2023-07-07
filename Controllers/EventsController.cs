using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetTopologySuite.Geometries;
using Npgsql;
using roadwork_portal_service.Configuration;
using roadwork_portal_service.Model;

namespace roadwork_portal_service.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly ILogger<EventsController> _logger;
        private IConfiguration Configuration { get; }

        public EventsController(ILogger<EventsController> logger,
                        IConfiguration configuration)
        {
            _logger = logger;
            this.Configuration = configuration;
        }

        // GET events/
        [HttpGet]
        [Authorize]
        public IEnumerable<EventFeature> GetEvents(string? uuid = "", string? roadWorkActivityUuid = "",
                         bool summary = false)
        {
            List<EventFeature> eventsFromDb = new List<EventFeature>();

            // get data of current user from database:
            using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
            {
                pgConn.Open();
                NpgsqlCommand selectComm = pgConn.CreateCommand();
                selectComm.CommandText = @"SELECT e.uuid, e.name, e.created, e.last_modified,
                            e.date_from, e.date_to, e.geom
                        FROM ""events"" e";

                if (uuid != null)
                {
                    uuid = uuid.Trim().ToLower();
                    if (uuid != "")
                    {
                        selectComm.CommandText += " WHERE e.uuid=@uuid";
                        selectComm.Parameters.AddWithValue("uuid", new Guid(uuid));
                    }
                }

                if ((uuid == null || uuid == "") && roadWorkActivityUuid != null)
                {
                    roadWorkActivityUuid = roadWorkActivityUuid.Trim().ToLower();
                    if (roadWorkActivityUuid != "")
                    {
                        NpgsqlCommand selectIntersectingNeeds = pgConn.CreateCommand();
                        selectComm.CommandText += @", ""roadworkactivities"" r WHERE ST_Intersects(e.geom, r.geom) AND r.uuid=@roadworkactivity_uuid";
                        selectComm.Parameters.AddWithValue("roadworkactivity_uuid", new Guid(roadWorkActivityUuid));
                    }
                }

                using (NpgsqlDataReader reader = selectComm.ExecuteReader())
                {
                    EventFeature eventFeatureFromDb;
                    while (reader.Read())
                    {
                        eventFeatureFromDb = new EventFeature();
                        eventFeatureFromDb.properties.uuid = reader.IsDBNull(0) ? "" : reader.GetGuid(0).ToString();
                        eventFeatureFromDb.properties.name = reader.IsDBNull(1) ? "" : reader.GetString(1);
                        eventFeatureFromDb.properties.created = reader.IsDBNull(2) ? DateTime.MinValue : reader.GetDateTime(2);
                        eventFeatureFromDb.properties.lastModified = reader.IsDBNull(3) ? DateTime.MinValue : reader.GetDateTime(3);
                        eventFeatureFromDb.properties.dateFrom = reader.IsDBNull(4) ? DateTime.MinValue : reader.GetDateTime(4);
                        eventFeatureFromDb.properties.dateTo = reader.IsDBNull(5) ? DateTime.MinValue : reader.GetDateTime(5);

                        Polygon ntsPoly = reader.IsDBNull(6) ? Polygon.Empty : reader.GetValue(6) as Polygon;
                        eventFeatureFromDb.geometry = new RoadworkPolygon(ntsPoly);

                        eventsFromDb.Add(eventFeatureFromDb);
                    }
                }
                pgConn.Close();
            }

            return eventsFromDb.ToArray();
        }

    }
}