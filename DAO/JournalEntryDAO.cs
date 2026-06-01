using Npgsql;
using roadwork_portal_service.Configuration;
using roadwork_portal_service.Helper;
using roadwork_portal_service.Mappers;
using roadwork_portal_service.Model;

namespace roadwork_portal_service.DAO
{
    /// <summary>
    /// DAO for JournalEntryFeature.
    /// </summary>
    public class JournalEntryDAO
    {
        private string connectionString;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public JournalEntryDAO()
        {
            connectionString = AppConfig.connectionString;
        }

        /// <summary>
        /// Get a journal entry by uuid.
        /// </summary>
        /// <param name="uuid"></param>
        /// <returns></returns>
        public JournalEntryFeature GetByUuid(string uuid)
        {
            JournalEntryFeature journalEntryFeatureFromDb = null;

            // get data of current user from database:
            using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
            {
                pgConn.Open();

                // Get the Journal entry
                using (NpgsqlCommand command = GetByUuidCommand(uuid, pgConn))
                using (NpgsqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        journalEntryFeatureFromDb = JournalEntryMapper.FromReader(reader);
                    }
                }

                pgConn.Close();
            }

            return journalEntryFeatureFromDb;
        }

        /// <summary>
        /// Select command to retrieve the JournalEntryFeature (wtb_ssp_journal_entries) by uuid.
        /// </summary>
        /// <param name="uuid"></param>
        /// <param name="pgConn"></param>
        /// <returns></returns>
        internal static NpgsqlCommand GetByUuidCommand(string uuid, NpgsqlConnection pgConn)
        {
            NpgsqlCommand selectComm = pgConn.CreateCommand();
            selectComm.CommandText = @"SELECT
                            j.uuid, j.uuid_roadwork_activity, j.content, j.created, j.last_modified, j.created_by,
                            u.first_name, u.last_name
                        FROM wtb_ssp_journal_entries j
                        LEFT JOIN wtb_ssp_users u ON u.uuid = j.created_by
                        WHERE j.uuid = @uuid";

            selectComm.Parameters.AddWithValue("@uuid", new Guid(uuid));
            return selectComm;
        }

        /// <summary>
        /// Get all journal entries of the activity.
        /// </summary>
        /// <param name="roadWorkActivityUuid"></param>
        /// <returns></returns>
        public List<JournalEntryFeature> GetByActivityUuid(string roadWorkActivityUuid)
        {
            List<JournalEntryFeature> journalEntryFeaturesFromDb = new List<JournalEntryFeature>();

            // get data of current user from database:
            using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
            {
                pgConn.Open();

                // Get the Journal entry
                using (NpgsqlCommand command = GetByActivityUuidCommand(roadWorkActivityUuid, pgConn))
                using (NpgsqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        journalEntryFeaturesFromDb.Add(JournalEntryMapper.FromReader(reader));
                    }
                }

                pgConn.Close();
            }

            return journalEntryFeaturesFromDb;
        }

        /// <summary>
        /// Select command to retrieve the JournalEntryFeature (wtb_ssp_journal_entries) of the activity (wtb_ssp_roadworkactivities).
        /// </summary>
        /// <param name="roadWorkActivityUuid"></param>
        /// <param name="pgConn"></param>
        /// <returns></returns>
        internal static NpgsqlCommand GetByActivityUuidCommand(string roadWorkActivityUuid, NpgsqlConnection pgConn)
        {
            NpgsqlCommand selectComm = pgConn.CreateCommand();
            selectComm.CommandText = @"SELECT
                            j.uuid, j.uuid_roadwork_activity, j.content, j.created, j.last_modified, j.created_by,
                            u.first_name, u.last_name
                        FROM wtb_ssp_journal_entries j
                        LEFT JOIN wtb_ssp_users u ON u.uuid = j.created_by
                        WHERE j.uuid_roadwork_activity = @uuid_roadwork_activity
                        ORDER BY j.sort_order";

            selectComm.Parameters.AddWithValue("@uuid_roadwork_activity", new Guid(roadWorkActivityUuid));
            return selectComm;
        }

        /// <summary>
        /// Insert a new JournalFeature into the Database.
        /// </summary>
        /// <param name="journalEntryFeature"></param>
        /// <returns></returns>
        public JournalEntryFeature Insert(JournalEntryFeature journalEntryFeature)
        {
            if (journalEntryFeature == null)
                return null;

            using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            connection.Open();

            using (NpgsqlTransaction transaction = connection.BeginTransaction())
            {
                using (NpgsqlCommand command = CreateInsertCommand(connection, journalEntryFeature))
                {
                    command?.ExecuteNonQuery();
                }

                transaction.Commit();
            }

            return journalEntryFeature;
        }

        /// <summary>
        /// Create an NpgsqlCommand insert command for a JournalEntry object.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="journalEntry"></param>
        /// <returns></returns>
        private static NpgsqlCommand CreateInsertCommand(NpgsqlConnection connection, JournalEntryFeature journalEntry)
        {
            var command = new NpgsqlCommand(@"
                INSERT INTO wtb_ssp_journal_entries (
                    uuid, uuid_roadwork_activity, content, created, last_modified, created_by)
                VALUES (
                    @uuid, @uuid_roadwork_activity, @content, @created, @last_modified, @created_by);",
                /* RETURNING
                    uuid, content, created, last_modified, created_by;", */
                connection);

            command.Parameters.AddWithValue("@uuid", new Guid(journalEntry.properties.uuid));
            command.Parameters.AddWithValue("@uuid_roadwork_activity", new Guid(journalEntry.properties.uuidRoadworkActivity));
            command.Parameters.AddWithValue("@content", journalEntry.properties.content);
            command.Parameters.AddWithValue("@created", HelperFunctions.ToDbValue(journalEntry.properties.created));
            command.Parameters.AddWithValue("@last_modified", HelperFunctions.ToDbValue(journalEntry.properties.lastModified));
            command.Parameters.AddWithValue("@created_by", new Guid(journalEntry.properties.createdBy));

            return command;
        }

        /// <summary>
        /// Update a JournalFeature in the Database.
        /// </summary>
        /// <param name="journalEntryFeature"></param>
        /// <returns></returns>
        public JournalEntryFeature Update(JournalEntryFeature journalEntryFeature)
        {
            if (journalEntryFeature == null)
                return null;

            using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            connection.Open();

            using (NpgsqlTransaction transaction = connection.BeginTransaction())
            {
                using (NpgsqlCommand command = CreateUpdateCommand(connection, journalEntryFeature))
                {
                    command?.ExecuteNonQuery();
                }

                transaction.Commit();
            }

            return journalEntryFeature;
        }

        /// <summary>
        /// Create an NpgsqlCommand update command for a JournalEntry object.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="journalEntry"></param>
        /// <returns></returns>
        private static NpgsqlCommand CreateUpdateCommand(NpgsqlConnection connection, JournalEntryFeature journalEntry)
        {
            var command = new NpgsqlCommand(@"
                UPDATE wtb_ssp_journal_entries
                SET
                    content = @content, last_modified = @last_modified
                WHERE
                    uuid = @uuid;",
                connection);

            command.Parameters.AddWithValue("@uuid", new Guid(journalEntry.properties.uuid));
            command.Parameters.AddWithValue("@content", journalEntry.properties.content);
            command.Parameters.AddWithValue("@last_modified", HelperFunctions.ToDbValue(journalEntry.properties.lastModified));

            return command;
        }
    }
}
