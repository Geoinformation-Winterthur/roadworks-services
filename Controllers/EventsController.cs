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
                         bool? temporal = false, bool? spatial = false, bool summary = false)
        {
            List<EventFeature> eventsFromDb = new List<EventFeature>();

            // get data of current user from database:
            using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
            {
                pgConn.Open();
                NpgsqlCommand selectComm = pgConn.CreateCommand();
                selectComm.CommandText = @"SELECT e.uuid, e.name, e.created, e.last_modified,
                            e.date_from, e.date_to, editable, e.geom
                        FROM ""wtb_ssp_events"" e";

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
                        bool spatialParam = false;
                        if (spatial != null)
                        {
                            spatialParam = (bool)spatial;
                        }
                        bool temporalParam = false;
                        if (temporal != null)
                        {
                            temporalParam = (bool)temporal;
                        }

                        if (spatialParam && temporalParam)
                        {
                            selectComm.CommandText += @", ""wtb_ssp_roadworkactivities"" r 
                                                WHERE ST_Intersects(e.geom, r.geom)
                                                        AND (r.date_from <= e.date_to AND r.date_to >= e.date_from)
                                                        AND r.uuid=@roadworkactivity_uuid";
                            selectComm.Parameters.AddWithValue("roadworkactivity_uuid", new Guid(roadWorkActivityUuid));
                        }
                        else if (spatialParam)
                        {
                            selectComm.CommandText += @", ""wtb_ssp_roadworkactivities"" r 
                                                WHERE ST_Intersects(e.geom, r.geom)
                                                        AND (r.date_from > e.date_to OR r.date_to < e.date_from)
                                                        AND r.uuid=@roadworkactivity_uuid";
                            selectComm.Parameters.AddWithValue("roadworkactivity_uuid", new Guid(roadWorkActivityUuid));
                        }
                        else if (temporalParam)
                        {
                            selectComm.CommandText += @", ""wtb_ssp_roadworkactivities"" r
                                                WHERE NOT ST_Intersects(e.geom, r.geom) 
                                                    AND (r.date_from <= e.date_to AND r.date_to >= e.date_from)
                                                    AND r.uuid=@roadworkactivity_uuid";
                            selectComm.Parameters.AddWithValue("roadworkactivity_uuid", new Guid(roadWorkActivityUuid));
                        }
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
                        eventFeatureFromDb.properties.isEditingAllowed = reader.IsDBNull(6) ? false : reader.GetBoolean(6);

                        Polygon ntsPoly = reader.IsDBNull(7) ? Polygon.Empty : reader.GetValue(7) as Polygon;
                        eventFeatureFromDb.geometry = new RoadworkPolygon(ntsPoly);

                        eventsFromDb.Add(eventFeatureFromDb);
                    }
                }
                pgConn.Close();
            }

            return eventsFromDb.ToArray();
        }

        // POST events/
        [HttpPost]
        [Authorize]
        public ActionResult<EventFeature> AddEvent([FromBody] EventFeature eventFeature)
        {
            try
            {
                if (eventFeature == null)
                {
                    _logger.LogWarning("No event feature received in update event feature method.");
                    eventFeature = new EventFeature();
                    eventFeature.errorMessage = "KOPAL-3";
                    return Ok(eventFeature);
                }

                if (eventFeature.geometry == null ||
                        eventFeature.geometry.coordinates == null ||
                        eventFeature.geometry.coordinates.Length < 3)
                {
                    _logger.LogWarning("Event feature has a geometry error.");
                    eventFeature.errorMessage = "KOPAL-3";
                    return Ok(eventFeature);
                }

                Polygon eventPoly = eventFeature.geometry.getNtsPolygon();

                if (!eventPoly.IsSimple)
                {
                    _logger.LogWarning("Geometry of event feature " + eventFeature.properties.uuid +
                            " does not fulfill the criteria of geometrical simplicity.");
                    eventFeature.errorMessage = "KOPAL-10";
                    return Ok(eventFeature);
                }

                if (!eventPoly.IsValid)
                {
                    _logger.LogWarning("Geometry of event feature " + eventFeature.properties.uuid +
                            " does not fulfill the criteria of geometrical validity.");
                    eventFeature.errorMessage = "KOPAL-11";
                    return Ok(eventFeature);
                }

                if (eventFeature.properties.dateFrom > eventFeature.properties.dateTo)
                {
                    _logger.LogWarning("The finish from date of aan event feature cannot be higher than its finish to date.");
                    eventFeature.errorMessage = "KOPAL-19";
                    return Ok(eventFeature);
                }

                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {

                    pgConn.Open();

                    NpgsqlCommand updateComm = pgConn.CreateCommand();
                    updateComm.CommandText = @"INSERT INTO ""wtb_ssp_events"" (uuid, name,
                                    created, last_modified, date_from, date_to,
                                    editable, geom)
                                    VALUES (@uuid, @name, current_timestamp,
                                    current_timestamp, @date_from, @date_to,
                                    true, @geom)";

                    eventFeature.properties.uuid = Guid.NewGuid().ToString();
                    updateComm.Parameters.AddWithValue("uuid", new Guid(eventFeature.properties.uuid));
                    updateComm.Parameters.AddWithValue("name", eventFeature.properties.name);
                    updateComm.Parameters.AddWithValue("date_from", eventFeature.properties.dateFrom);
                    updateComm.Parameters.AddWithValue("date_to", eventFeature.properties.dateTo);
                    updateComm.Parameters.AddWithValue("geom", eventPoly);

                    updateComm.ExecuteNonQuery();

                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                eventFeature.errorMessage = "KOPAL-3";
                return Ok(eventFeature);
            }

            return Ok(eventFeature);
        }

        // PUT events/
        [HttpPut]
        [Authorize]
        public ActionResult<EventFeature> UpdateEvent([FromBody] EventFeature eventFeature)
        {
            try
            {
                if (eventFeature == null)
                {
                    _logger.LogWarning("No event feature received in update event feature method.");
                    eventFeature = new EventFeature();
                    eventFeature.errorMessage = "KOPAL-3";
                    return Ok(eventFeature);
                }

                if (eventFeature.geometry == null ||
                        eventFeature.geometry.coordinates == null ||
                        eventFeature.geometry.coordinates.Length < 3)
                {
                    _logger.LogWarning("Event feature has a geometry error.");
                    eventFeature.errorMessage = "KOPAL-3";
                    return Ok(eventFeature);
                }

                Polygon eventPoly = eventFeature.geometry.getNtsPolygon();

                if (!eventPoly.IsSimple)
                {
                    _logger.LogWarning("Geometry of event feature " + eventFeature.properties.uuid +
                            " does not fulfill the criteria of geometrical simplicity.");
                    eventFeature.errorMessage = "KOPAL-10";
                    return Ok(eventFeature);
                }

                if (!eventPoly.IsValid)
                {
                    _logger.LogWarning("Geometry of event feature " + eventFeature.properties.uuid +
                            " does not fulfill the criteria of geometrical validity.");
                    eventFeature.errorMessage = "KOPAL-11";
                    return Ok(eventFeature);
                }

                if (eventFeature.properties.dateFrom > eventFeature.properties.dateTo)
                {
                    _logger.LogWarning("The finish from date of aan event feature cannot be higher than its finish to date.");
                    eventFeature.errorMessage = "KOPAL-19";
                    return Ok(eventFeature);
                }

                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {

                    pgConn.Open();

                    NpgsqlCommand updateComm = pgConn.CreateCommand();
                    updateComm.CommandText = @"UPDATE ""wtb_ssp_events""
                                    SET name=@name, last_modified=current_timestamp,
                                    date_from=@date_from, date_to=@date_to, geom=@geom
                                    WHERE uuid=@uuid AND editable=true";

                    updateComm.Parameters.AddWithValue("name", eventFeature.properties.name);
                    updateComm.Parameters.AddWithValue("date_from", eventFeature.properties.dateFrom);
                    updateComm.Parameters.AddWithValue("date_to", eventFeature.properties.dateTo);
                    updateComm.Parameters.AddWithValue("geom", eventPoly);
                    updateComm.Parameters.AddWithValue("uuid", new Guid(eventFeature.properties.uuid));

                    updateComm.ExecuteNonQuery();

                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                eventFeature.errorMessage = "KOPAL-3";
                return Ok(eventFeature);
            }

            return Ok(eventFeature);
        }

        // DELETE /events?uuid=...
        [HttpDelete]
        [Authorize]
        public ActionResult<ErrorMessage> DeleteEvent(string uuid)
        {
            ErrorMessage errorResult = new ErrorMessage();

            try
            {

                if (uuid == null)
                {
                    _logger.LogWarning("No uuid provided by user in delete event feature process. " +
                                "Thus process is canceled, no event feature is deleted.");
                    errorResult.errorMessage = "KOPAL-15";
                    return Ok(errorResult);
                }

                uuid = uuid.ToLower().Trim();

                if (uuid == "")
                {
                    _logger.LogWarning("No uuid provided by user in delete event feature process. " +
                                "Thus process is canceled, no event feature is deleted.");
                    errorResult.errorMessage = "KOPAL-15";
                    return Ok(errorResult);
                }

                int noAffectedRows = 0;
                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {
                    pgConn.Open();

                    NpgsqlCommand deleteComm = pgConn.CreateCommand();
                    deleteComm = pgConn.CreateCommand();
                    deleteComm.CommandText = @"DELETE FROM ""wtb_ssp_events""
                                WHERE uuid=@uuid AND editable=true";
                    deleteComm.Parameters.AddWithValue("uuid", new Guid(uuid));
                    noAffectedRows = deleteComm.ExecuteNonQuery();

                    pgConn.Close();
                }

                if (noAffectedRows == 1)
                {
                    return Ok();
                }

                _logger.LogError("Unknown error.");
                errorResult.errorMessage = "KOPAL-3";
                return Ok(errorResult);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                errorResult.errorMessage = "KOPAL-3";
                return Ok(errorResult);
            }

        }

    }
}