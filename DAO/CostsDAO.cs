using Npgsql;
using roadwork_portal_service.Configuration;
using roadwork_portal_service.Helper;
using roadwork_portal_service.Mappers;
using roadwork_portal_service.Model;

namespace roadwork_portal_service.DAO
{
    /// <summary>
    /// DAO for Costs.
    /// </summary>
    public class CostsDAO
    {
        private string connectionString;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public CostsDAO()
        {
            connectionString = AppConfig.connectionString;
        }

        /// <summary>
        /// Get a coast entry by uuid.
        /// </summary>
        /// <param name="uuid"></param>
        /// <returns></returns>
        public Costs GetByUuid(string uuid)
        {
            Costs costsFromDb = null;

            // get data of current user from database:
            using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
            {
                pgConn.Open();

                // Get the cost entry
                using (NpgsqlCommand command = GetByUuidCommand(uuid, pgConn))
                using (NpgsqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        costsFromDb = CostsMapper.FromReader(reader);
                    }
                }

                pgConn.Close();
            }

            return costsFromDb;
        }

        /// <summary>
        /// Select command to retrieve the Cost (wtb_ssp_costs) by uuid.
        /// </summary>
        /// <param name="uuid"></param>
        /// <param name="pgConn"></param>
        /// <returns></returns>
        internal static NpgsqlCommand GetByUuidCommand(string uuid, NpgsqlConnection pgConn)
        {
            NpgsqlCommand selectComm = pgConn.CreateCommand();
            selectComm.CommandText = @"SELECT
                            uuid, roadworkneed, costs, project_type, costs_comment, erp_number
                        FROM wtb_ssp_costs
                        WHERE uuid = @uuid";

            selectComm.Parameters.AddWithValue("@uuid", new Guid(uuid));
            return selectComm;
        }

        /// <summary>
        /// Insert a new costs into the Database.
        /// </summary>
        /// <param name="costs"></param>
        /// <param name="roadWorkNeedProperties"></param>
        /// <returns></returns>
        public Costs Insert(Costs costs, RoadWorkNeedProperties roadWorkNeedProperties)
        {
            if (costs == null)
                return null;

            using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            connection.Open();

            using (NpgsqlTransaction transaction = connection.BeginTransaction())
            {
                using (NpgsqlCommand command = CreateInsertCommand(connection, costs, roadWorkNeedProperties))
                {
                    command?.ExecuteNonQuery();
                }

                transaction.Commit();
            }

            return costs;
        }

        /// <summary>
        /// Create an NpgsqlCommand insert command for a Costs object.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="costs"></param>
        /// <param name="roadWorkNeedProperties"></param>
        /// <returns></returns>
        private static NpgsqlCommand CreateInsertCommand(NpgsqlConnection connection, Costs costs, RoadWorkNeedProperties roadWorkNeedProperties)
        {
            var command = new NpgsqlCommand(@"
                INSERT INTO wtb_ssp_costs (
                    uuid, roadworkneed, costs, project_type, costs_comment, erp_number)
                VALUES (
                    @uuid, @roadworkneed, @costs, @project_type, @costs_comment, @erp_number);",
                /* RETURNING
                    uuid, roadworkneed, costs, project_type, costs_comment, erp_number;", */
                connection);

            command.Parameters.AddWithValue("@uuid", new Guid(costs.uuid));
            command.Parameters.AddWithValue("@roadworkneed", new Guid(roadWorkNeedProperties.uuid));
            command.Parameters.AddWithValue("@costs", HelperFunctions.ToDbValue(costs.costs));
            command.Parameters.AddWithValue("@project_type", HelperFunctions.ToDbValue(costs.projectType));
            command.Parameters.AddWithValue("@costs_comment", HelperFunctions.ToDbValue(costs.costsComment));
            command.Parameters.AddWithValue("@erp_number", HelperFunctions.ToDbValue(costs.erpNumber));

            return command;
        }

        /// <summary>
        /// Update a Costs in the Database.
        /// </summary>
        /// <param name="costs"></param>
        /// <returns></returns>
        public Costs Update(Costs costs)
        {
            if (costs == null)
                return null;

            using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            connection.Open();

            using (NpgsqlTransaction transaction = connection.BeginTransaction())
            {
                using (NpgsqlCommand command = CreateUpdateCommand(connection, costs))
                {
                    command?.ExecuteNonQuery();
                }

                transaction.Commit();
            }

            return costs;
        }

        /// <summary>
        /// Create an NpgsqlCommand update command for a Costs object.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="costs"></param>
        /// <returns></returns>
        private static NpgsqlCommand CreateUpdateCommand(NpgsqlConnection connection, Costs costs)
        {
            var command = new NpgsqlCommand(@"
                UPDATE wtb_ssp_costs
                SET
                    costs = @costs, project_type = @project_type, costs_comment = @costs_comment, erp_number = @erp_number
                WHERE
                    uuid = @uuid;",
                connection);

            command.Parameters.AddWithValue("@uuid", new Guid(costs.uuid));
            command.Parameters.AddWithValue("@costs", HelperFunctions.ToDbValue(costs.costs));
            command.Parameters.AddWithValue("@project_type", HelperFunctions.ToDbValue(costs.projectType));
            command.Parameters.AddWithValue("@costs_comment", HelperFunctions.ToDbValue(costs.costsComment));
            command.Parameters.AddWithValue("@erp_number", HelperFunctions.ToDbValue(costs.erpNumber));

            return command;
        }
    }
}
