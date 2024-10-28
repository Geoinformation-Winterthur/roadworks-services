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
                            r.finish_optimum_to, r.finish_late_to, p.code, r.status as statusname,
                            r.description, 
                            r.created, r.last_modified, an.uuid_roadwork_activity, u.e_mail,
                            r.relevance, an.activityrelationtype, r.costs,
                            r.note_of_area_man, r.area_man_note_date,
                            n.first_name as area_manager_first_name, n.last_name as area_manager_last_name,
                            r.private, r.section, r.comment, r.url, o.is_civil_eng, o.abbreviation,
                            r.overarching_measure, r.desired_year_from, r.desired_year_to,
                            u.e_mail, r.pdf_document IS NOT NULL has_pdf,
                            r.has_sponge_city_meas, r.is_sponge_1_1, r.is_sponge_1_2,
                            r.is_sponge_1_3, r.is_sponge_1_4, r.is_sponge_1_5, r.is_sponge_1_6,
                            r.is_sponge_1_7, r.is_sponge_1_8, r.is_sponge_2_1, r.is_sponge_2_2,
                            r.is_sponge_2_3, r.is_sponge_2_4, r.is_sponge_2_5, r.is_sponge_2_6,
                            r.is_sponge_2_7, r.is_sponge_3_1, r.is_sponge_3_2, r.is_sponge_3_3,
                            r.is_sponge_4_1, r.is_sponge_4_2, r.is_sponge_5_1, work_title,
                            project_type, costs_comment, r.geom
                        FROM ""wtb_ssp_roadworkneeds"" r
                        LEFT JOIN ""wtb_ssp_activities_to_needs"" an ON an.uuid_roadwork_need = r.uuid
                        LEFT JOIN ""wtb_ssp_users"" u ON r.orderer = u.uuid
                        LEFT JOIN ""wtb_ssp_organisationalunits"" o ON u.org_unit = o.uuid
                        LEFT JOIN ""wtb_ssp_priorities"" p ON r.priority = p.code
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
                            needFeatureFromDb.properties.status = reader.IsDBNull(10) ? "" : reader.GetString(10);
                            needFeatureFromDb.properties.description = reader.IsDBNull(11) ? "" : reader.GetString(11);

                            needFeatureFromDb.properties.created = reader.IsDBNull(12) ? DateTime.MinValue : reader.GetDateTime(12);
                            needFeatureFromDb.properties.lastModified = reader.IsDBNull(13) ? DateTime.MinValue : reader.GetDateTime(13);
                            needFeatureFromDb.properties.roadWorkActivityUuid = reader.IsDBNull(14) ? "" : reader.GetGuid(14).ToString();

                            string ordererMailAddress = reader.IsDBNull(15) ? "" : reader.GetString(15);
                            string mailOfLoggedInUser = User.FindFirstValue(ClaimTypes.Email);

                            needFeatureFromDb.properties.activityRelationType = reader.IsDBNull(17) ? "" : reader.GetString(17);
                            if (!reader.IsDBNull(18)) needFeatureFromDb.properties.costs = reader.GetInt32(18);
                            needFeatureFromDb.properties.noteOfAreaManager = reader.IsDBNull(19) ? "" : reader.GetString(19);
                            needFeatureFromDb.properties.areaManagerNoteDate = reader.IsDBNull(20) ? DateTime.MinValue : reader.GetDateTime(20);

                            User areaManagerOfNote = new User();
                            areaManagerOfNote.firstName = reader.IsDBNull(21) ? "" : reader.GetString(21);
                            areaManagerOfNote.lastName = reader.IsDBNull(22) ? "" : reader.GetString(22);
                            needFeatureFromDb.properties.areaManagerOfNote = areaManagerOfNote;
                            needFeatureFromDb.properties.isPrivate = reader.IsDBNull(23) ? true : reader.GetBoolean(23);
                            needFeatureFromDb.properties.section = reader.IsDBNull(24) ? "" : reader.GetString(24);
                            needFeatureFromDb.properties.comment = reader.IsDBNull(25) ? "" : reader.GetString(25);
                            needFeatureFromDb.properties.url = reader.IsDBNull(26) ? "" : reader.GetString(26);
                            needFeatureFromDb.properties.orderer.organisationalUnit.isCivilEngineering = reader.IsDBNull(27) ? false : reader.GetBoolean(27);

                            orgUnit.abbreviation = reader.IsDBNull(28) ? "" : reader.GetString(28);

                            needFeatureFromDb.properties.overarchingMeasure = reader.IsDBNull(29) ? false : reader.GetBoolean(29);
                            needFeatureFromDb.properties.desiredYearFrom = reader.IsDBNull(30) ? null : reader.GetInt32(30);
                            needFeatureFromDb.properties.desiredYearTo = reader.IsDBNull(31) ? null : reader.GetInt32(31);

                            needFeatureFromDb.properties.orderer.mailAddress = reader.IsDBNull(32) ? "" : reader.GetString(32);
                            needFeatureFromDb.properties.hasPdfDocument = reader.IsDBNull(33) ? false : reader.GetBoolean(33);
                            needFeatureFromDb.properties.hasSpongeCityMeasures = reader.IsDBNull(34) ? false : reader.GetBoolean(34);

                            List<string> spongeCityMeasures = new List<string>();
                            if (!reader.IsDBNull(35) && reader.GetBoolean(35)) spongeCityMeasures.Add("1.1");
                            if (!reader.IsDBNull(36) && reader.GetBoolean(36)) spongeCityMeasures.Add("1.2");
                            if (!reader.IsDBNull(37) && reader.GetBoolean(37)) spongeCityMeasures.Add("1.3");
                            if (!reader.IsDBNull(38) && reader.GetBoolean(38)) spongeCityMeasures.Add("1.4");
                            if (!reader.IsDBNull(39) && reader.GetBoolean(39)) spongeCityMeasures.Add("1.5");
                            if (!reader.IsDBNull(40) && reader.GetBoolean(40)) spongeCityMeasures.Add("1.6");
                            if (!reader.IsDBNull(41) && reader.GetBoolean(41)) spongeCityMeasures.Add("1.7");
                            if (!reader.IsDBNull(42) && reader.GetBoolean(42)) spongeCityMeasures.Add("1.8");
                            if (!reader.IsDBNull(43) && reader.GetBoolean(43)) spongeCityMeasures.Add("2.1");
                            if (!reader.IsDBNull(44) && reader.GetBoolean(44)) spongeCityMeasures.Add("2.2");
                            if (!reader.IsDBNull(45) && reader.GetBoolean(45)) spongeCityMeasures.Add("2.3");
                            if (!reader.IsDBNull(46) && reader.GetBoolean(46)) spongeCityMeasures.Add("2.4");
                            if (!reader.IsDBNull(47) && reader.GetBoolean(47)) spongeCityMeasures.Add("2.5");
                            if (!reader.IsDBNull(48) && reader.GetBoolean(48)) spongeCityMeasures.Add("2.6");
                            if (!reader.IsDBNull(49) && reader.GetBoolean(49)) spongeCityMeasures.Add("2.7");
                            if (!reader.IsDBNull(50) && reader.GetBoolean(50)) spongeCityMeasures.Add("3.1");
                            if (!reader.IsDBNull(51) && reader.GetBoolean(51)) spongeCityMeasures.Add("3.2");
                            if (!reader.IsDBNull(52) && reader.GetBoolean(52)) spongeCityMeasures.Add("3.3");
                            if (!reader.IsDBNull(53) && reader.GetBoolean(53)) spongeCityMeasures.Add("4.1");
                            if (!reader.IsDBNull(54) && reader.GetBoolean(54)) spongeCityMeasures.Add("4.2");
                            if (!reader.IsDBNull(55) && reader.GetBoolean(55)) spongeCityMeasures.Add("5.1");

                            needFeatureFromDb.properties.spongeCityMeasures = spongeCityMeasures.ToArray();

                            if (!reader.IsDBNull(56)) needFeatureFromDb.properties.workTitle = reader.GetString(56);
                            if (!reader.IsDBNull(57)) needFeatureFromDb.properties.projectType = reader.GetString(57);
                            if (!reader.IsDBNull(58)) needFeatureFromDb.properties.costsComment = reader.GetString(58);

                            Polygon ntsPoly = reader.IsDBNull(59) ? Polygon.Empty : reader.GetValue(59) as Polygon;
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
        [Authorize(Roles = "orderer,territorymanager,administrator")]
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

                ConfigurationData configData = AppConfigController.getConfigurationFromDb(false);

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
                else if (roadWorkNeedFeature.errorMessage == "SSP-38")
                    _logger.LogWarning("If sponge city measure is activated then at least one sponge city measure must be provided.");
                else if (roadWorkNeedFeature.errorMessage == "SSP-40")
                    _logger.LogWarning("Roadwork need is from civil engineering but one or more required cost " +
                        "estrimation attribute values are missing.");


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
        [Authorize(Roles = "orderer,territorymanager,administrator")]
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

                if (roadWorkNeedFeature.properties.hasSpongeCityMeasures)
                {
                    if (roadWorkNeedFeature.properties.spongeCityMeasures == null ||
                        roadWorkNeedFeature.properties.spongeCityMeasures.Length == 0)
                    {
                        roadWorkNeedFeature.errorMessage = "SSP-38";
                        return roadWorkNeedFeature;
                    }

                    bool hasNonEmptyEntries = false;
                    for (int i = 0; i < roadWorkNeedFeature.properties.spongeCityMeasures.Length; i++)
                    {
                        if (roadWorkNeedFeature.properties.spongeCityMeasures[i] != null)
                            roadWorkNeedFeature.properties.spongeCityMeasures[i] =
                                roadWorkNeedFeature.properties.spongeCityMeasures[i].Trim();

                        if (roadWorkNeedFeature.properties.spongeCityMeasures[i] != String.Empty)
                            hasNonEmptyEntries = true;
                    }

                    if (!hasNonEmptyEntries)
                    {
                        roadWorkNeedFeature.errorMessage = "SSP-38";
                        return roadWorkNeedFeature;
                    }
                }

                if (roadWorkNeedFeature.properties.workTitle != null)
                    roadWorkNeedFeature.properties.workTitle = roadWorkNeedFeature.properties.workTitle.Trim().ToLower();

                if (roadWorkNeedFeature.properties.projectType != null)
                    roadWorkNeedFeature.properties.projectType = roadWorkNeedFeature.properties.projectType.Trim().ToLower();

                if (roadWorkNeedFeature.properties.orderer.organisationalUnit.isCivilEngineering)
                {
                    bool notValid = false;
                    if (roadWorkNeedFeature.properties.workTitle == null || roadWorkNeedFeature.properties.workTitle == "")
                        notValid = true;
                    if (roadWorkNeedFeature.properties.projectType == null || roadWorkNeedFeature.properties.projectType == "")
                        notValid = true;
                    if (roadWorkNeedFeature.properties.costs == null ||
                            roadWorkNeedFeature.properties.costs == 0)
                        notValid = true;

                    if (notValid)
                    {
                        _logger.LogWarning("Roadwork need is from civil engineering but one or more required cost "+
                                "estrimation attribute values are missing.");
                        roadWorkNeedFeature.errorMessage = "SSP-40";
                        return Ok(roadWorkNeedFeature);
                    }
                } else {
                    roadWorkNeedFeature.properties.workTitle = null;
                    roadWorkNeedFeature.properties.projectType = null;
                    roadWorkNeedFeature.properties.costs = null;
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

                ConfigurationData configData = AppConfigController.getConfigurationFromDb(false);
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

                    if (!User.IsInRole("administrator") && !User.IsInRole("territorymanager"))
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
                                    description=@description, 
                                    costs=@costs, section=@section, comment=@comment, 
                                    url=@url, private=@private, overarching_measure=@overarching_measure,
                                    desired_year_from=@desired_year_from,
                                    desired_year_to=@desired_year_to, has_sponge_city_meas=@has_sponge_city_meas,
                                    is_sponge_1_1=@is_sponge_1_1, is_sponge_1_2=@is_sponge_1_2,
                                    is_sponge_1_3=@is_sponge_1_3, is_sponge_1_4=@is_sponge_1_4,
                                    is_sponge_1_5=@is_sponge_1_5, is_sponge_1_6=@is_sponge_1_6,
                                    is_sponge_1_7=@is_sponge_1_7, is_sponge_1_8=@is_sponge_1_8,
                                    is_sponge_2_1=@is_sponge_2_1, is_sponge_2_2=@is_sponge_2_2,
                                    is_sponge_2_3=@is_sponge_2_3, is_sponge_2_4=@is_sponge_2_4,
                                    is_sponge_2_5=@is_sponge_2_5, is_sponge_2_6=@is_sponge_2_6,
                                    is_sponge_2_7=@is_sponge_2_7, is_sponge_3_1=@is_sponge_3_1,
                                    is_sponge_3_2=@is_sponge_3_2, is_sponge_3_3=@is_sponge_3_3,
                                    is_sponge_4_1=@is_sponge_4_1, is_sponge_4_2=@is_sponge_4_2,
                                    is_sponge_5_1=@is_sponge_5_1, work_title=@work_title,
                                    project_type=@project_type, costs_comment=@costs_comment, geom=@geom";

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
                        updateComm.Parameters.AddWithValue("costs", roadWorkNeedFeature.properties.costs != null ? roadWorkNeedFeature.properties.costs : DBNull.Value);
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

                        updateComm.Parameters.AddWithValue("has_sponge_city_meas", roadWorkNeedFeature.properties.hasSpongeCityMeasures);

                        updateComm.Parameters.AddWithValue("is_sponge_1_1", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("1.1"));
                        updateComm.Parameters.AddWithValue("is_sponge_1_2", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("1.2"));
                        updateComm.Parameters.AddWithValue("is_sponge_1_3", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("1.3"));
                        updateComm.Parameters.AddWithValue("is_sponge_1_4", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("1.4"));
                        updateComm.Parameters.AddWithValue("is_sponge_1_5", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("1.5"));
                        updateComm.Parameters.AddWithValue("is_sponge_1_6", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("1.6"));
                        updateComm.Parameters.AddWithValue("is_sponge_1_7", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("1.7"));
                        updateComm.Parameters.AddWithValue("is_sponge_1_8", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("1.8"));
                        updateComm.Parameters.AddWithValue("is_sponge_2_1", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("2.1"));
                        updateComm.Parameters.AddWithValue("is_sponge_2_2", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("2.2"));
                        updateComm.Parameters.AddWithValue("is_sponge_2_3", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("2.3"));
                        updateComm.Parameters.AddWithValue("is_sponge_2_4", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("2.4"));
                        updateComm.Parameters.AddWithValue("is_sponge_2_5", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("2.5"));
                        updateComm.Parameters.AddWithValue("is_sponge_2_6", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("2.6"));
                        updateComm.Parameters.AddWithValue("is_sponge_2_7", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("2.7"));
                        updateComm.Parameters.AddWithValue("is_sponge_3_1", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("3.1"));
                        updateComm.Parameters.AddWithValue("is_sponge_3_2", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("3.2"));
                        updateComm.Parameters.AddWithValue("is_sponge_3_3", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("3.3"));
                        updateComm.Parameters.AddWithValue("is_sponge_4_1", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("4.1"));
                        updateComm.Parameters.AddWithValue("is_sponge_4_2", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("4.2"));
                        updateComm.Parameters.AddWithValue("is_sponge_5_1", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("5.1"));

                        updateComm.Parameters.AddWithValue("work_title", roadWorkNeedFeature.properties.workTitle != null ? roadWorkNeedFeature.properties.workTitle : DBNull.Value);
                        updateComm.Parameters.AddWithValue("project_type", roadWorkNeedFeature.properties.projectType != null ? roadWorkNeedFeature.properties.projectType : DBNull.Value);
                        updateComm.Parameters.AddWithValue("costs_comment", roadWorkNeedFeature.properties.costsComment != null ? roadWorkNeedFeature.properties.costsComment : DBNull.Value);

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


                            NpgsqlCommand selectCommand = pgConn.CreateCommand();
                            selectCommand.CommandText = @"SELECT uuid_roadwork_activity
                                FROM ""wtb_ssp_activities_to_needs""
                                WHERE uuid_roadwork_need=@uuid
                                AND activityrelationtype='assignedneed'";
                            selectCommand.Parameters.AddWithValue("uuid", new Guid(roadWorkNeedFeature.properties.uuid));

                            string affectedActivityUuid = "";

                            using (NpgsqlDataReader reader = selectCommand.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    affectedActivityUuid = reader.IsDBNull(0) ? "" : reader.GetGuid(0).ToString();
                                }
                            }

                            int assignedNeedsCount = _countAssignedNeeds(affectedActivityUuid, pgConn);
                            if (assignedNeedsCount == 1)
                            {
                                _logger.LogWarning("The roadwork need cannot be deleted since it is the last need of the roadwork activity " + affectedActivityUuid);
                                roadWorkNeedFeature.errorMessage = "SSP-29";
                                return Ok(roadWorkNeedFeature);
                            }


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
        [Authorize(Roles = "orderer,territorymanager,administrator")]
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

                    if (!User.IsInRole("administrator") && !User.IsInRole("territorymanager"))
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
                                WHERE uuid_roadwork_need=@uuid
                                AND activityrelationtype='assignedneed'";
                    selectCommand.Parameters.AddWithValue("uuid", new Guid(uuid));

                    string affectedActivityUuid = "";

                    using (NpgsqlDataReader reader = selectCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            affectedActivityUuid = reader.IsDBNull(0) ? "" : reader.GetGuid(0).ToString();
                        }
                    }

                    int assignedNeedsCount = _countAssignedNeeds(affectedActivityUuid, pgConn);
                    if (assignedNeedsCount == 1)
                    {
                        _logger.LogWarning("The roadwork need cannot be deleted since it is the last need of the roadwork activity " + affectedActivityUuid);
                        errorResult.errorMessage = "SSP-29";
                        return Ok(errorResult);
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

        private int _countAssignedNeeds(string affectedActivityUuid, NpgsqlConnection pgConn)
        {
            int result = 0;
            if (affectedActivityUuid != String.Empty)
            {
                NpgsqlCommand selectCountAssignedNeedsComm = pgConn.CreateCommand();
                selectCountAssignedNeedsComm.CommandText = @"SELECT count(*) FROM ""wtb_ssp_activities_to_needs""
                                                        WHERE uuid_roadwork_activity=@uuid_roadwork_activity
                                                        AND activityrelationtype='assignedneed'";
                selectCountAssignedNeedsComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(affectedActivityUuid));

                using (NpgsqlDataReader reader = selectCountAssignedNeedsComm.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        result = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                    }
                }

            }
            return result;
        }

    }
}