using Npgsql;
using roadwork_portal_service.Extensions;
using roadwork_portal_service.Helper;
using roadwork_portal_service.Model;

namespace roadwork_portal_service.Mappers
{
    /// <summary>
    /// Map from and to RoadWorkApprovals.
    /// </summary>
    public static class RoadWorkApprovalsMapper
    {
        /// <summary>
        /// Maps from the NpgsqlDataReader to RoadWorkApprovals. No aliases for the columns allowed!
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static RoadWorkApprovals FromReader(NpgsqlDataReader reader)
        {
            return new RoadWorkApprovals
            {
                uuid = reader.GetGuidAsStringOrEmpty("uuid_parameters"),
                uuidRoadworkActivity = reader.GetGuidAsStringOrEmpty("uuid_roadwork_activity"),
                uuidRoadworkNeed = reader.GetGuidAsStringOrEmpty("uuid_roadwork_need"),
                approvalRequired = reader.GetBooleanOrFalse("approval_required"),
                strgApprovalRequired = reader.GetBooleanOrFalse("strg_approval_required"),
                bafuApprovalRequired = reader.GetBooleanOrFalse("bafu_approval_required"),
                lsvApprovalRequired = reader.GetBooleanOrFalse("lsv_approval_required"),
                ssvApprovalRequired = reader.GetBooleanOrFalse("ssv_approval_required"),
                wwgApprovalRequired = reader.GetBooleanOrFalse("wwg_approval_required"),
                eriApprovalRequired = reader.GetBooleanOrFalse("eri_approval_required"),
                pbgApprovalRequired = reader.GetBooleanOrFalse("pbg_approval_required"),
                ebgApprovalRequired = reader.GetBooleanOrFalse("ebg_approval_required"),
                awelApprovalRequired = reader.GetBooleanOrFalse("awel_approval_required"),
                estiApprovalRequired = reader.GetBooleanOrFalse("esti_approval_required"),
                otherApprovalRequired = reader.GetBooleanOrFalse("other_approval_required"),
                otherApprovalDetails = reader.GetStringOrEmpty("other_approval_details"),
            };
        }

