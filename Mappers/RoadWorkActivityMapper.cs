using Npgsql;
using roadwork_portal_service.Extensions;
using roadwork_portal_service.Helper;
using roadwork_portal_service.Model;

namespace roadwork_portal_service.Mappers
{
    /// <summary>
    /// Map from and to RoadWorkActivityMapper.
    /// </summary>
    public static class RoadWorkActivityMapper
    {
        /// <summary>
        /// Maps from the NpgsqlDataReader to RoadWorkActivityMapper. No aliases for the columns allowed!
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static RoadWorkActivityProperties FromReader(NpgsqlDataReader reader)
        {
            return FromReader(reader, new RoadWorkActivityProperties());
        }

        /// <summary>
        /// Maps from the NpgsqlDataReader to RoadWorkActivityMapper. No aliases for the columns allowed!
        /// Only a partial mapping of the RoadWorkActivityProperties. Therefore with the existing object as parameter.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="roadWorkActivityProperties"></param>
        /// <returns></returns>
        public static RoadWorkActivityProperties FromReader(NpgsqlDataReader reader, RoadWorkActivityProperties roadWorkActivityProperties)
        {
            // Aggloprogramm
            roadWorkActivityProperties.partOfAggloprogram = reader.GetBooleanOrFalse("part_of_aggloprogram");
            roadWorkActivityProperties.aggloprogramGeneration = reader.GetNullableInt("aggloprogram_generation");
            roadWorkActivityProperties.aggloprogramAreCode = reader.GetStringOrEmpty("aggloprogram_are_code");
            roadWorkActivityProperties.aggloprogramAreDescription = reader.GetStringOrEmpty("aggloprogram_are_description");
            roadWorkActivityProperties.aggloprogramDueDate = reader.GetNullableDateTime("aggloprogram_due_date");
            roadWorkActivityProperties.aggloprogramCostTotal = reader.GetNullableDecimal("aggloprogram_cost_total");
            roadWorkActivityProperties.aggloprogramCostCanton = reader.GetNullableDecimal("aggloprogram_cost_canton");

            // Vorstudie
            //roadWorkActivityProperties.preliminaryStudyRequired = reader.GetBooleanOrFalse("prel_study_required");
            //roadWorkActivityProperties.preliminaryStudyDuration = reader.GetStringOrEmpty("prel_study_duration");
            //roadWorkActivityProperties.preliminaryStudyDetail = reader.GetStringOrEmpty("prel_study_detail");
            //roadWorkActivityProperties.preliminaryStudyVkErConfirmed = reader.GetNullableDateTime("prel_study_vk_er_confirmed");
            //roadWorkActivityProperties.preliminaryStudyVkErNumber = reader.GetNullableLong("prel_study_vk_er_number");

            // Betroffene Themen
            roadWorkActivityProperties.busStopsSheltersAffected = reader.GetBooleanOrFalse("bus_stops_shelters_affected");
            roadWorkActivityProperties.structuresAffected = reader.GetBooleanOrFalse("structures_affected");
            roadWorkActivityProperties.roadDrainageAffected = reader.GetBooleanOrFalse("roadDrainage_affected");
            roadWorkActivityProperties.houseConnectionsAffected = reader.GetBooleanOrFalse("houseConnections_affected");
            roadWorkActivityProperties.wasteFacilitiesAffected = reader.GetBooleanOrFalse("wasteFacilities_affected");
            roadWorkActivityProperties.technicalInstallationsAffected = reader.GetBooleanOrFalse("technical_installations_affected");
            roadWorkActivityProperties.treesAffected = reader.GetBooleanOrFalse("trees_affected");
            roadWorkActivityProperties.streetFurnitureAffected = reader.GetBooleanOrFalse("street_furniture_affected");
            roadWorkActivityProperties.urbanClimateAffected = reader.GetBooleanOrFalse("urban_climate_affected");
            roadWorkActivityProperties.subjectToDepaving = reader.GetBooleanOrFalse("subject_to_depaving");
            roadWorkActivityProperties.pedestriansCyclingAffected = reader.GetBooleanOrFalse("pedestrians_cycling_affected");
            roadWorkActivityProperties.disabilityEqualityAffected = reader.GetBooleanOrFalse("disability_equality_affected");
            roadWorkActivityProperties.trafficRegulationAffected = reader.GetBooleanOrFalse("traffic_regulation_affected");

            // Private betroffen
            roadWorkActivityProperties.privateEntityAffected = reader.GetBooleanOrFalse("private_entity_affected");
            roadWorkActivityProperties.privateEntityExtent = reader.GetStringOrEmpty("private_entity_extent");
            roadWorkActivityProperties.privateEntityRequirements = reader.GetStringOrEmpty("private_entity_requirements");
            roadWorkActivityProperties.privateEntityAcquisition = reader.GetStringOrEmpty("private_entity_acquisition");
            roadWorkActivityProperties.privateEntityIsInitiator = reader.GetStringOrEmpty("private_entity_is_initiator");

            // Provis (Abacus)
            roadWorkActivityProperties.erpNumber = reader.GetNullableLong("erp_number");

            // Ressourcen
            roadWorkActivityProperties.staffResourcesAprConfirmed = reader.GetNullableDateTime("staff_resources_apr_confirmed");
            roadWorkActivityProperties.costEstimateAprConfirmed = reader.GetNullableDateTime("cost_estimate_apr_confirmed");

            // Projektierungsauftrag
            roadWorkActivityProperties.coreDrillingContracted = reader.GetBooleanOrFalse("core_drilling_contracted");
            roadWorkActivityProperties.quotesRequested = reader.GetBooleanOrFalse("quotes_requested");
            roadWorkActivityProperties.quotesReviewed = reader.GetBooleanOrFalse("quotes_reviewed");
            roadWorkActivityProperties.aprChecked = reader.GetBooleanOrFalse("apr_checked");
            roadWorkActivityProperties.afmChecked = reader.GetBooleanOrFalse("afm_checked");

            // Ausgabengenehmigung und Ablage
            roadWorkActivityProperties.cfDone = reader.GetBooleanOrFalse("cf_done");
            roadWorkActivityProperties.rdDone = reader.GetBooleanOrFalse("rd_done");
            roadWorkActivityProperties.approved = reader.GetBooleanOrFalse("approved");
            roadWorkActivityProperties.fabasoftDone = reader.GetBooleanOrFalse("fabasoft_done");
            roadWorkActivityProperties.gisUpdated = reader.GetBooleanOrFalse("gis_updated");

            return roadWorkActivityProperties;
        }

