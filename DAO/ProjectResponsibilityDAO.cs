using Npgsql;
using roadwork_portal_service.Configuration;
using roadwork_portal_service.Helper;
using roadwork_portal_service.Mappers;
using roadwork_portal_service.Model;

namespace roadwork_portal_service.DAO
{
    /// <summary>
    /// DAO for ActivityResponsibilityFeature.
    /// </summary>
    public class ActivityResponsibilityDAO
    {
        private string connectionString;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public ActivityResponsibilityDAO()
        {
            connectionString = AppConfig.connectionString;
        }

        /// <summary>
        /// Get a activity responsitility by uuid.
        /// </summary>
        /// <param name="uuid"></param>
        /// <returns></returns>
        public ActivityResponsibilityFeature GetByUuid(string uuid)
        {
            ActivityResponsibilityFeature activityResponsibilityFeatureFromDb = null;

            // get data of current user from database:
            using (NpgsqlConnection connection = new NpgsqlConnection(AppConfig.connectionString))
            {
                connection.Open();

                // Get the activity responsibility entry
                using (NpgsqlCommand command = GetByUuidCommand(uuid, connection))
                using (NpgsqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        activityResponsibilityFeatureFromDb = ActivityResponsibilityMapper.FromReader(reader);
                    }
                }
            }

            return activityResponsibilityFeatureFromDb;
        }

        /// <summary>
        /// Select command to retrieve the ActivityResponsibilityFeature (wtb_ssp_journal_entries) by uuid.
        /// </summary>
        /// <param name="uuid"></param>
        /// <param name="pgConn"></param>
        /// <returns></returns>
        internal static NpgsqlCommand GetByUuidCommand(string uuid, NpgsqlConnection pgConn)
        {
            NpgsqlCommand selectComm = pgConn.CreateCommand();
            selectComm.CommandText = @"SELECT
                            uuid, uuid_roadwork_activity, uuid_organisationalunit, uuid_user, responsibility_type, phase, sort_order
                        FROM wtb_ssp_activity_responsibilities
                        WHERE uuid = @uuid
                        ORDER BY sort_order";

            selectComm.Parameters.AddWithValue("@uuid", new Guid(uuid));
            return selectComm;
        }

        /// <summary>
        /// Get the activity project lead responsitility of an activity.
        /// </summary>
        /// <param name="uuidActivity"></param>
        /// <returns></returns>
        public ActivityResponsibilityFeature GetProjectLeadByUuidActivity(string uuidActivity)
        {
            ActivityResponsibilityFeature activityResponsibilityFeatureFromDb = null;

            // get data of current user from database:
            using (NpgsqlConnection connection = new NpgsqlConnection(AppConfig.connectionString))
            {
                connection.Open();

                // Get the activity responsibility entry
                using (NpgsqlCommand command = GetProjectLeadByUuidCommand(uuidActivity, connection))
                using (NpgsqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        activityResponsibilityFeatureFromDb = ActivityResponsibilityMapper.FromReader(reader);
                    }
                }
            }

