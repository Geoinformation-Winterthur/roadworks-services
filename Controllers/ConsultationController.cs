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

              /*   if (userFromDb != null && userFromDb.mailAddress != null &&
                        userFromDb.mailAddress != "" && User.IsInRole("orderer"))
                {
                    selectConsultationComm.CommandText += " AND u.e_mail = @e_mail";
                    selectConsultationComm.Parameters.AddWithValue("e_mail", userFromDb.mailAddress);
                } */

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

        /// <summary>
        /// Add a single consultation input.
        /// E.g. POST consultation/?roadworkactivityuuid=...
        /// </summary>
        /// <param name="roadworkActivityUuid"></param>
        /// <param name="consultationInput"></param>
        /// <param name="isDryRun"></param>
        /// <returns></returns>
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

                    string roadWorkActivityStatus = GetRoadWorkActivityStatus(roadworkActivityUuid, pgConn);
                    if ((roadWorkActivityStatus != "inconsult1" && roadWorkActivityStatus != "inconsult2" && roadWorkActivityStatus != "reporting") ||
                                roadWorkActivityStatus != consultationInput.feedbackPhase)
                    {
                        _logger.LogWarning("User tried to add consultation feedback though the consultation phase is closed");
                        consultationInput.errorMessage = "SSP-33";

                        return Conflict(consultationInput);
                    }

                    InsertConsultation(roadworkActivityUuid, consultationInput, userFromDb, pgConn);

                    trans.Commit();
                }

                return Ok(consultationInput);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                consultationInput.errorMessage = "SSP-33";

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    consultationInput);
            }
        }

        /// <summary>
        /// Add many consultation inputs at once. Must all be in the same feedback phase.
        /// E.g. POST consultations/?roadworkactivityuuid=...
        /// </summary>
        /// <param name="roadworkActivityUuid"></param>
        /// <param name="consultationInputs"></param>
        /// <param name="isDryRun"></param>
        /// <returns></returns>
        [HttpPost("/Consultations")]
        [Authorize(Roles = "territorymanager,orderer,administrator")]
        public ActionResult<AffectedRowsResult> AddConsultations(string roadworkActivityUuid,
                    [FromBody] ConsultationInput[] consultationInputs, bool isDryRun = false)
        {
            // check if any consultation in the list
            if (consultationInputs == null || consultationInputs.Length == 0)
                return Ok(new AffectedRowsResult { affectedRows = 0 });

            try
            {
                int insertedConsultationsCount = 0;
                User userFromDb = LoginController.getAuthorizedUserFromDb(this.User, false);

                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {
                    pgConn.Open();

                    using NpgsqlTransaction trans = pgConn.BeginTransaction();

                    string roadWorkActivityStatus = GetRoadWorkActivityStatus(roadworkActivityUuid, pgConn);
                    if ((roadWorkActivityStatus != "inconsult1" && roadWorkActivityStatus != "inconsult2" && roadWorkActivityStatus != "reporting") ||
                        consultationInputs.Any(elem => elem.feedbackPhase != roadWorkActivityStatus))
                    {
                        _logger.LogWarning("User tried to add consultation feedback though the consultation phase is closed");

                        return Conflict(new AffectedRowsResult { affectedRows = 0, errorMessage = "SSP-33" });
                    }

                    insertedConsultationsCount = InsertConsultations(roadworkActivityUuid, consultationInputs, userFromDb, pgConn);

                    trans.Commit();
                }

                return Ok(insertedConsultationsCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new AffectedRowsResult { affectedRows = 0,errorMessage = "SSP-3" });
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

                    if (userFromDb.mailAddress == consultationInput.inputBy.mailAddress
                        &&  consultationInput.ordererFeedback != null && consultationInput.ordererFeedback != "")                        
                        updateComm.Parameters.AddWithValue("feedback_given", true);
                    else
                        updateComm.Parameters.AddWithValue("feedback_given", consultationInput.feedbackGiven);

                    updateComm.ExecuteNonQuery();

                    WriteHistory(roadworkActivityUuid, "Eingabe zur Vernehmlassung durch Bedarfsteller aktualisiert.", userFromDb, pgConn);

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
        public ActionResult<ErrorMessage> DeleteConsultation(string roadworkActivityUuid, string inputByUuid, string feedbackPhase)
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
                                WHERE input_by=@uuid_input_by AND uuid_roadwork_activity=@uuid_roadwork_activity AND feedback_phase=@feedback_phase";
                    deleteComm.Parameters.AddWithValue("uuid_input_by", new Guid(inputByUuid));
                    deleteComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(roadworkActivityUuid));
                    deleteComm.Parameters.AddWithValue("feedback_phase", feedbackPhase);
                    noAffectedRows = deleteComm.ExecuteNonQuery();

                    pgConn.Close();
                }

                if (noAffectedRows == 1)
                {
                    // Attribute "errorMessage" (empty string for success) required for evaluation clientside.
                    // Therefore class ErrorMessage here because no other return value is required 
                    return Ok(new ErrorMessage());
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

        private ConsultationInput InsertConsultation(string roadworkActivityUuid, ConsultationInput consultationInput, User userFromDb, NpgsqlConnection pgConn)
        {
            NpgsqlCommand command = pgConn.CreateCommand();

            // add parameters for consultation input
            consultationInput.uuid = Guid.NewGuid().ToString();

            if (consultationInput.decline)
            {
                consultationInput.ordererFeedbackText = string.Empty;
            }

            if (userFromDb.mailAddress == consultationInput.inputBy?.mailAddress)
            {
                consultationInput.feedbackGiven = true;
            }

            if (User.IsInRole("administrator") || User.IsInRole("territorymanager"))
            {
                consultationInput.managerFeedback = consultationInput.managerFeedback?.Trim() ?? string.Empty;
            }
            else
            {
                consultationInput.managerFeedback = string.Empty;
            }

            // set parameters
            command.Parameters.AddWithValue($"uuid", new Guid(consultationInput.uuid));
            command.Parameters.AddWithValue($"uuid_roadwork_activity", new Guid(roadworkActivityUuid));
            command.Parameters.AddWithValue($"last_edit", DateTime.Now);
            command.Parameters.AddWithValue($"last_edit_by", new Guid(userFromDb.uuid));
            command.Parameters.AddWithValue($"input_by", new Guid(consultationInput.inputBy.uuid));
            command.Parameters.AddWithValue($"orderer_feedback", consultationInput.ordererFeedback);
            command.Parameters.AddWithValue($"orderer_feedback_text", consultationInput.ordererFeedbackText);
            command.Parameters.AddWithValue($"manager_feedback", consultationInput.managerFeedback);
            command.Parameters.AddWithValue($"decline", consultationInput.decline);
            command.Parameters.AddWithValue($"valuation", consultationInput.valuation);
            command.Parameters.AddWithValue($"feedback_phase", consultationInput.feedbackPhase);
            command.Parameters.AddWithValue($"feedback_given", consultationInput.feedbackGiven);

            // build the comand text
            command.CommandText = @"INSERT INTO ""wtb_ssp_activity_consult""
                                        (uuid, uuid_roadwork_activity, last_edit, last_edit_by,
                                        input_by, orderer_feedback, manager_feedback, decline,
                                        valuation, feedback_phase, feedback_given, orderer_feedback_text)
                                        VALUES (@uuid, @uuid_roadwork_activity, @last_edit, @last_edit_by,
                                        @input_by, @orderer_feedback, @manager_feedback, @decline,
                                        @valuation, @feedback_phase, @feedback_given, @orderer_feedback_text)";

            // insert the consultations into the db
            command.ExecuteNonQuery();
            WriteHistory(roadworkActivityUuid, "Eingabe zur Vernehmlassung durch Bedarfsteller hinzugefügt.", userFromDb, pgConn);

            return consultationInput;
        }

        private int InsertConsultations(string roadworkActivityUuid, ConsultationInput[] consultationInput, User userFromDb, NpgsqlConnection pgConn)
        {
            NpgsqlCommand command = pgConn.CreateCommand();
            var values = new List<string>();

            // loop and add parameters for all consultation inputs
            for (var i = 0; i < consultationInput.Length; i++)
            {
                values.Add($@"(@uuid{i}, @uuid_roadwork_activity{i}, @last_edit{i}, @last_edit_by{i},
                    @input_by{i}, @orderer_feedback{i}, @manager_feedback{i}, @decline{i},
                    @valuation{i}, @feedback_phase{i}, @feedback_given{i}, @orderer_feedback_text{i})");

                // update conditional values
                consultationInput[i].uuid = Guid.NewGuid().ToString();

                if (consultationInput[i].decline)
                {
                    consultationInput[i].ordererFeedbackText = string.Empty;
                }

                if (userFromDb.mailAddress == consultationInput[i].inputBy?.mailAddress)
                {
                    consultationInput[i].feedbackGiven = true;
                }

                if (User.IsInRole("administrator") || User.IsInRole("territorymanager"))
                {
                    consultationInput[i].managerFeedback = consultationInput[i].managerFeedback?.Trim() ?? string.Empty;
                }
                else
                {
                    consultationInput[i].managerFeedback = string.Empty;
                }

                // set parameters
                command.Parameters.AddWithValue($"uuid{i}", new Guid(consultationInput[i].uuid));
                command.Parameters.AddWithValue($"uuid_roadwork_activity{i}", new Guid(roadworkActivityUuid));
                command.Parameters.AddWithValue($"last_edit{i}", DateTime.Now);
                command.Parameters.AddWithValue($"last_edit_by{i}", new Guid(userFromDb.uuid));
                command.Parameters.AddWithValue($"input_by{i}", new Guid(consultationInput[i].inputBy.uuid));
                command.Parameters.AddWithValue($"orderer_feedback{i}", consultationInput[i].ordererFeedback);
                command.Parameters.AddWithValue($"orderer_feedback_text{i}", consultationInput[i].ordererFeedbackText);
                command.Parameters.AddWithValue($"manager_feedback{i}", consultationInput[i].managerFeedback);
                command.Parameters.AddWithValue($"decline{i}", consultationInput[i].decline);
                command.Parameters.AddWithValue($"valuation{i}", consultationInput[i].valuation);
                command.Parameters.AddWithValue($"feedback_phase{i}", consultationInput[i].feedbackPhase);
                command.Parameters.AddWithValue($"feedback_given{i}", consultationInput[i].feedbackGiven);
            }

            // build the comand text
            command.CommandText = $@"INSERT INTO ""wtb_ssp_activity_consult""
                                    (uuid, uuid_roadwork_activity, last_edit, last_edit_by,
                                    input_by, orderer_feedback, manager_feedback, decline,
                                    valuation, feedback_phase, feedback_given, orderer_feedback_text)
                                    VALUES {string.Join(", ", values)}";

            // insert the consultations into the db
            int resultCount = command.ExecuteNonQuery();
            WriteHistory(roadworkActivityUuid, "Eingaben zur Vernehmlassung durch Bedarfsteller hinzugefügt.", userFromDb, pgConn);

            return resultCount;
        }

        private static string GetRoadWorkActivityStatus(string roadworkActivityUuid, NpgsqlConnection pgConn)
        {
            NpgsqlCommand selectComm = pgConn.CreateCommand();
            selectComm.CommandText = @"SELECT status FROM ""wtb_ssp_roadworkactivities""
                WHERE uuid=@uuid_roadwork_activity";

            selectComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(roadworkActivityUuid));

            using (NpgsqlDataReader reader = selectComm.ExecuteReader())
            {
                if (reader.Read())
                {
                    return reader.IsDBNull(0) ? "" : reader.GetString(0);
                }
            }

            return string.Empty;
        }

        private static void WriteHistory(string roadworkActivityUuid, string what, User userFromDb, NpgsqlConnection pgConn)
        {
            ActivityHistoryItem activityHistoryItem = new ActivityHistoryItem
            {
                uuid = Guid.NewGuid().ToString(),
                changeDate = DateTime.Now,
                who = userFromDb.firstName + " " + userFromDb.lastName,
                what = what,
                userComment = ""
            };

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
        }

    }
    
}