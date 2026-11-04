using Npgsql;
using roadwork_portal_service.Extensions;
using roadwork_portal_service.Model;

namespace roadwork_portal_service.Mappers
{
    /// <summary>
    /// Map from and to Costs.
    /// </summary>
    public static class CostsMapper
    {
        /// <summary>
        /// Maps from the NpgsqlDataReader to CostsMapper. No aliases for the columns allowed!
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static Costs FromReader(NpgsqlDataReader reader)
        {
            return new Costs()
            {
                uuid = reader.GetGuidAsStringOrEmpty("uuid"),
                costs = reader.GetNullableDecimal("costs"),
                projectType = reader.GetStringOrEmpty("project_type"),
                costsComment = reader.GetStringOrEmpty("costs_comment"),
                erpNumber = reader.GetStringOrEmpty("erp_number"),
            };
        }
    }
}