            return activityResponsibilityFeatureFromDb;
        }

        /// <summary>
        /// Get the activity phase responsitilities of an activity.
        /// </summary>
        /// <param name="uuidActivity"></param>
        /// <returns></returns>
        public List<ActivityResponsibilityFeature> GetPhaseLeadsByUuidActivity(string uuidActivity)
        {
            List<ActivityResponsibilityFeature> activityResponsibilityFeaturesFromDb = new List<ActivityResponsibilityFeature>();

            // get data of current user from database:
            using (NpgsqlConnection connection = new NpgsqlConnection(AppConfig.connectionString))
            {
                connection.Open();

                // Get the activity responsibility entry
                using (NpgsqlCommand command = GetPhaseLeadsByUuidCommand(uuidActivity, connection))
                using (NpgsqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        activityResponsibilityFeaturesFromDb.Add(ActivityResponsibilityMapper.FromReader(reader));
                    }
                }
            }

            return activityResponsibilityFeaturesFromDb;
        }

        /// <summary>
        /// Select command to retrieve the project main ActivityResponsibilityFeature (wtb_ssp_activity_responsibilities) by activity uuid.
        /// </summary>
        /// <param name="uuidRoadworkActivity"></param>
        /// <param name="pgConn"></param>
        /// <returns></returns>
        internal static NpgsqlCommand GetProjectLeadByUuidCommand(string uuidRoadworkActivity, NpgsqlConnection pgConn)
        {
            NpgsqlCommand selectComm = pgConn.CreateCommand();
            selectComm.CommandText = @"SELECT
                            uuid, uuid_roadwork_activity, uuid_organisationalunit, uuid_user, responsibility_type, phase, sort_order
                        FROM wtb_ssp_activity_responsibilities 
                        WHERE uuid_roadwork_activity = @uuid_roadwork_activity and responsibility_type like 'ProjectLead'";

            selectComm.Parameters.AddWithValue("@uuid_roadwork_activity", new Guid(uuidRoadworkActivity));
            return selectComm;
        }

        /// <summary>
        /// Select command to retrieve the phase ActivityResponsibilityFeatures (wtb_ssp_activity_responsibilities) by activity uuid.
        /// </summary>
        /// <param name="uuidRoadworkActivity"></param>
        /// <param name="pgConn"></param>
        /// <returns></returns>
        internal static NpgsqlCommand GetPhaseLeadsByUuidCommand(string uuidRoadworkActivity, NpgsqlConnection pgConn)
        {
            NpgsqlCommand selectComm = pgConn.CreateCommand();
            selectComm.CommandText = @"SELECT
                            uuid, uuid_roadwork_activity, uuid_organisationalunit, uuid_user, responsibility_type, phase, sort_order
                        FROM wtb_ssp_activity_responsibilities
                        WHERE uuid_roadwork_activity = @uuid_roadwork_activity and responsibility_type like 'PhaseLead'
                        ORDER BY sort_order";

            selectComm.Parameters.AddWithValue("@uuid_roadwork_activity", new Guid(uuidRoadworkActivity));
            return selectComm;
        }

        /// <summary>
        /// Insert a new activity responsibilityFeature into the Database.
        /// </summary>
        /// <param name="activityResponsibilityFeature"></param>
        /// <returns></returns>
        public ActivityResponsibilityFeature Insert(ActivityResponsibilityFeature activityResponsibilityFeature)
        {
            if (activityResponsibilityFeature == null)
                return null;

            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                using (NpgsqlTransaction transaction = connection.BeginTransaction())
                {
                    using (NpgsqlCommand command = CreateInsertCommand(connection, activityResponsibilityFeature))
                    {
                        command?.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
            }

            return activityResponsibilityFeature;
        }

        /// <summary>
        /// Create an NpgsqlCommand insert command for a ActivityResponsibility object.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="activityResponsibility"></param>
        /// <returns></returns>
        private static NpgsqlCommand CreateInsertCommand(NpgsqlConnection connection, ActivityResponsibilityFeature activityResponsibility)
        {
            var responsibility = activityResponsibility.properties;
            var command = new NpgsqlCommand(@"
                INSERT INTO wtb_ssp_activity_responsibilities (
                    uuid, uuid_roadwork_activity, uuid_organisationalunit, uuid_user, responsibility_type, phase, sort_order)
                VALUES (
                    @uuid, @uuid_roadwork_activity, @uuid_organisationalunit, @uuid_user, @responsibility_type, @phase, @sort_order);",
                connection);

            command.Parameters.AddWithValue("@uuid", new Guid(responsibility.uuid));
            command.Parameters.AddWithValue("@uuid_roadwork_activity", new Guid(responsibility.uuidRoadworkActivity));
            command.Parameters.AddWithValue("@uuid_organisationalunit", HelperFunctions.ToDbNullableGuid(responsibility.uuidOrganisationalUnit));
            command.Parameters.AddWithValue("@uuid_user", HelperFunctions.ToDbNullableGuid(responsibility.uuidUser));
            command.Parameters.AddWithValue("@responsibility_type", responsibility.responsibilityType.ToString());
            command.Parameters.AddWithValue("@phase", HelperFunctions.ToDbValue(responsibility.phase));
            command.Parameters.AddWithValue("@sort_order", HelperFunctions.ToDbValue(responsibility.sortOrder));

            return command;
        }

        /// <summary>
        /// Update a activity responsibilityFeature in the Database.
        /// </summary>
        /// <param name="activityResponsibilityFeature"></param>
        /// <returns></returns>
        public ActivityResponsibilityFeature Update(ActivityResponsibilityFeature activityResponsibilityFeature)
        {
            if (activityResponsibilityFeature == null)
                return null;

            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                using (NpgsqlTransaction transaction = connection.BeginTransaction())
                {
                    using (NpgsqlCommand command = CreateUpdateCommand(connection, activityResponsibilityFeature))
                    {
                        command?.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
            }

            return activityResponsibilityFeature;
        }

        /// <summary>
        /// Create an NpgsqlCommand update command for a ActivityResponsibility object.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="activityResponsibility"></param>
        /// <returns></returns>
        private static NpgsqlCommand CreateUpdateCommand(NpgsqlConnection connection, ActivityResponsibilityFeature activityResponsibility)
        {
            var responsibility = activityResponsibility.properties;
            var command = new NpgsqlCommand(@"
                UPDATE wtb_ssp_activity_responsibilities
                SET
                    uuid_organisationalunit = @uuid_organisationalunit, uuid_user = @uuid_user, phase = @phase, sort_order = @sort_order
                WHERE
                    uuid = @uuid;",
                connection);

            command.Parameters.AddWithValue("@uuid", new Guid(responsibility.uuid));
            command.Parameters.AddWithValue("@uuid_organisationalunit", HelperFunctions.ToDbNullableGuid(responsibility.uuidOrganisationalUnit));
            command.Parameters.AddWithValue("@uuid_user", HelperFunctions.ToDbNullableGuid(responsibility.uuidUser));
            command.Parameters.AddWithValue("@phase", HelperFunctions.ToDbValue(responsibility.phase));
            command.Parameters.AddWithValue("@sort_order", HelperFunctions.ToDbValue(activityResponsibility.properties.sortOrder));

            return command;
        }

        /// <summary>
        /// Delete an activity responsitility by uuid.
        /// </summary>
        /// <param name="uuid"></param>
        /// <returns></returns>
        public int Delete(string uuid)
        {
            // get data of current user from database:
            using (NpgsqlConnection connection = new NpgsqlConnection(AppConfig.connectionString))
            {
                connection.Open();

                // Get the activity responsibility entry
                using (NpgsqlCommand command = DeleteByUuidCommand(uuid, connection))
                {
                    return command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Select command to delete the ActivityResponsibilityFeature (wtb_ssp_journal_entries) by uuid.
        /// </summary>
        /// <param name="uuid"></param>
        /// <param name="pgConn"></param>
        /// <returns></returns>
        internal static NpgsqlCommand DeleteByUuidCommand(string uuid, NpgsqlConnection pgConn)
        {
            NpgsqlCommand selectComm = pgConn.CreateCommand();
            selectComm.CommandText = @"DELETE
                        FROM wtb_ssp_activity_responsibilities
                        WHERE uuid = @uuid";

            selectComm.Parameters.AddWithValue("@uuid", new Guid(uuid));
            return selectComm;
        }
    }
}