        /// <summary>
        /// Create an NpgsqlCommand insert command for a RoadWorkApprovals object.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="roadWorkApprovals"></param>
        /// <returns></returns>
        public static NpgsqlCommand CreateInsertOrUpdateCommand(NpgsqlConnection connection, RoadWorkApprovals roadWorkApprovals)
        {
            // Check the fks (XOR)
            if (string.IsNullOrEmpty(roadWorkApprovals.uuidRoadworkActivity) == string.IsNullOrEmpty(roadWorkApprovals.uuidRoadworkNeed))
                throw new ArgumentException("The RoadWorkApprovals must have exactly one valid foreign key (uuidRoadworkActivity or uuidRoadworkNeed)");

            // Get the keys
            Guid uuid = roadWorkApprovals.uuid?.ToGuidOrNewGuid() ?? Guid.NewGuid();
            Guid? uuidRoadworkActivity = roadWorkApprovals.uuidRoadworkActivity?.ToNullableGuid();
            Guid? uuidRoadworkNeed = roadWorkApprovals.uuidRoadworkNeed?.ToNullableGuid();

            var command = new NpgsqlCommand(@"
                INSERT INTO wtb_ssp_roadwork_approvals (
                    uuid, uuid_roadwork_activity, uuid_roadwork_need,
                    approval_required,
                    strg_approval_required, bafu_approval_required, lsv_approval_required,
                    ssv_approval_required, wwg_approval_required, eri_approval_required,
                    pbg_approval_required, ebg_approval_required, awel_approval_required,
                    esti_approval_required, other_approval_required, other_approval_details)
                VALUES (
                    @uuid, @uuid_roadwork_activity, @uuid_roadwork_need,
                    @approval_required,
                    @strg_approval_required, @bafu_approval_required, @lsv_approval_required,
                    @ssv_approval_required, @wwg_approval_required, @eri_approval_required,
                    @pbg_approval_required, @ebg_approval_required, @awel_approval_required,
                    @esti_approval_required, @other_approval_required, @other_approval_details)
                ON CONFLICT (uuid)
                DO UPDATE SET
                    uuid_roadwork_activity = EXCLUDED.uuid_roadwork_activity, uuid_roadwork_need = EXCLUDED.uuid_roadwork_need,
                    approval_required = EXCLUDED.approval_required, strg_approval_required = EXCLUDED.strg_approval_required, 
                    bafu_approval_required = EXCLUDED.bafu_approval_required, lsv_approval_required = EXCLUDED.lsv_approval_required,
                    ssv_approval_required = EXCLUDED.ssv_approval_required, wwg_approval_required = EXCLUDED.wwg_approval_required, 
                    eri_approval_required = EXCLUDED.eri_approval_required, pbg_approval_required = EXCLUDED.pbg_approval_required, 
                    ebg_approval_required = EXCLUDED.ebg_approval_required, awel_approval_required = EXCLUDED.awel_approval_required,
                    esti_approval_required = EXCLUDED.esti_approval_required, other_approval_required = EXCLUDED.other_approval_required, 
                    other_approval_details = EXCLUDED.other_approval_details;",
                /* RETURNING
                    uuid, uuid_roadwork_activity, uuid_roadwork_need,
                    approval_required,
                    strg_approval_required, bafu_approval_required, lsv_approval_required,
                    ssv_approval_required, wwg_approval_required, eri_approval_required,
                    pbg_approval_required, ebg_approval_required, awel_approval_required,
                    esti_approval_required, other_approval_required, other_approval_details;", */
                connection);

            command.Parameters.AddWithValue("@uuid", uuid);
            command.Parameters.AddWithValue("@uuid_roadwork_activity", HelperFunctions.ToDbValue(uuidRoadworkActivity));
            command.Parameters.AddWithValue("@uuid_roadwork_need", HelperFunctions.ToDbValue(uuidRoadworkNeed));
            command.Parameters.AddWithValue("@approval_required", HelperFunctions.ToDbValue(roadWorkApprovals.approvalRequired));
            command.Parameters.AddWithValue("@strg_approval_required", HelperFunctions.ToDbValue(roadWorkApprovals.strgApprovalRequired));
            command.Parameters.AddWithValue("@bafu_approval_required", HelperFunctions.ToDbValue(roadWorkApprovals.bafuApprovalRequired));
            command.Parameters.AddWithValue("@lsv_approval_required", HelperFunctions.ToDbValue(roadWorkApprovals.lsvApprovalRequired));
            command.Parameters.AddWithValue("@ssv_approval_required", HelperFunctions.ToDbValue(roadWorkApprovals.ssvApprovalRequired));
            command.Parameters.AddWithValue("@wwg_approval_required", HelperFunctions.ToDbValue(roadWorkApprovals.wwgApprovalRequired));
            command.Parameters.AddWithValue("@eri_approval_required", HelperFunctions.ToDbValue(roadWorkApprovals.eriApprovalRequired));
            command.Parameters.AddWithValue("@pbg_approval_required", HelperFunctions.ToDbValue(roadWorkApprovals.pbgApprovalRequired));
            command.Parameters.AddWithValue("@ebg_approval_required", HelperFunctions.ToDbValue(roadWorkApprovals.ebgApprovalRequired));
            command.Parameters.AddWithValue("@awel_approval_required", HelperFunctions.ToDbValue(roadWorkApprovals.awelApprovalRequired));
            command.Parameters.AddWithValue("@esti_approval_required", HelperFunctions.ToDbValue(roadWorkApprovals.estiApprovalRequired));
            command.Parameters.AddWithValue("@other_approval_required", HelperFunctions.ToDbValue(roadWorkApprovals.otherApprovalRequired));
            command.Parameters.AddWithValue("@other_approval_details", HelperFunctions.ToDbValue(roadWorkApprovals.otherApprovalDetails));

            return command;
        }
    }
}
