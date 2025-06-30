using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using roadwork_portal_service.Configuration;
using roadwork_portal_service.Model;

namespace roadwork_portal_service.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ConsultationController : ControllerBase
    {
        private readonly ILogger<ConsultationController> _logger;
        private IConfiguration Configuration { get; }

        public ConsultationController(ILogger<ConsultationController> logger,
                        IConfiguration configuration)
        {
            _logger = logger;
            this.Configuration = configuration;
        }

        // GET consultation/?roadworkactivityuuid=...
        [HttpGet]
        [Authorize(Roles = "orderer,trefficmanager,territorymanager,administrator")]
        public async Task<IEnumerable<ConsultationInput>> GetConsultations(string roadworkActivityUuid = "")
        {
            List<ConsultationInput> consultationInputs = new List<ConsultationInput>();

            roadworkActivityUuid = roadworkActivityUuid.Trim().ToLower();

            if (String.Empty == roadworkActivityUuid)
            {
                _logger.LogWarning("No roadwork activity UUID was given in get consultations method.");
                ConsultationInput consultationInput = new ConsultationInput();
                consultationInput.errorMessage = "SSP-15";
                consultationInputs.Add(consultationInput);
                return consultationInputs;
            }

            User userFromDb = LoginController.getAuthorizedUserFromDb(this.User, false);

            // get data of current user from database:
            using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
            {
                pgConn.Open();

                NpgsqlCommand selectConsultationComm = pgConn.CreateCommand();
                selectConsultationComm.CommandText = @"SELECT c.uuid, c.last_edit, c.decline,
                                            u.uuid as user_uuid, 
                                            u.e_mail, u.last_name, u.first_name, c.orderer_feedback,
                                            c.manager_feedback, c.valuation, c.feedback_phase,
                                            c.feedback_given, c.orderer_feedback_text,
                                            org.uuid, org.name, org.abbreviation, org.is_civil_eng,
                                            ue.e_mail as last_edit_by, c.uuid_roadwork_need, n.name as roadworkname
                                        FROM ""wtb_ssp_activity_consult"" c
                                        LEFT JOIN ""wtb_ssp_users"" u ON c.input_by = u.uuid
                                        LEFT JOIN ""wtb_ssp_users"" ue ON c.last_edit_by = ue.uuid
                                        LEFT JOIN ""wtb_ssp_organisationalunits"" org ON u.org_unit = org.uuid
                                        LEFT JOIN ""wtb_ssp_roadworkneeds"" n ON c.uuid_roadwork_need = n.uuid                                      
                                        WHERE uuid_roadwork_activity = @uuid_roadwork_activity";
                selectConsultationComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(roadworkActivityUuid));

                if (userFromDb != null && userFromDb.mailAddress != null &&
                        userFromDb.mailAddress != "" && User.IsInRole("orderer"))
                {
                    selectConsultationComm.CommandText += " AND u.e_mail = @e_mail";
                    selectConsultationComm.Parameters.AddWithValue("e_mail", userFromDb.mailAddress);
                }

                using (NpgsqlDataReader activityConsultationReader = await selectConsultationComm.ExecuteReaderAsync())
                {
                    while (activityConsultationReader.Read())
                    {
                        ConsultationInput activityConsulationInput = new ConsultationInput();
                        activityConsulationInput.uuid = activityConsultationReader.IsDBNull(0) ? "" : activityConsultationReader.GetGuid(0).ToString();
                        if (!activityConsultationReader.IsDBNull(1)) activityConsulationInput.lastEdit = activityConsultationReader.GetDateTime(1);
                        activityConsulationInput.decline = activityConsultationReader.IsDBNull(2) ? false : activityConsultationReader.GetBoolean(2);
                        User consultationUser = new User();
                        consultationUser.uuid = activityConsultationReader.IsDBNull(3) ? "" : activityConsultationReader.GetGuid(3).ToString();
                        consultationUser.mailAddress = activityConsultationReader.IsDBNull(4) ? "" : activityConsultationReader.GetString(4);
                        consultationUser.lastName = activityConsultationReader.IsDBNull(5) ? "" : activityConsultationReader.GetString(5);
                        consultationUser.firstName = activityConsultationReader.IsDBNull(6) ? "" : activityConsultationReader.GetString(6);
                        activityConsulationInput.inputBy = consultationUser;
                        activityConsulationInput.ordererFeedback = activityConsultationReader.IsDBNull(7) ? "" : activityConsultationReader.GetString(7);
                        activityConsulationInput.managerFeedback = activityConsultationReader.IsDBNull(8) ? "" : activityConsultationReader.GetString(8);
                        activityConsulationInput.valuation = activityConsultationReader.IsDBNull(9) ? 0 : activityConsultationReader.GetInt32(9);
                        activityConsulationInput.feedbackPhase = activityConsultationReader.IsDBNull(10) ? "" : activityConsultationReader.GetString(10);
                        activityConsulationInput.feedbackGiven = activityConsultationReader.IsDBNull(11) ? false : activityConsultationReader.GetBoolean(11);
                        activityConsulationInput.ordererFeedbackText = activityConsultationReader.IsDBNull(12) ? "" : activityConsultationReader.GetString(12);
                        OrganisationalUnit orgUnit = new OrganisationalUnit();
                        orgUnit.uuid = activityConsultationReader.IsDBNull(13) ? "" : activityConsultationReader.GetGuid(13).ToString();
                        orgUnit.name = activityConsultationReader.IsDBNull(14) ? "" : activityConsultationReader.GetString(14);
                        orgUnit.abbreviation = activityConsultationReader.IsDBNull(15) ? "" : activityConsultationReader.GetString(15);
                        orgUnit.isCivilEngineering = activityConsultationReader.IsDBNull(16) ? false : activityConsultationReader.GetBoolean(16);                        
                        activityConsulationInput.inputBy.organisationalUnit = orgUnit;
                        activityConsulationInput.lastEditBy = activityConsultationReader.IsDBNull(17) ? "" : activityConsultationReader.GetString(17);                        ;
                        activityConsulationInput.roadworkNeedUuid = activityConsultationReader.IsDBNull(18) ? "" : activityConsultationReader.GetGuid(18).ToString();
                        activityConsulationInput.roadworkNeedName= activityConsultationReader.IsDBNull(19) ? "" : activityConsultationReader.GetString(19);
                        consultationInputs.Add(activityConsulationInput);
                    }
                }
                pgConn.Close();
            }

            return consultationInputs;
        }

        // POST consultation/?roadworkactivityuuid=...
        [HttpPost]
        [Authorize(Roles = "territorymanager,orderer,administrator")]
        public ActionResult<ConsultationInput> AddConsultation(string roadworkActivityUuid,
                    [FromBody] ConsultationInput consultationInput, bool isDryRun = false)
        {
            try
            {
                User userFromDb = LoginController.getAuthorizedUserFromDb(this.User, false);

                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {
                    pgConn.Open();

                    using NpgsqlTransaction trans = pgConn.BeginTransaction();

                    NpgsqlCommand selectComm = pgConn.CreateCommand();
                    selectComm.CommandText = @"SELECT status FROM ""wtb_ssp_roadworkactivities""
                                        WHERE uuid=@uuid_roadwork_activity";
                    selectComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(roadworkActivityUuid));

                    string roadWorkActivityStatus = "";
                    using (NpgsqlDataReader reader = selectComm.ExecuteReader())
                    {
                        if (reader.Read())
                            roadWorkActivityStatus = reader.IsDBNull(0) ? "" :
                                        reader.GetString(0);
                    }

                    if ((roadWorkActivityStatus != "inconsult1" && roadWorkActivityStatus != "inconsult2" && roadWorkActivityStatus != "reporting") ||
                                roadWorkActivityStatus != consultationInput.feedbackPhase)
                    {
                        _logger.LogWarning("User tried to add consultation feedback though " +
                                "the consultation phase is closed");
                        consultationInput.errorMessage = "SSP-33";
                        return Ok(consultationInput);
                    }

                    consultationInput.uuid = Guid.NewGuid().ToString();
                    NpgsqlCommand insertComm = pgConn.CreateCommand();
                    insertComm.CommandText = @"INSERT INTO ""wtb_ssp_activity_consult""
                                    (uuid, uuid_roadwork_activity, last_edit, last_edit_by,
                                    input_by, orderer_feedback, manager_feedback, decline,
                                    valuation, feedback_phase, feedback_given, orderer_feedback_text)
                                    VALUES (@uuid, @uuid_roadwork_activity, @last_edit, @last_edit_by,
                                    @input_by, @orderer_feedback, @manager_feedback, @decline,
                                    @valuation, @feedback_phase, @feedback_given, @orderer_feedback_text)";
                    insertComm.Parameters.AddWithValue("uuid", new Guid(consultationInput.uuid));
                    insertComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(roadworkActivityUuid));
                    insertComm.Parameters.AddWithValue("last_edit", DateTime.Now);
                    insertComm.Parameters.AddWithValue("last_edit_by", new Guid(userFromDb.uuid));
                    insertComm.Parameters.AddWithValue("input_by", new Guid(consultationInput.inputBy.uuid));
                    insertComm.Parameters.AddWithValue("orderer_feedback", consultationInput.ordererFeedback);
                    if (consultationInput.decline)
                        consultationInput.ordererFeedbackText = "";
                    insertComm.Parameters.AddWithValue("orderer_feedback_text", consultationInput.ordererFeedbackText);

                    if (User.IsInRole("administrator") || User.IsInRole("territorymanager"))
                    {
                        if (consultationInput.managerFeedback == null)
                            consultationInput.managerFeedback = "";
                        consultationInput.managerFeedback = consultationInput.managerFeedback.Trim();
                        insertComm.Parameters.AddWithValue("manager_feedback", consultationInput.managerFeedback);
                    } else {
                        insertComm.Parameters.AddWithValue("manager_feedback", "");
                    }

                    insertComm.Parameters.AddWithValue("decline", consultationInput.decline);
                    insertComm.Parameters.AddWithValue("valuation", consultationInput.valuation);
                    insertComm.Parameters.AddWithValue("feedback_phase", consultationInput.feedbackPhase);

                    if (userFromDb.mailAddress == consultationInput.inputBy.mailAddress)
                        insertComm.Parameters.AddWithValue("feedback_given", true);
                    else
                        insertComm.Parameters.AddWithValue("feedback_given", consultationInput.feedbackGiven);

                    insertComm.ExecuteNonQuery();

                    ActivityHistoryItem activityHistoryItem = new ActivityHistoryItem();
                    activityHistoryItem.uuid = Guid.NewGuid().ToString();
                    activityHistoryItem.changeDate = DateTime.Now;
                    activityHistoryItem.who = userFromDb.firstName + " " + userFromDb.lastName;
                    activityHistoryItem.what = "Eingabe zur Vernehmlassung durch Bedarfsteller hinzugefügt.";
                    activityHistoryItem.userComment = "";

                    NpgsqlCommand insertHistoryComm = pgConn.CreateCommand();
                    insertHistoryComm.CommandText = @"INSERT INTO ""wtb_ssp_activities_history""
                            (uuid, uuid_roadwork_activity, changedate, who, what)
                            VALUES
                            (@uuid, @uuid_roadwork_activity, @changedate, @who, @what)";

                    insertHistoryComm.Parameters.AddWithValue("uuid", new Guid(activityHistoryItem.uuid));
                    insertHistoryComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(roadworkActivityUuid));
                    insertHistoryComm.Parameters.AddWithValue("changedate", activityHistoryItem.changeDate);
                    insertHistoryComm.Parameters.AddWithValue("who", activityHistoryItem.who);
                    insertHistoryComm.Parameters.AddWithValue("what", activityHistoryItem.what);

                    insertHistoryComm.ExecuteNonQuery();

                    trans.Commit();
                }

                return Ok(consultationInput);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                consultationInput.errorMessage = "SSP-3";
                return Ok(consultationInput);
            }

        }

        // PUT consultation/?roadworkactivityuuid=...
        [HttpPut]
        [Authorize(Roles = "orderer,territorymanager,administrator")]
        public ActionResult<ConsultationInput> UpdateConsultation(
                        string roadworkActivityUuid,
                        [FromBody] ConsultationInput consultationInput)
        {

            try
            {

                if (consultationInput == null)
                {
                    _logger.LogWarning("No consultation data received.");
                    ConsultationInput errorObj = new ConsultationInput();
                    errorObj.errorMessage = "SSP-15";
                    return Ok(errorObj);
                }

                User userFromDb = LoginController.getAuthorizedUserFromDb(this.User, false);

                if (userFromDb == null || userFromDb.uuid == null ||
                        (User.IsInRole("orderer") && userFromDb.mailAddress != consultationInput.inputBy.mailAddress))
                {
                    _logger.LogWarning("Unauthorized access.");
                    return Unauthorized();
                }

                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {

                    pgConn.Open();

                    using NpgsqlTransaction trans = pgConn.BeginTransaction();

                    NpgsqlCommand updateComm = pgConn.CreateCommand();
                    updateComm.CommandText = @"UPDATE ""wtb_ssp_activity_consult""
                                    SET last_edit=@last_edit, last_edit_by=@last_edit_by, orderer_feedback=@orderer_feedback,
                                    decline=@decline, valuation=@valuation, orderer_feedback_text=@orderer_feedback_text,
                                    feedback_phase=@feedback_phase, feedback_given=@feedback_given";

                    if (User.IsInRole("administrator") || User.IsInRole("territorymanager"))
                        updateComm.CommandText += ", manager_feedback=@manager_feedback";

                    updateComm.CommandText += " WHERE uuid=@uuid";

                    updateComm.Parameters.AddWithValue("uuid", new Guid(consultationInput.uuid));

                    consultationInput.lastEdit = DateTime.Now;
                    updateComm.Parameters.AddWithValue("last_edit", consultationInput.lastEdit);
                    updateComm.Parameters.AddWithValue("last_edit_by", new Guid(userFromDb.uuid));
                    updateComm.Parameters.AddWithValue("orderer_feedback", consultationInput.ordererFeedback);
                    if (consultationInput.decline)
                        consultationInput.ordererFeedbackText = "";
                    updateComm.Parameters.AddWithValue("orderer_feedback_text", consultationInput.ordererFeedbackText);

                    if (User.IsInRole("administrator") || User.IsInRole("territorymanager"))
                    {
                        if (consultationInput.managerFeedback == null)
                            consultationInput.managerFeedback = "";
                        consultationInput.managerFeedback = consultationInput.managerFeedback.Trim();
                        updateComm.Parameters.AddWithValue("manager_feedback", consultationInput.managerFeedback);
                    }
                    updateComm.Parameters.AddWithValue("decline", consultationInput.decline);
                    updateComm.Parameters.AddWithValue("valuation", consultationInput.valuation);
                    updateComm.Parameters.AddWithValue("feedback_phase", consultationInput.feedbackPhase);

                    if (userFromDb.mailAddress == consultationInput.inputBy.mailAddress)
                        updateComm.Parameters.AddWithValue("feedback_given", true);
                    else
                        updateComm.Parameters.AddWithValue("feedback_given", consultationInput.feedbackGiven);

                    updateComm.ExecuteNonQuery();

                    ActivityHistoryItem activityHistoryItem = new ActivityHistoryItem();
                    activityHistoryItem.uuid = Guid.NewGuid().ToString();
                    activityHistoryItem.changeDate = DateTime.Now;
                    activityHistoryItem.who = userFromDb.firstName + " " + userFromDb.lastName;
                    activityHistoryItem.what = "Eingabe zur Vernehmlassung durch Bedarfsteller aktualisiert.";
                    activityHistoryItem.userComment = "";

                    NpgsqlCommand insertHistoryComm = pgConn.CreateCommand();
                    insertHistoryComm.CommandText = @"INSERT INTO ""wtb_ssp_activities_history""
                            (uuid, uuid_roadwork_activity, changedate, who, what)
                            VALUES
                            (@uuid, @uuid_roadwork_activity, @changedate, @who, @what)";

                    insertHistoryComm.Parameters.AddWithValue("uuid", new Guid(activityHistoryItem.uuid));
                    insertHistoryComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(roadworkActivityUuid));
                    insertHistoryComm.Parameters.AddWithValue("changedate", activityHistoryItem.changeDate);
                    insertHistoryComm.Parameters.AddWithValue("who", activityHistoryItem.who);
                    insertHistoryComm.Parameters.AddWithValue("what", activityHistoryItem.what);

                    insertHistoryComm.ExecuteNonQuery();

                    trans.Commit();

                    pgConn.Close();

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                consultationInput = new ConsultationInput();
                consultationInput.errorMessage = "SSP-3";
                return Ok(consultationInput);
            }

            return Ok(consultationInput);
        }



        // DELETE consultation/?roadworkactivityuuid=...
        [HttpDelete]
        [Authorize(Roles = "territorymanager,orderer,administrator")]          
        public ActionResult<ErrorMessage> DeleteConsultation(string roadworkActivityUuid, string inputByUuid)
        {
            ErrorMessage errorResult = new ErrorMessage();

            try
            {

                if (roadworkActivityUuid == null || inputByUuid == null)
                {
                    _logger.LogWarning("No uuid provided by user in delete consultation feature process. " +
                                "Thus process is canceled, no consultation feature is deleted.");
                    errorResult.errorMessage = "SSP-15";
                    return Ok(errorResult);
                }

                roadworkActivityUuid = roadworkActivityUuid.ToLower().Trim();
                inputByUuid = inputByUuid.ToLower().Trim();

                if (roadworkActivityUuid == "" || inputByUuid == "")
                {
                    _logger.LogWarning("No uuid provided by user in delete consultation feature process. " +
                                "Thus process is canceled, no consultation feature is deleted.");
                    errorResult.errorMessage = "SSP-15";
                    return Ok(errorResult);
                }

                int noAffectedRows = 0;
                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {
                    pgConn.Open();

                    NpgsqlCommand deleteComm = pgConn.CreateCommand();
                    deleteComm = pgConn.CreateCommand();
                    deleteComm.CommandText = @"DELETE FROM ""wtb_ssp_activity_consult""
                                WHERE input_by=@uuid_input_by AND uuid_roadwork_activity=@uuid_roadwork_activity";
                    deleteComm.Parameters.AddWithValue("uuid_input_by", new Guid(inputByUuid));
                    deleteComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(roadworkActivityUuid));
                    noAffectedRows = deleteComm.ExecuteNonQuery();

                    pgConn.Close();
                }

                if (noAffectedRows == 1)
                {
                    return Ok();
                }

                if (noAffectedRows == 0)
                {
                    _logger.LogWarning("Es wurde keine Konsultationsfunktion zum Löschen gefunden");
                    errorResult.errorMessage = "SSP-41";
                    return Ok(errorResult);
                }

                _logger.LogError("Unknown error.");
                errorResult.errorMessage = "SSP-3";
                return Ok(errorResult);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                errorResult.errorMessage = "SSP-3";
                return Ok(errorResult);
            }

        }

    }
    
}