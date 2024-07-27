using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetTopologySuite.Geometries;
using Npgsql;
using roadwork_portal_service.Configuration;
using roadwork_portal_service.DAO;
using roadwork_portal_service.Helper;
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

        // GET roadworkneed/?year=2023&uuids=...&roadworkactivityuuid=...
        [HttpGet]
        [Authorize(Roles = "orderer,trefficmanager,territorymanager,administrator")]
        public IEnumerable<RoadWorkNeedFeature> GetNeeds(int? relevance,
                DateTime? dateOfCreation, int? year, string? uuids,
                string? roadWorkActivityUuid, string? name,
                string? areaManagerUuid, bool? onlyMyNeeds, string? status,
                bool summary = false)
        {
            List<RoadWorkNeedFeature> projectsFromDb = new List<RoadWorkNeedFeature>();
            try
            {

                if (year == null)
                    year = 0;

                if (uuids == null)
                    uuids = "";
                else
                    uuids = uuids.Trim().ToLower();

                if (roadWorkActivityUuid == null)
                    roadWorkActivityUuid = "";
                else
                    roadWorkActivityUuid = roadWorkActivityUuid.Trim().ToLower();

                if (name == null)
                    name = "";
                else
                    name = name.Trim().ToLower();

                if (status == null)
                    status = "";
                else
                    status = status.Trim().ToLower();
                string[] statusArray = status.Split(",");

                if (areaManagerUuid == null)
                    areaManagerUuid = "";
                else
                    areaManagerUuid = areaManagerUuid.Trim().ToLower();

                if (onlyMyNeeds == null)
                    onlyMyNeeds = false;

                User userFromDb = LoginController.getAuthorizedUserFromDb(this.User, false);

                // get data of current user from database:
                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {
                    pgConn.Open();
                    NpgsqlCommand selectComm = pgConn.CreateCommand();
                    selectComm.CommandText = @"SELECT r.uuid, r.name, r.orderer,
                            u.first_name, u.last_name, o.name as orgname, r.finish_early_to,
                            r.finish_optimum_to, r.finish_late_to, p.code, s.code, s.name as statusname,
                            r.description, 
                            r.created, r.last_modified, an.uuid_roadwork_activity, u.e_mail,
                            r.relevance, an.activityrelationtype, r.costs,
                            r.note_of_area_man, r.area_man_note_date,
                            n.first_name as area_manager_first_name, n.last_name as area_manager_last_name,
                            r.private, r.section, r.comment, r.url, o.is_civil_eng, o.abbreviation,
                            r.overarching_measure, r.desired_year_from, r.desired_year_to,
                            u.e_mail, r.geom
                        FROM ""wtb_ssp_roadworkneeds"" r
                        LEFT JOIN ""wtb_ssp_activities_to_needs"" an ON an.uuid_roadwork_need = r.uuid
                        LEFT JOIN ""wtb_ssp_users"" u ON r.orderer = u.uuid
                        LEFT JOIN ""wtb_ssp_organisationalunits"" o ON u.org_unit = o.uuid
                        LEFT JOIN ""wtb_ssp_priorities"" p ON r.priority = p.code
                        LEFT JOIN ""wtb_ssp_status"" s ON r.status = s.code
                        LEFT JOIN ""wtb_ssp_users"" n ON r.area_man_of_note = n.uuid";

                    if (roadWorkActivityUuid != "")
                    {
                        selectComm.CommandText += @" LEFT JOIN ""wtb_ssp_roadworkactivities"" act ON act.uuid = @act_uuid";
                        selectComm.Parameters.AddWithValue("act_uuid", new Guid(roadWorkActivityUuid));
                    }

                    selectComm.CommandText += " WHERE (r.private = false OR r.orderer = @orderer_uuid)";
                    selectComm.Parameters.AddWithValue("orderer_uuid", new Guid(userFromDb.uuid));

                    if (uuids != "")
                    {
                        List<Guid> uuidsList = new List<Guid>();

                        foreach (string uuid in uuids.Split(","))
                        {
                            if (uuid != null && uuid != "")
                            {
                                uuidsList.Add(new Guid(uuid));
                            }
                        }
                        selectComm.CommandText += " AND r.uuid = ANY (:uuids)";
                        selectComm.Parameters.AddWithValue("uuids", uuidsList);

                    }
                    else if (roadWorkActivityUuid != "")
                    {
                        selectComm.CommandText += @" AND ST_Intersects(act.geom, r.geom)";
                        selectComm.Parameters.AddWithValue("act_uuid", new Guid(roadWorkActivityUuid));
                    }
                    else
                    {
                        if (year != 0)
                        {
                            selectComm.CommandText += " AND EXTRACT(YEAR FROM r.finish_optimum_to) = @year";
                            selectComm.Parameters.AddWithValue("year", year);
                        }

                        if (name != "")
                        {
                            selectComm.CommandText += " AND LOWER(r.name) LIKE @name";
                            selectComm.Parameters.AddWithValue("name", "%" + name + "%");
                        }

                        if (relevance != null)
                        {
                            selectComm.CommandText += " AND r.relevance = @relevance";
                            selectComm.Parameters.AddWithValue("relevance", relevance);
                        }

                        if (dateOfCreation != null)
                        {
                            selectComm.CommandText += " AND r.created = @created";
                            selectComm.Parameters.AddWithValue("created", dateOfCreation);
                        }

                        if ((bool)onlyMyNeeds)
                        {
                            selectComm.CommandText += " AND r.orderer = @orderer_uuid";
                        }

                        if (statusArray[0] != "")
                        {
                            selectComm.CommandText += " AND (r.status=@status0";
                            selectComm.Parameters.AddWithValue("status0", statusArray[0]);

                            if (statusArray.Length > 1)
                            {
                                for (int i = 1; i < statusArray.Length; i++)
                                {
                                    selectComm.CommandText += " OR ";
                                    selectComm.CommandText += "r.status=@status" + i;
                                    selectComm.Parameters.AddWithValue("status" + i, statusArray[i]);
                                }
                            }
                            selectComm.CommandText += ")";
                        }
                    }

                    List<RoadWorkNeedFeature> projectsFromDbTemp = new List<RoadWorkNeedFeature>();

                    using (NpgsqlDataReader reader = selectComm.ExecuteReader())
                    {
                        RoadWorkNeedFeature needFeatureFromDb;
                        while (reader.Read())
                        {
                            needFeatureFromDb = new RoadWorkNeedFeature();
                            needFeatureFromDb.properties.uuid =
                                    reader.IsDBNull(reader.GetOrdinal("uuid")) ?
                                        "" : reader.GetGuid(reader.GetOrdinal("uuid")).ToString();
                            needFeatureFromDb.properties.name =
                                    reader.IsDBNull(reader.GetOrdinal("name")) ?
                                        "" : reader.GetString(reader.GetOrdinal("name"));
                            User orderer = new User();
                            orderer.uuid = reader.IsDBNull(2) ? "" : reader.GetGuid(2).ToString();
                            orderer.firstName = reader.IsDBNull(3) ? "" : reader.GetString(3);
                            orderer.lastName = reader.IsDBNull(4) ? "" : reader.GetString(4);
                            OrganisationalUnit orgUnit = new OrganisationalUnit();
                            orgUnit.name = reader.IsDBNull(5) ? "" : reader.GetString(5);
                            orderer.organisationalUnit = orgUnit;
                            needFeatureFromDb.properties.orderer = orderer;
                            needFeatureFromDb.properties.finishEarlyTo = reader.IsDBNull(6) ? DateTime.MinValue : reader.GetDateTime(6);
                            needFeatureFromDb.properties.finishOptimumTo = reader.IsDBNull(7) ? DateTime.MinValue : reader.GetDateTime(7);
                            needFeatureFromDb.properties.finishLateTo = reader.IsDBNull(8) ? DateTime.MinValue : reader.GetDateTime(8);
                            Priority priority = new Priority();
                            priority.code = reader.IsDBNull(9) ? "" : reader.GetString(9);
                            needFeatureFromDb.properties.priority = priority;
                            Status statusObj = new Status();
                            statusObj.code = reader.IsDBNull(10) ? "" : reader.GetString(10);
                            statusObj.name = reader.IsDBNull(11) ? "" : reader.GetString(11);
                            needFeatureFromDb.properties.status = statusObj;
                            needFeatureFromDb.properties.description = reader.IsDBNull(12) ? "" : reader.GetString(12);

                            needFeatureFromDb.properties.created = reader.IsDBNull(13) ? DateTime.MinValue : reader.GetDateTime(13);
                            needFeatureFromDb.properties.lastModified = reader.IsDBNull(14) ? DateTime.MinValue : reader.GetDateTime(14);
                            needFeatureFromDb.properties.roadWorkActivityUuid = reader.IsDBNull(15) ? "" : reader.GetGuid(15).ToString();

                            string ordererMailAddress = reader.IsDBNull(16) ? "" : reader.GetString(16);
                            string mailOfLoggedInUser = User.FindFirstValue(ClaimTypes.Email);

                            needFeatureFromDb.properties.relevance = reader.IsDBNull(17) ? 0 : reader.GetInt32(17);
                            needFeatureFromDb.properties.activityRelationType = reader.IsDBNull(18) ? "" : reader.GetString(18);
                            needFeatureFromDb.properties.costs = reader.IsDBNull(19) ? 0 : reader.GetInt32(19);
                            needFeatureFromDb.properties.noteOfAreaManager = reader.IsDBNull(20) ? "" : reader.GetString(20);
                            needFeatureFromDb.properties.areaManagerNoteDate = reader.IsDBNull(21) ? DateTime.MinValue : reader.GetDateTime(21);

                            User areaManagerOfNote = new User();
                            areaManagerOfNote.firstName = reader.IsDBNull(22) ? "" : reader.GetString(22);
                            areaManagerOfNote.lastName = reader.IsDBNull(23) ? "" : reader.GetString(23);
                            needFeatureFromDb.properties.areaManagerOfNote = areaManagerOfNote;
                            needFeatureFromDb.properties.isPrivate = reader.IsDBNull(24) ? true : reader.GetBoolean(24);
                            needFeatureFromDb.properties.section = reader.IsDBNull(25) ? "" : reader.GetString(25);
                            needFeatureFromDb.properties.comment = reader.IsDBNull(26) ? "" : reader.GetString(26);
                            needFeatureFromDb.properties.url = reader.IsDBNull(27) ? "" : reader.GetString(27);
                            needFeatureFromDb.properties.orderer.organisationalUnit.isCivilEngineering = reader.IsDBNull(28) ? false : reader.GetBoolean(28);

                            orgUnit.abbreviation = reader.IsDBNull(29) ? "" : reader.GetString(29);

                            needFeatureFromDb.properties.overarchingMeasure = reader.IsDBNull(30) ? false : reader.GetBoolean(30);
                            needFeatureFromDb.properties.desiredYearFrom = reader.IsDBNull(31) ? null : reader.GetInt32(31);
                            needFeatureFromDb.properties.desiredYearTo = reader.IsDBNull(32) ? null : reader.GetInt32(32);

                            needFeatureFromDb.properties.orderer.mailAddress = reader.IsDBNull(33) ? "" : reader.GetString(33);

                            Polygon ntsPoly = reader.IsDBNull(34) ? Polygon.Empty : reader.GetValue(34) as Polygon;
                            needFeatureFromDb.geometry = new RoadworkPolygon(ntsPoly);

                            if (User.IsInRole("administrator"))
                            {
                                needFeatureFromDb.properties.isEditingAllowed = true;
                            }
                            else if (ordererMailAddress == mailOfLoggedInUser
                                        && needFeatureFromDb.properties.isPrivate)
                            {
                                // editing for the orderer is only allowed as long as the need is not public (is private):
                                needFeatureFromDb.properties.isEditingAllowed = true;
                            }

                            projectsFromDbTemp.Add(needFeatureFromDb);
                        }
                    }

                    if (areaManagerUuid != "")
                    {
                        foreach (RoadWorkNeedFeature needFeatureFromDb in projectsFromDbTemp)
                        {
                            NpgsqlCommand selectManAreaComm = pgConn.CreateCommand();
                            selectManAreaComm.CommandText = @"SELECT am.uuid
                                            FROM ""wtb_ssp_managementareas"" m
                                            LEFT JOIN ""wtb_ssp_users"" am ON m.manager = am.uuid
                                            WHERE ST_Area(ST_Intersection(@geom, m.geom)) > 0
                                            ORDER BY ST_Area(ST_Intersection(@geom, m.geom)) DESC
                                            LIMIT 1";
                            selectManAreaComm.Parameters.AddWithValue("geom", needFeatureFromDb.geometry.getNtsPolygon());

                            using (NpgsqlDataReader selectManAreaReader = selectManAreaComm.ExecuteReader())
                            {
                                if (selectManAreaReader.Read())
                                {
                                    string areaManagerUuidFromDb = selectManAreaReader.IsDBNull(0) ? "" : selectManAreaReader.GetGuid(0).ToString();
                                    if (areaManagerUuid == areaManagerUuidFromDb)
                                        projectsFromDb.Add(needFeatureFromDb);
                                }
                            }
                        }

                    }
                    else
                    {
                        projectsFromDb = projectsFromDbTemp;
                    }
                    pgConn.Close();
                }

                return projectsFromDb.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                RoadWorkNeedFeature roadWorkNeedFeature = new RoadWorkNeedFeature();
                roadWorkNeedFeature.errorMessage = "SSP-3";
                projectsFromDb = new List<RoadWorkNeedFeature>();
                projectsFromDb.Add(roadWorkNeedFeature);
                return projectsFromDb;
            }

        }

        // POST roadworkneed/
        [HttpPost]
        [Authorize(Roles = "orderer,administrator")]
        public ActionResult<RoadWorkNeedFeature> AddNeed([FromBody] RoadWorkNeedFeature roadWorkNeedFeature, bool isDryRun = false)
        {
            try
            {
                User userFromDb = LoginController.getAuthorizedUserFromDb(this.User, false);
                roadWorkNeedFeature.properties.orderer = userFromDb;

                Polygon roadWorkNeedPoly = roadWorkNeedFeature.geometry.getNtsPolygon();
                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {
                    pgConn.Open();
                    if (roadWorkNeedFeature.properties.name == null || roadWorkNeedFeature.properties.name == "")
                    {
                        roadWorkNeedFeature.properties.name = HelperFunctions.getAddressNames(roadWorkNeedPoly, pgConn);
                    }
                }

                ConfigurationData configData = AppConfigController.getConfigurationFromDb();

                RoadWorkNeedDAO roadWorkNeedDAO = new RoadWorkNeedDAO(isDryRun);
                roadWorkNeedFeature = roadWorkNeedDAO.Insert(roadWorkNeedFeature, configData);

                if (roadWorkNeedFeature.errorMessage == "SSP-23")
                    _logger.LogWarning("The provided roadworkneed data has no description attribute value." +
                                " But description is mandatory.");
                else if (roadWorkNeedFeature.errorMessage == "SSP-7")
                    _logger.LogWarning("Roadworkneed Polygon has less than 3 coordinates.");
                else if (roadWorkNeedFeature.errorMessage == "SSP-8")
                    _logger.LogWarning("Roadworkneed area is less than or equal " + configData.minAreaSize + "qm.");
                else if (roadWorkNeedFeature.errorMessage == "SSP-16")
                    _logger.LogWarning("Roadworkneed area is less than or equal " + configData.minAreaSize + "qm.");
                else if (roadWorkNeedFeature.errorMessage == "SSP-22")
                    _logger.LogWarning("No roadworkneed data received.");
                else if (roadWorkNeedFeature.errorMessage == "SSP-26")
                    _logger.LogWarning("URI of given roadwork need is not valid.");
                else if (roadWorkNeedFeature.errorMessage == "SSP-27")
                    _logger.LogWarning("The given desired year value of a newly created roadwork need is invalid.");
                else if (roadWorkNeedFeature.errorMessage == "SSP-30")
                    _logger.LogWarning("The given relevance value of a newly created roadwork need is invalid.");

                return Ok(roadWorkNeedFeature);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                roadWorkNeedFeature.errorMessage = "SSP-3";
                return Ok(roadWorkNeedFeature);
            }

        }

        // PUT roadworkneed/
        [HttpPut]
        [Authorize(Roles = "orderer,administrator")]
        public ActionResult<RoadWorkNeedFeature> UpdateNeed([FromBody] RoadWorkNeedFeature roadWorkNeedFeature)
        {

            try
            {

                if (roadWorkNeedFeature == null)
                {
                    _logger.LogWarning("No roadworkneed data received.");
                    RoadWorkNeedFeature errorObj = new RoadWorkNeedFeature();
                    errorObj.errorMessage = "SSP-22";
                    return Ok(errorObj);
                }

                if (roadWorkNeedFeature.properties.description == null)
                    roadWorkNeedFeature.properties.description = "";
                else
                    roadWorkNeedFeature.properties.description = roadWorkNeedFeature.properties.description.Trim();

                if (roadWorkNeedFeature.properties.description == "")
                {
                    _logger.LogWarning("The provided roadworkneed data has no description attribute value." +
                                " But description is mandatory.");
                    roadWorkNeedFeature.errorMessage = "SSP-23";
                    return Ok(roadWorkNeedFeature);
                }

                if (roadWorkNeedFeature.properties.overarchingMeasure &&
                        ((roadWorkNeedFeature.properties.desiredYearFrom == null
                            || roadWorkNeedFeature.properties.desiredYearFrom < DateTime.Now.Year) ||
                        (roadWorkNeedFeature.properties.desiredYearTo == null
                            || roadWorkNeedFeature.properties.desiredYearTo < DateTime.Now.Year)))
                {
                    _logger.LogWarning("The given desired year value of an updated roadwork need is invalid.");
                    roadWorkNeedFeature.errorMessage = "SSP-27";
                    return Ok(roadWorkNeedFeature);
                }

                Uri uri;
                bool isUri = Uri.TryCreate(roadWorkNeedFeature.properties.url, UriKind.Absolute, out uri);
                if (roadWorkNeedFeature.properties.url != "" && !isUri)
                {
                    _logger.LogWarning("URI of given roadwork need is not valid.");
                    roadWorkNeedFeature.errorMessage = "SSP-26";
                    return Ok(roadWorkNeedFeature);
                }

                if (roadWorkNeedFeature.geometry == null ||
                        roadWorkNeedFeature.geometry.coordinates == null ||
                            roadWorkNeedFeature.geometry.coordinates.Length < 3)
                {
                    _logger.LogWarning("Roadworkneed has a geometry error.");
                    roadWorkNeedFeature = new RoadWorkNeedFeature();
                    roadWorkNeedFeature.errorMessage = "SSP-3";
                    return Ok(roadWorkNeedFeature);
                }

                Polygon roadWorkNeedPoly = roadWorkNeedFeature.geometry.getNtsPolygon();

                ConfigurationData configData = AppConfigController.getConfigurationFromDb();
                // only if project area is greater than min area size:
                if (roadWorkNeedPoly.Area <= configData.minAreaSize)
                {
                    _logger.LogWarning("Roadworkneed area is less than or equal " + configData.minAreaSize + "qm.");
                    roadWorkNeedFeature = new RoadWorkNeedFeature();
                    roadWorkNeedFeature.errorMessage = "SSP-8";
                    return Ok(roadWorkNeedFeature);
                }

                // only if project area is smaller than max area size:
                if (roadWorkNeedPoly.Area > configData.maxAreaSize)
                {
                    _logger.LogWarning("Roadworkneed area is greater than " + configData.maxAreaSize + "qm.");
                    roadWorkNeedFeature = new RoadWorkNeedFeature();
                    roadWorkNeedFeature.errorMessage = "SSP-16";
                    return Ok(roadWorkNeedFeature);
                }

                if (!roadWorkNeedPoly.IsSimple)
                {
                    _logger.LogWarning("Geometry of roadworkneed " + roadWorkNeedFeature.properties.uuid +
                            " does not fulfill the criteria of geometrical simplicity.");
                    roadWorkNeedFeature.errorMessage = "SSP-10";
                    return Ok(roadWorkNeedFeature);
                }

                if (!roadWorkNeedPoly.IsValid)
                {
                    _logger.LogWarning("Geometry of roadworkneed " + roadWorkNeedFeature.properties.uuid +
                            " does not fulfill the criteria of geometrical validity.");
                    roadWorkNeedFeature.errorMessage = "SSP-11";
                    return Ok(roadWorkNeedFeature);
                }

                User userFromDb = LoginController.getAuthorizedUserFromDb(this.User, false);

                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {

                    pgConn.Open();

                    if (!User.IsInRole("administrator"))
                    {

                        NpgsqlCommand selectOrdererOfNeedComm = pgConn.CreateCommand();
                        selectOrdererOfNeedComm.CommandText = @"SELECT u.e_mail
                                    FROM ""wtb_ssp_roadworkneeds"" r
                                    LEFT JOIN ""wtb_ssp_users"" u ON r.orderer = u.uuid
                                    WHERE r.uuid=@uuid";
                        selectOrdererOfNeedComm.Parameters.AddWithValue("uuid", new Guid(roadWorkNeedFeature.properties.uuid));

                        string eMailOfOrderer = "";

                        using (NpgsqlDataReader reader = selectOrdererOfNeedComm.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                eMailOfOrderer = reader.IsDBNull(0) ? "" : reader.GetString(0);
                            }
                        }

                        string mailOfLoggedInUser = User.FindFirstValue(ClaimTypes.Email);

                        if (mailOfLoggedInUser != eMailOfOrderer)
                        {
                            _logger.LogWarning("User " + mailOfLoggedInUser + " has no right to edit " +
                                "roadwork need " + roadWorkNeedFeature.properties.uuid + " but tried " +
                                "to edit it.");
                            roadWorkNeedFeature.errorMessage = "SSP-14";
                            return Ok(roadWorkNeedFeature);
                        }

                    }

                    using (NpgsqlTransaction updateTransAction = pgConn.BeginTransaction())
                    {

                        NpgsqlCommand updateComm = pgConn.CreateCommand();
                        updateComm.CommandText = @"UPDATE ""wtb_ssp_roadworkneeds""
                                    SET name=@name, orderer=@orderer, last_modified=@last_modified,
                                    finish_early_to=@finish_early_to, finish_optimum_to=@finish_optimum_to,
                                    finish_late_to=@finish_late_to, priority=@priority,
                                    description=@description, relevance=@relevance, 
                                    costs=@costs, section=@section, comment=@comment, 
                                    url=@url, private=@private, overarching_measure=@overarching_measure,
                                    desired_year_from=@desired_year_from,
                                    desired_year_to=@desired_year_to, geom=@geom";

                        updateComm.Parameters.AddWithValue("name", roadWorkNeedFeature.properties.name);
                        if (roadWorkNeedFeature.properties.orderer.uuid != "")
                        {
                            updateComm.Parameters.AddWithValue("orderer", new Guid(roadWorkNeedFeature.properties.orderer.uuid));
                        }
                        else
                        {
                            updateComm.Parameters.AddWithValue("orderer", DBNull.Value);
                        }
                        roadWorkNeedFeature.properties.lastModified = DateTime.Now;
                        updateComm.Parameters.AddWithValue("last_modified", roadWorkNeedFeature.properties.lastModified);
                        updateComm.Parameters.AddWithValue("finish_early_to", roadWorkNeedFeature.properties.finishEarlyTo);
                        updateComm.Parameters.AddWithValue("finish_optimum_to", roadWorkNeedFeature.properties.finishOptimumTo);
                        updateComm.Parameters.AddWithValue("finish_late_to", roadWorkNeedFeature.properties.finishLateTo);
                        updateComm.Parameters.AddWithValue("priority", roadWorkNeedFeature.properties.priority.code);
                        updateComm.Parameters.AddWithValue("description", roadWorkNeedFeature.properties.description);
                        updateComm.Parameters.AddWithValue("relevance", roadWorkNeedFeature.properties.relevance);
                        updateComm.Parameters.AddWithValue("costs", roadWorkNeedFeature.properties.costs != 0 ? roadWorkNeedFeature.properties.costs : DBNull.Value);
                        updateComm.Parameters.AddWithValue("section", roadWorkNeedFeature.properties.section);
                        updateComm.Parameters.AddWithValue("comment", roadWorkNeedFeature.properties.comment);
                        updateComm.Parameters.AddWithValue("url", roadWorkNeedFeature.properties.url);
                        updateComm.Parameters.AddWithValue("private", roadWorkNeedFeature.properties.isPrivate);
                        updateComm.Parameters.AddWithValue("overarching_measure", roadWorkNeedFeature.properties.overarchingMeasure);
                        if (roadWorkNeedFeature.properties.desiredYearFrom != null)
                            updateComm.Parameters.AddWithValue("desired_year_from", roadWorkNeedFeature.properties.desiredYearFrom);
                        else
                            updateComm.Parameters.AddWithValue("desired_year_from", DBNull.Value);
                        if (roadWorkNeedFeature.properties.desiredYearTo != null)
                            updateComm.Parameters.AddWithValue("desired_year_to", roadWorkNeedFeature.properties.desiredYearTo);
                        else
                            updateComm.Parameters.AddWithValue("desired_year_to", DBNull.Value);
                        updateComm.Parameters.AddWithValue("geom", roadWorkNeedPoly);

                        string activityRelationType = "";
                        if (roadWorkNeedFeature.properties.activityRelationType != null)
                        {
                            activityRelationType = roadWorkNeedFeature.properties.activityRelationType.Trim();
                        }

                        if (activityRelationType == "assignedneed")
                        {
                            updateComm.CommandText += ", status=(SELECT status FROM wtb_ssp_roadworkactivities WHERE uuid = @uuid_roadwork_activity)";
                            updateComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(roadWorkNeedFeature.properties.roadWorkActivityUuid));
                        }
                        else
                        {
                            updateComm.CommandText += ", status='requirement'";
                        }

                        updateComm.CommandText += " WHERE uuid=@uuid";
                        updateComm.Parameters.AddWithValue("uuid", new Guid(roadWorkNeedFeature.properties.uuid));

                        updateComm.ExecuteNonQuery();

                        if (roadWorkNeedFeature.properties.roadWorkActivityUuid != "")
                        {
                            NpgsqlCommand deleteComm = pgConn.CreateCommand();
                            deleteComm.CommandText = @"DELETE FROM ""wtb_ssp_activities_to_needs""
                                    WHERE uuid_roadwork_need=@uuid_roadwork_need
                                        AND uuid_roadwork_activity=@uuid_roadwork_activity";
                            deleteComm.Parameters.AddWithValue("uuid_roadwork_need", new Guid(roadWorkNeedFeature.properties.uuid));
                            deleteComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(roadWorkNeedFeature.properties.roadWorkActivityUuid));
                            deleteComm.ExecuteNonQuery();

                            deleteComm.CommandText = @"DELETE FROM ""wtb_ssp_activities_to_needs""
                                    WHERE uuid_roadwork_need=@uuid_roadwork_need
                                        AND activityrelationtype='assignedneed'";
                            deleteComm.ExecuteNonQuery();

                            if (activityRelationType != "")
                            {
                                NpgsqlCommand insertComm = pgConn.CreateCommand();
                                insertComm.CommandText = @"INSERT INTO ""wtb_ssp_activities_to_needs""
                                        (uuid, uuid_roadwork_need, uuid_roadwork_activity, activityrelationtype)
                                        VALUES(@uuid, @uuid_roadwork_need, @uuid_roadwork_activity, @activityrelationtype)";
                                insertComm.Parameters.AddWithValue("uuid", Guid.NewGuid());
                                insertComm.Parameters.AddWithValue("uuid_roadwork_need", new Guid(roadWorkNeedFeature.properties.uuid));
                                insertComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(roadWorkNeedFeature.properties.roadWorkActivityUuid));
                                insertComm.Parameters.AddWithValue("activityrelationtype", activityRelationType);
                                insertComm.ExecuteNonQuery();

                                NpgsqlCommand insertHistoryComm = pgConn.CreateCommand();
                                insertHistoryComm.CommandText = @"INSERT INTO ""wtb_ssp_activities_history""
                                    (uuid, uuid_roadwork_activity, changedate, who, what)
                                    VALUES
                                    (@uuid, @uuid_roadwork_activity, @changedate, @who, @what)";

                                insertHistoryComm.Parameters.AddWithValue("uuid", Guid.NewGuid());
                                insertHistoryComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(roadWorkNeedFeature.properties.roadWorkActivityUuid));
                                insertHistoryComm.Parameters.AddWithValue("changedate", DateTime.Now);
                                insertHistoryComm.Parameters.AddWithValue("who", userFromDb.firstName + " " + userFromDb.lastName);
                                string whatText = "Das Baubedürfnis '" + roadWorkNeedFeature.properties.name +
                                                    "' wurde neu zugewiesen. Die neue Zuweisung ist: ";
                                if (activityRelationType == "assignedneed")
                                {
                                    whatText += "Zugewiesenes Bedürfnis";
                                }
                                else if (activityRelationType == "registeredneed")
                                {
                                    whatText += "Angemeldetes Bedürfnis";
                                }
                                insertHistoryComm.Parameters.AddWithValue("what", whatText);

                                insertHistoryComm.ExecuteNonQuery();
                            }
                        }
                        updateTransAction.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                roadWorkNeedFeature = new RoadWorkNeedFeature();
                roadWorkNeedFeature.errorMessage = "SSP-3";
                return Ok(roadWorkNeedFeature);
            }

            return Ok(roadWorkNeedFeature);
        }

        // DELETE /roadworkneed?uuid=...&releaseonly=true
        [HttpDelete]
        [Authorize(Roles = "orderer,administrator")]
        public ActionResult<ErrorMessage> DeleteNeed(string uuid, bool releaseOnly = false)
        {
            ErrorMessage errorResult = new ErrorMessage();

            if (uuid == null)
            {
                _logger.LogWarning("No uuid provided by user in delete roadwork need process. " +
                            "Thus process is canceled, no roadwork need is deleted.");
                errorResult.errorMessage = "SSP-15";
                return Ok(errorResult);
            }

            uuid = uuid.ToLower().Trim();

            if (uuid == "")
            {
                _logger.LogWarning("No uuid provided by user in delete roadwork need process. " +
                            "Thus process is canceled, no roadwork need is deleted.");
                errorResult.errorMessage = "SSP-15";
                return Ok(errorResult);
            }

            User userFromDb = LoginController.getAuthorizedUserFromDb(this.User, false);

            try
            {
                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {
                    pgConn.Open();

                    if (!User.IsInRole("administrator"))
                    {

                        NpgsqlCommand selectOrdererOfNeedComm = pgConn.CreateCommand();
                        selectOrdererOfNeedComm.CommandText = @"SELECT u.e_mail
                                    FROM ""wtb_ssp_roadworkneeds"" r
                                    LEFT JOIN ""wtb_ssp_users"" u ON r.orderer = u.uuid
                                    WHERE r.uuid=@uuid";
                        selectOrdererOfNeedComm.Parameters.AddWithValue("uuid", new Guid(uuid));

                        string eMailOfOrderer = "";

                        using (NpgsqlDataReader reader = selectOrdererOfNeedComm.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                eMailOfOrderer = reader.IsDBNull(0) ? "" : reader.GetString(0);
                            }
                        }

                        string mailOfLoggedInUser = User.FindFirstValue(ClaimTypes.Email);

                        if (mailOfLoggedInUser != eMailOfOrderer)
                        {
                            _logger.LogWarning("User " + mailOfLoggedInUser + " has no right to delete " +
                                "roadwork need " + uuid + " but tried " +
                                "to delete it.");
                            errorResult.errorMessage = "SSP-14";
                            return Ok(errorResult);
                        }

                    }

                    NpgsqlCommand selectCommand = pgConn.CreateCommand();
                    selectCommand.CommandText = @"SELECT uuid_roadwork_activity
                                FROM ""wtb_ssp_activities_to_needs""
                                WHERE uuid_roadwork_need=@uuid";
                    selectCommand.Parameters.AddWithValue("uuid", new Guid(uuid));

                    string affectedActivityUuid = "";

                    using (NpgsqlDataReader reader = selectCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            affectedActivityUuid = reader.IsDBNull(0) ? "" : reader.GetGuid(0).ToString();
                        }
                    }

                    if (affectedActivityUuid != String.Empty)
                    {
                        NpgsqlCommand selectCountAssignedNeedsComm = pgConn.CreateCommand();
                        selectCountAssignedNeedsComm.CommandText = @"SELECT count(*) FROM ""wtb_ssp_activities_to_needs""
                                                        WHERE uuid_roadwork_activity=@uuid_roadwork_activity
                                                        AND activityrelationtype='assignedneed'";
                        selectCountAssignedNeedsComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(affectedActivityUuid));

                        int assignedNeedsCount = 0;
                        using (NpgsqlDataReader reader = selectCountAssignedNeedsComm.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                assignedNeedsCount = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                            }
                        }

                        if (assignedNeedsCount <= 1)
                        {
                            _logger.LogWarning("The roadwork need cannot be deleted since it is the last need of the roadwork activity " + affectedActivityUuid);
                            errorResult.errorMessage = "SSP-29";
                            return Ok(errorResult);
                        }
                    }

                    NpgsqlCommand deleteRelationComm = pgConn.CreateCommand();
                    deleteRelationComm.CommandText = @"DELETE FROM ""wtb_ssp_activities_to_needs""
                                WHERE uuid_roadwork_need=@uuid";
                    deleteRelationComm.Parameters.AddWithValue("uuid", new Guid(uuid));

                    using NpgsqlTransaction trans = pgConn.BeginTransaction();

                    deleteRelationComm.ExecuteNonQuery();

                    NpgsqlCommand changeNeedComm = pgConn.CreateCommand();
                    if (releaseOnly)
                    {
                        changeNeedComm.CommandText = @"UPDATE ""wtb_ssp_roadworkneeds""
                                SET status='requirement'
                                WHERE uuid=@uuid";
                    }
                    else
                    {
                        changeNeedComm.CommandText = @"DELETE FROM ""wtb_ssp_roadworkneeds""
                                WHERE uuid=@uuid";
                    }
                    changeNeedComm.Parameters.AddWithValue("uuid", new Guid(uuid));
                    changeNeedComm.ExecuteNonQuery();

                    if (affectedActivityUuid != String.Empty)
                    {
                        NpgsqlCommand insertHistoryComm = pgConn.CreateCommand();
                        insertHistoryComm.CommandText = @"INSERT INTO ""wtb_ssp_activities_history""
                                    (uuid, uuid_roadwork_activity, changedate, who, what)
                                    VALUES
                                    (@uuid, @uuid_roadwork_activity, @changedate, @who, @what)";

                        insertHistoryComm.Parameters.AddWithValue("uuid", Guid.NewGuid());
                        insertHistoryComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(affectedActivityUuid));
                        insertHistoryComm.Parameters.AddWithValue("changedate", DateTime.Now);
                        insertHistoryComm.Parameters.AddWithValue("who", userFromDb.firstName + " " + userFromDb.lastName);
                        string whatText = "Das Baubedürfnis '" + uuid +
                                            "' wurde von dieser Massnahme entfernt.";
                        insertHistoryComm.Parameters.AddWithValue("what", whatText);

                        insertHistoryComm.ExecuteNonQuery();
                    }

                    trans.Commit();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                errorResult.errorMessage = "SSP-3";
                return Ok(errorResult);
            }

            return Ok();

        }

    }
}