        /// <summary>
        /// Create an NpgsqlCommand insert command for a RoadWorkApprovals object.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="roadWorkActivityProperties"></param>
        /// <returns></returns>
        public static NpgsqlCommand CreateUpdateCommand(NpgsqlConnection connection, RoadWorkActivityProperties roadWorkActivityProperties)
        {
            var command = new NpgsqlCommand(@"
                UPDATE wtb_ssp_roadworkactivities 
                SET 
                    -- Aggloprogramm
                    part_of_aggloprogram = @part_of_aggloprogram, aggloprogram_generation = @aggloprogram_generation,
                    aggloprogram_are_code = @aggloprogram_are_code, aggloprogram_are_description = @aggloprogram_are_description, 
                    aggloprogram_due_date = @aggloprogram_due_date, aggloprogram_cost_total = @aggloprogram_cost_total,
                    aggloprogram_cost_canton = @aggloprogram_cost_canton,
                    -- Vorstudie
                    --prel_study_required = @prel_study_required, prel_study_duration = @prel_study_duration, 
                    --prel_study_detail = @prel_study_detail, prel_study_vk_er_confirmed = @prel_study_vk_er_confirmed, 
                    --prel_study_vk_er_number = @prel_study_vk_er_number,
                    -- Betroffene Themen
                    bus_stops_shelters_affected = @bus_stops_shelters_affected, structures_affected = @structures_affected, 
                    roadDrainage_affected = @roadDrainage_affected, houseConnections_affected = @houseConnections_affected, 
                    wasteFacilities_affected = @wasteFacilities_affected, technical_installations_affected = @technical_installations_affected,
                    trees_affected = @trees_affected, street_furniture_affected = @street_furniture_affected, 
                    urban_climate_affected = @urban_climate_affected, subject_to_depaving = @subject_to_depaving,
                    pedestrians_cycling_affected = @pedestrians_cycling_affected, disability_equality_affected = @disability_equality_affected,
                    traffic_regulation_affected = @traffic_regulation_affected, 
                    -- Private betroffen
                    private_entity_affected = @private_entity_affected, private_entity_extent = @private_entity_extent, 
                    private_entity_requirements = @private_entity_requirements, private_entity_acquisition = @private_entity_acquisition, 
                    private_entity_is_initiator = @private_entity_is_initiator,
                    -- Provis (Abacus)
                    erp_number = @erp_number,
                    -- Ressourcen
                    staff_resources_apr_confirmed = @staff_resources_apr_confirmed, cost_estimate_apr_confirmed = @cost_estimate_apr_confirmed,
                    -- Projektierungsauftrag
                    core_drilling_contracted = @core_drilling_contracted, quotes_requested = @quotes_requested, 
                    quotes_reviewed = @quotes_reviewed, apr_checked = @apr_checked, afm_checked = @afm_checked, 
                    -- Ausgabengenehmigung und Ablage
                    cf_done = @cf_done, rd_done = @rd_done, approved = @approved, fabasoft_done = @fabasoft_done, gis_updated = @gis_updated
                WHERE uuid = @uuid;",
                connection);

            Guid uuid = roadWorkActivityProperties.uuid.ToGuid();
            command.Parameters.AddWithValue("@uuid", uuid);
            AddParameters(command.Parameters, roadWorkActivityProperties);

            return command;
        }

