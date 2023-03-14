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
    public class RoadWorkNeedController : ControllerBase
    {
        private readonly ILogger<RoadWorkNeedController> _logger;
        private IConfiguration Configuration { get; }

        public RoadWorkNeedController(ILogger<RoadWorkNeedController> logger,
                        IConfiguration configuration)
        {
            _logger = logger;
            this.Configuration = configuration;
        }

        // GET roadworkneed/
        [HttpGet]
        [Authorize]
        public IEnumerable<RoadWorkNeedFeature> GetProjects(string? uuid = "", bool summary = false)
        {
            List<RoadWorkNeedFeature> projectsFromDb = new List<RoadWorkNeedFeature>();
            // get data of current user from database:
            using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
            {
                pgConn.Open();
                NpgsqlCommand selectComm = pgConn.CreateCommand();
                selectComm.CommandText = @"SELECT uuid, name, kind,
                        orderer, finish_early_from, finish_early_to, finish_optimum_from,
                        finish_optimum_to, finish_late_from, finish_late_to,
                        priority, status, comment, managementarea, geom
                    FROM ""roadworkneeds""";

                if (uuid != null)
                {
                    uuid = uuid.Trim();
                    if (uuid != "")
                    {
                        selectComm.CommandText += " WHERE uuid=@uuid";
                        selectComm.Parameters.AddWithValue("uuid", new Guid(uuid));
                    }
                }

                using (NpgsqlDataReader reader = selectComm.ExecuteReader())
                {
                    RoadWorkNeedFeature needFeatureFromDb;
                    while (reader.Read())
                    {
                        needFeatureFromDb = new RoadWorkNeedFeature();
                        needFeatureFromDb.properties.uuid = reader.IsDBNull(0) ? "" : reader.GetGuid(0).ToString();
                        needFeatureFromDb.properties.name = reader.IsDBNull(1) ? "" : reader.GetString(1);
                        needFeatureFromDb.properties.kind = reader.IsDBNull(2) ? "" : reader.GetString(2);
                        needFeatureFromDb.properties.ordererUuid = reader.IsDBNull(3) ? "" : reader.GetGuid(3).ToString();
                        needFeatureFromDb.properties.finishEarlyFrom = reader.IsDBNull(4) ? DateTime.MinValue : reader.GetDateTime(4);
                        needFeatureFromDb.properties.finishEarlyTo = reader.IsDBNull(5) ? DateTime.MinValue : reader.GetDateTime(5);
                        needFeatureFromDb.properties.finishOptimumFrom = reader.IsDBNull(6) ? DateTime.MinValue : reader.GetDateTime(6);
                        needFeatureFromDb.properties.finishOptimumTo = reader.IsDBNull(7) ? DateTime.MinValue : reader.GetDateTime(7);
                        needFeatureFromDb.properties.finishLateFrom = reader.IsDBNull(8) ? DateTime.MinValue : reader.GetDateTime(8);
                        needFeatureFromDb.properties.finishLateTo = reader.IsDBNull(9) ? DateTime.MinValue : reader.GetDateTime(9);
                        needFeatureFromDb.properties.priorityUuid = reader.IsDBNull(10) ? "" : reader.GetGuid(10).ToString();
                        needFeatureFromDb.properties.statusUuid = reader.IsDBNull(11) ? "" : reader.GetGuid(11).ToString();
                        needFeatureFromDb.properties.comment = reader.IsDBNull(12) ? "" : reader.GetString(12);
                        needFeatureFromDb.properties.managementareaUuid = reader.IsDBNull(13) ? "" : reader.GetGuid(13).ToString();
                        needFeatureFromDb.geometry = reader.IsDBNull(14) ? Polygon.Empty : reader.GetValue(14) as Polygon;

                        projectsFromDb.Add(needFeatureFromDb);
                    }
                }
                pgConn.Close();
            }

            return projectsFromDb.ToArray();
        }

        // POST roadworkneed/?projectid=...
        [HttpPost]
        [Authorize]
        public ActionResult<RoadWorkNeedFeature> AddNeed([FromBody] RoadWorkNeedFeature roadWorkNeedFeature)
        {
            string resultUuidString = "";
            Coordinate[] coordinates = roadWorkNeedFeature.geometry.Coordinates;
            if (coordinates.Length > 2)
            {
                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {
                    try
                    {
                        pgConn.Open();

                        // only if project area is greater than 10qm:
                        if (roadWorkNeedFeature.geometry.Area > 10.0)
                        {
                            Guid resultUuid = Guid.NewGuid();
                            resultUuidString = resultUuid.ToString();

                            NpgsqlCommand insertComm = pgConn.CreateCommand();
                            insertComm.CommandText = @"INSERT INTO ""roadworkneeds""
                                    (uuid, name, kind, orderer, finish_early_from, finish_early_to,
                                    finish_optimum_from, finish_optimum_to, finish_late_from,
                                    finish_late_to, priority, status, comment, managementarea, geom)
                                    VALUES (@uuid, @name, @kind, @orderer, @finish_early_from, @finish_early_to,
                                    @finish_optimum_from, @finish_optimum_to, @finish_late_from,
                                    @finish_late_to, @priority, @status, @comment, @managementarea, @geom)";
                            insertComm.Parameters.AddWithValue("uuid", resultUuidString);
                            insertComm.Parameters.AddWithValue("name", roadWorkNeedFeature.properties.name);
                            insertComm.Parameters.AddWithValue("kind", roadWorkNeedFeature.properties.kind);
                            insertComm.Parameters.AddWithValue("orderer", new Guid(roadWorkNeedFeature.properties.ordererUuid));
                            insertComm.Parameters.AddWithValue("finish_early_from", roadWorkNeedFeature.properties.finishEarlyFrom);
                            insertComm.Parameters.AddWithValue("finish_early_to", roadWorkNeedFeature.properties.finishEarlyTo);
                            insertComm.Parameters.AddWithValue("finish_optimum_from", roadWorkNeedFeature.properties.finishOptimumFrom);
                            insertComm.Parameters.AddWithValue("finish_optimum_to", roadWorkNeedFeature.properties.finishOptimumTo);
                            insertComm.Parameters.AddWithValue("finish_late_from", roadWorkNeedFeature.properties.finishLateFrom);
                            insertComm.Parameters.AddWithValue("finish_late_to", roadWorkNeedFeature.properties.finishLateTo);
                            insertComm.Parameters.AddWithValue("priority", new Guid(roadWorkNeedFeature.properties.priorityUuid));
                            insertComm.Parameters.AddWithValue("status", new Guid(roadWorkNeedFeature.properties.statusUuid));
                            insertComm.Parameters.AddWithValue("comment", roadWorkNeedFeature.properties.comment);
                            insertComm.Parameters.AddWithValue("managementarea", new Guid(roadWorkNeedFeature.properties.managementareaUuid));
                            insertComm.Parameters.AddWithValue("geom", roadWorkNeedFeature.geometry);

                            insertComm.ExecuteNonQuery();
                        }
                        else
                        {
                            return Ok(new WebAppException("Roadwork project area is less than or equal 10qm."));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message);
                        return Ok(new WebAppException(ex.Message));
                    }
                    finally
                    {
                        pgConn.Close();
                    }
                }
            }
            RoadWorkNeedFeature result = new RoadWorkNeedFeature();
            result.properties.uuid = resultUuidString;
            return Ok(result);
        }

        // PUT roadworkneed/?uuid=...
        [HttpPut]
        [Authorize]
        public ActionResult<WebAppException> UpdateNeed([FromBody] RoadWorkNeedFeature roadWorkNeedFeature)
        {
            if (roadWorkNeedFeature != null && roadWorkNeedFeature.geometry != null &&
                    roadWorkNeedFeature.geometry.Coordinates != null && 
                    roadWorkNeedFeature.geometry.Coordinates.Length > 2)
            {

                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {
                    try
                    {
                        pgConn.Open();

                        // only if project area is greater than 10qm:
                        if (roadWorkNeedFeature.geometry.Area > 10.0)
                        {
                            NpgsqlCommand updateComm = pgConn.CreateCommand();
                            updateComm.CommandText = @"UPDATE ""roadworkneeds""
                                    SET name=@name, kind=@kind, orderer=@orderer,
                                    finish_early_from=@finish_early_from, finish_early_to=@finish_early_to,
                                    finish_optimum_from=@finish_optimum_from, finish_optimum_to=@finish_optimum_to,
                                    finish_late_from=@finish_late_from, finish_late_to=@finish_late_to,
                                    priority=@priority, status=@status, comment=@comment,
                                    managementarea=@managementarea, geom=@geom
                                    WHERE uuid=@uuid";

                            updateComm.Parameters.AddWithValue("name", roadWorkNeedFeature.properties.name);
                            updateComm.Parameters.AddWithValue("kind", roadWorkNeedFeature.properties.kind);
                            updateComm.Parameters.AddWithValue("orderer", roadWorkNeedFeature.properties.ordererUuid);
                            updateComm.Parameters.AddWithValue("finish_early_from", roadWorkNeedFeature.properties.finishEarlyFrom);
                            updateComm.Parameters.AddWithValue("finish_early_to", roadWorkNeedFeature.properties.finishEarlyTo);
                            updateComm.Parameters.AddWithValue("finish_optimum_from", roadWorkNeedFeature.properties.finishOptimumFrom);
                            updateComm.Parameters.AddWithValue("finish_optimum_to", roadWorkNeedFeature.properties.finishOptimumTo);
                            updateComm.Parameters.AddWithValue("finish_late_from", roadWorkNeedFeature.properties.finishLateFrom);
                            updateComm.Parameters.AddWithValue("finish_late_to", roadWorkNeedFeature.properties.finishLateTo);
                            updateComm.Parameters.AddWithValue("priority", roadWorkNeedFeature.properties.priorityUuid);
                            updateComm.Parameters.AddWithValue("status", roadWorkNeedFeature.properties.statusUuid);
                            updateComm.Parameters.AddWithValue("comment", roadWorkNeedFeature.properties.comment);
                            updateComm.Parameters.AddWithValue("managementarea", roadWorkNeedFeature.properties.managementareaUuid);
                            updateComm.Parameters.AddWithValue("geom", roadWorkNeedFeature.geometry);
                            updateComm.Parameters.AddWithValue("uuid", roadWorkNeedFeature.properties.uuid);

                            updateComm.ExecuteNonQuery();
                        }
                        else
                        {
                            return Ok(new WebAppException("Roadwork project area is less than or equal 10qm."));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message);
                        return Ok(new WebAppException(ex.Message));
                    }
                    finally
                    {
                        pgConn.Close();
                    }
                }
            }
            return Ok();
        }

    }
}