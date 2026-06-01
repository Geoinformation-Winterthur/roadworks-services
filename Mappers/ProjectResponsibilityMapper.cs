using Npgsql;
using roadwork_portal_service.Extensions;
using roadwork_portal_service.Model;

namespace roadwork_portal_service.Mappers
{
    /// <summary>
    /// Map from and to ActivityResponsibilityMapper.
    /// </summary>
    public static class ActivityResponsibilityMapper
    {
        /// <summary>
        /// Maps from the NpgsqlDataReader to ActivityResponsibilityMapper. No aliases for the columns allowed!
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static ActivityResponsibilityFeature FromReader(NpgsqlDataReader reader)
        {
            return new ActivityResponsibilityFeature()
            {
                properties = new ActivityResponsibilityProperties()
                {
                    uuid = reader.GetGuidAsStringOrEmpty("uuid"),
                    uuidRoadworkActivity = reader.GetGuidAsStringOrEmpty("uuid_roadwork_activity"),
                    uuidOrganisationalUnit = reader.GetGuidAsStringOrEmpty("uuid_organisationalunit"),
                    uuidUser = reader.GetGuidAsStringOrEmpty("uuid_user"),
                    responsibilityType = Enum.Parse<ResponsibilityType>(reader.GetStringOrEmpty("responsibility_type"), true),
                    phase = reader.GetStringOrEmpty("phase"),
                    sortOrder = reader.GetShortOrDefault("sort_order", 0),
                }
            };
        }
    }
}
