using Npgsql;
using roadwork_portal_service.Extensions;
using roadwork_portal_service.Model;

namespace roadwork_portal_service.Mappers
{
    /// <summary>
    /// Map from and to JournalEntryMapper.
    /// </summary>
    public static class JournalEntryMapper
    {
        /// <summary>
        /// Maps from the NpgsqlDataReader to JournalEntryMapper. No aliases for the columns allowed!
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static JournalEntryFeature FromReader(NpgsqlDataReader reader)
        {
            return new JournalEntryFeature()
            {
                properties = new JournalEntryProperties()
                {
                    uuid = reader.GetGuidAsStringOrEmpty("uuid"),
                    uuidRoadworkActivity = reader.GetGuidAsStringOrEmpty("uuid_roadwork_activity"),
                    content = reader.GetStringOrEmpty("content"),
                    created = reader.GetNullableDateTime("created"),
                    lastModified = reader.GetNullableDateTime("last_modified"),
                    createdBy = reader.GetGuidAsStringOrEmpty("created_by"),
                    createdByName = $"{reader.GetStringOrEmpty("first_name")} {reader.GetStringOrEmpty("last_name")}",
                }
            };
        }
    }
}