        /// <summary>
        /// Adds the parameters the activity command(partial only!).
        /// </summary>
        /// <param name="parameters"></param>
        internal static void AddParameters(NpgsqlParameterCollection parameters, RoadWorkActivityProperties roadWorkActivityProperties)
        {
            // Aggloprogramm
            parameters.AddWithValue("@part_of_aggloprogram", HelperFunctions.ToDbValue(roadWorkActivityProperties.partOfAggloprogram));
            parameters.AddWithValue("@aggloprogram_generation", HelperFunctions.ToDbValue(roadWorkActivityProperties.aggloprogramGeneration));
            parameters.AddWithValue("@aggloprogram_are_code", HelperFunctions.ToDbValue(roadWorkActivityProperties.aggloprogramAreCode));
            parameters.AddWithValue("@aggloprogram_are_description", HelperFunctions.ToDbValue(roadWorkActivityProperties.aggloprogramAreDescription));
            parameters.AddWithValue("@aggloprogram_due_date", HelperFunctions.ToDbValue(roadWorkActivityProperties.aggloprogramDueDate));
            parameters.AddWithValue("@aggloprogram_cost_total", HelperFunctions.ToDbValue(roadWorkActivityProperties.aggloprogramCostTotal));
            parameters.AddWithValue("@aggloprogram_cost_canton", HelperFunctions.ToDbValue(roadWorkActivityProperties.aggloprogramCostCanton));

            // Vorstudie
            //parameters.AddWithValue("@prel_study_required", HelperFunctions.ToDbValue(roadWorkActivityProperties.preliminaryStudyRequired));
            //parameters.AddWithValue("@prel_study_duration", HelperFunctions.ToDbValue(roadWorkActivityProperties.preliminaryStudyDuration));
            //parameters.AddWithValue("@prel_study_detail", HelperFunctions.ToDbValue(roadWorkActivityProperties.preliminaryStudyDetail));
            //parameters.AddWithValue("@prel_study_vk_er_confirmed", HelperFunctions.ToDbValue(roadWorkActivityProperties.preliminaryStudyVkErConfirmed));
            //parameters.AddWithValue("@prel_study_vk_er_number", HelperFunctions.ToDbValue(roadWorkActivityProperties.preliminaryStudyVkErNumber));

            // Betroffene Themen
            parameters.AddWithValue("@bus_stops_shelters_affected", HelperFunctions.ToDbValue(roadWorkActivityProperties.busStopsSheltersAffected));
            parameters.AddWithValue("@structures_affected", HelperFunctions.ToDbValue(roadWorkActivityProperties.structuresAffected));
            parameters.AddWithValue("@roadDrainage_affected", HelperFunctions.ToDbValue(roadWorkActivityProperties.roadDrainageAffected));
            parameters.AddWithValue("@houseConnections_affected", HelperFunctions.ToDbValue(roadWorkActivityProperties.houseConnectionsAffected));
            parameters.AddWithValue("@wasteFacilities_affected", HelperFunctions.ToDbValue(roadWorkActivityProperties.wasteFacilitiesAffected));
            parameters.AddWithValue("@technical_installations_affected", HelperFunctions.ToDbValue(roadWorkActivityProperties.technicalInstallationsAffected));
            parameters.AddWithValue("@trees_affected", HelperFunctions.ToDbValue(roadWorkActivityProperties.treesAffected));
            parameters.AddWithValue("@street_furniture_affected", HelperFunctions.ToDbValue(roadWorkActivityProperties.streetFurnitureAffected));
            parameters.AddWithValue("@urban_climate_affected", HelperFunctions.ToDbValue(roadWorkActivityProperties.urbanClimateAffected));
            parameters.AddWithValue("@subject_to_depaving", HelperFunctions.ToDbValue(roadWorkActivityProperties.subjectToDepaving));
            parameters.AddWithValue("@pedestrians_cycling_affected", HelperFunctions.ToDbValue(roadWorkActivityProperties.pedestriansCyclingAffected));
            parameters.AddWithValue("@disability_equality_affected", HelperFunctions.ToDbValue(roadWorkActivityProperties.disabilityEqualityAffected));
            parameters.AddWithValue("@traffic_regulation_affected", HelperFunctions.ToDbValue(roadWorkActivityProperties.trafficRegulationAffected));

            // Private betroffen
            parameters.AddWithValue("@private_entity_affected", HelperFunctions.ToDbValue(roadWorkActivityProperties.privateEntityAffected));
            parameters.AddWithValue("@private_entity_extent", HelperFunctions.ToDbValue(roadWorkActivityProperties.privateEntityExtent));
            parameters.AddWithValue("@private_entity_requirements", HelperFunctions.ToDbValue(roadWorkActivityProperties.privateEntityRequirements));
            parameters.AddWithValue("@private_entity_acquisition", HelperFunctions.ToDbValue(roadWorkActivityProperties.privateEntityAcquisition));
            parameters.AddWithValue("@private_entity_is_initiator", HelperFunctions.ToDbValue(roadWorkActivityProperties.privateEntityIsInitiator));

            // Provis (Abacus)
            parameters.AddWithValue("@erp_number", HelperFunctions.ToDbValue(roadWorkActivityProperties.erpNumber));

            // Ressourcen
            parameters.AddWithValue("@staff_resources_apr_confirmed", HelperFunctions.ToDbValue(roadWorkActivityProperties.staffResourcesAprConfirmed));
            parameters.AddWithValue("@cost_estimate_apr_confirmed", HelperFunctions.ToDbValue(roadWorkActivityProperties.costEstimateAprConfirmed));

            // Projektierungsauftrag
            parameters.AddWithValue("@core_drilling_contracted", HelperFunctions.ToDbValue(roadWorkActivityProperties.coreDrillingContracted));
            parameters.AddWithValue("@quotes_requested", HelperFunctions.ToDbValue(roadWorkActivityProperties.quotesRequested));
            parameters.AddWithValue("@quotes_reviewed", HelperFunctions.ToDbValue(roadWorkActivityProperties.quotesReviewed));
            parameters.AddWithValue("@apr_checked", HelperFunctions.ToDbValue(roadWorkActivityProperties.aprChecked));
            parameters.AddWithValue("@afm_checked", HelperFunctions.ToDbValue(roadWorkActivityProperties.afmChecked));

            // Ausgabengenehmigung und Ablage
            parameters.AddWithValue("@cf_done", HelperFunctions.ToDbValue(roadWorkActivityProperties.cfDone));
            parameters.AddWithValue("@rd_done", HelperFunctions.ToDbValue(roadWorkActivityProperties.rdDone));
            parameters.AddWithValue("@approved", HelperFunctions.ToDbValue(roadWorkActivityProperties.approved));
            parameters.AddWithValue("@fabasoft_done", HelperFunctions.ToDbValue(roadWorkActivityProperties.fabasoftDone));
            parameters.AddWithValue("@gis_updated", HelperFunctions.ToDbValue(roadWorkActivityProperties.gisUpdated));
        }
    }
}
