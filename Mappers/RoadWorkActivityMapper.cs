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
            // Additional attributes for journal
            roadWorkActivityProperties.plannedTasks = reader.GetStringOrEmpty("planned_tasks");
            roadWorkActivityProperties.constraintsDependencies = reader.GetStringOrEmpty("constraints_dependencies");
            roadWorkActivityProperties.acquisitionPlanned = reader.GetStringOrEmpty("acquisition_planned");

            // Aggloprogramm
            roadWorkActivityProperties.partOfAggloprogram = reader.GetBooleanOrFalse("part_of_aggloprogram");
            roadWorkActivityProperties.aggloprogramLink = reader.GetStringOrEmpty("aggloprogram_link");
            roadWorkActivityProperties.aggloprogramGeneration = reader.GetNullableInt("aggloprogram_generation");
            roadWorkActivityProperties.aggloprogramMeasureNumber = reader.GetStringOrEmpty("aggloprogram_measure_number");
            roadWorkActivityProperties.aggloprogramAreCode = reader.GetStringOrEmpty("aggloprogram_are_code");
            roadWorkActivityProperties.aggloprogramAreDescription = reader.GetStringOrEmpty("aggloprogram_are_description");
            roadWorkActivityProperties.aggloprogramDueDate = reader.GetNullableDateTime("aggloprogram_due_date");
            roadWorkActivityProperties.aggloprogramCostTotal = reader.GetNullableDecimal("aggloprogram_cost_total");
            roadWorkActivityProperties.aggloprogramCostCanton = reader.GetNullableDecimal("aggloprogram_cost_canton");
            roadWorkActivityProperties.aggloprogramComment = reader.GetStringOrEmpty("aggloprogram_comment");

            // Prestudy
            roadWorkActivityProperties.prestudy = reader.GetBooleanOrFalse("prestudy");
            roadWorkActivityProperties.prestudySks = reader.GetBooleanOrFalse("prestudy_sks");
            roadWorkActivityProperties.prestudyDuration = reader.GetStringOrEmpty("prestudy_duration");
            roadWorkActivityProperties.prestudyContractor = reader.GetStringOrEmpty("prestudy_contractor");
            roadWorkActivityProperties.prestudyDetail = reader.GetStringOrEmpty("prestudy_detail");
            roadWorkActivityProperties.prestudyVkErConfirmed = reader.GetBooleanOrFalse("prestudy_vk_er_confirmed");
            roadWorkActivityProperties.datePrestudyVkErConfirmed = reader.GetNullableDateTime("date_prestudy_vk_er_confirmed");
            roadWorkActivityProperties.prestudyVkErNumber = reader.GetNullableLong("prestudy_vk_er_number");

            // Affected entities
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

            // Private entities
            roadWorkActivityProperties.privateEntityAffected = reader.GetBooleanOrFalse("private_entity_affected");
            roadWorkActivityProperties.privateEntityExtent = reader.GetStringOrEmpty("private_entity_extent");
            roadWorkActivityProperties.privateEntityRequirements = reader.GetStringOrEmpty("private_entity_requirements");
            roadWorkActivityProperties.privateEntityAcquisition = reader.GetBooleanOrFalse("private_entity_acquisition");
            roadWorkActivityProperties.privateEntityIsInitiator = reader.GetBooleanOrFalse("private_entity_is_initiator");

            // Provis (Abacus)
            roadWorkActivityProperties.erpNumber = reader.GetNullableLong("erp_number");

            // Ressources
            roadWorkActivityProperties.staffResourcesAprConfirmed = reader.GetBooleanOrFalse("staff_resources_apr_confirmed");
            roadWorkActivityProperties.dateStaffResourcesAprConfirmed = reader.GetNullableDateTime("date_staff_resources_apr_confirmed");
            roadWorkActivityProperties.costEstimateAprConfirmed = reader.GetBooleanOrFalse("cost_estimate_apr_confirmed");
            roadWorkActivityProperties.dateCostEstimateAprConfirmed = reader.GetNullableDateTime("date_cost_estimate_apr_confirmed");

            // Engineering contract
            roadWorkActivityProperties.coreDrillingContracted = reader.GetBooleanOrFalse("core_drilling_contracted");
            roadWorkActivityProperties.dateCoreDrillingContracted = reader.GetNullableDateTime("date_core_drilling_contracted");
            roadWorkActivityProperties.quotesRequested = reader.GetBooleanOrFalse("quotes_requested");
            roadWorkActivityProperties.dateQuotesRequested = reader.GetNullableDateTime("date_quotes_requested");
            roadWorkActivityProperties.quotesReviewed = reader.GetBooleanOrFalse("quotes_reviewed");
            roadWorkActivityProperties.dateQuotesReviewed = reader.GetNullableDateTime("date_quotes_reviewed");
            roadWorkActivityProperties.afmChecked = reader.GetBooleanOrFalse("afm_checked");
            roadWorkActivityProperties.dateAfmChecked = reader.GetNullableDateTime("date_afm_checked");

            // Approval and filing
            roadWorkActivityProperties.preReviewedByCf = reader.GetBooleanOrFalse("pre_reviewed_by_cf");
            roadWorkActivityProperties.datePreReviewedByCf = reader.GetNullableDateTime("date_pre_reviewed_by_cf");
            roadWorkActivityProperties.preReviewedByRd = reader.GetBooleanOrFalse("pre_reviewed_by_rd");
            roadWorkActivityProperties.datePreReviewedByRd = reader.GetNullableDateTime("date_pre_reviewed_by_rd");
            roadWorkActivityProperties.formallyApproved = reader.GetBooleanOrFalse("formally_approved");
            roadWorkActivityProperties.dateFormallyApproved = reader.GetNullableDateTime("date_formally_approved");
            roadWorkActivityProperties.filedInFabasoft = reader.GetBooleanOrFalse("filed_in_fabasoft");
            roadWorkActivityProperties.dateFiledInFabasoft = reader.GetNullableDateTime("date_filed_in_fabasoft");
            roadWorkActivityProperties.gisUpdated = reader.GetBooleanOrFalse("gis_updated");
            roadWorkActivityProperties.dateGisUpdated = reader.GetNullableDateTime("date_gis_updated");

            // Oks
            roadWorkActivityProperties.isOksActiveLastModified = reader.GetNullableDateTime("oks_active_last_modified");

            // Dates
            roadWorkActivityProperties.dateDesignAssignmentIssued = reader.GetNullableDateTime("date_design_assignment_issued");
            roadWorkActivityProperties.dateAprDesignCompletion = reader.GetNullableDateTime("date_apr_design_completion");
            roadWorkActivityProperties.dateAprConstructionCompletion = reader.GetNullableDateTime("date_apr_construction_completion");
            roadWorkActivityProperties.datePrepareEdcStart = reader.GetNullableDateTime("date_prepare_edc_start");
            roadWorkActivityProperties.datePrepareEdcEnd = reader.GetNullableDateTime("date_prepare_edc_end");
            roadWorkActivityProperties.dateHandoverToApk = reader.GetNullableDateTime("date_handover_to_apk");
            roadWorkActivityProperties.dateHandoverToApr = reader.GetNullableDateTime("date_handover_to_apr");
            roadWorkActivityProperties.dateRequestDesignBudget = reader.GetNullableDateTime("date_request_design_budget");
            roadWorkActivityProperties.dateProjectApprovalStart = reader.GetNullableDateTime("date_project_approval_start");
            roadWorkActivityProperties.dateProjectApprovalEnd = reader.GetNullableDateTime("date_project_approval_end");
            roadWorkActivityProperties.dateConstructionBudgetApprovalStart = reader.GetNullableDateTime("date_construction_budget_approval_start");
            roadWorkActivityProperties.dateConstructionBudgetApprovalEnd = reader.GetNullableDateTime("date_construction_budget_approval_end");
            roadWorkActivityProperties.dateSubmissionStart = reader.GetNullableDateTime("date_submission_start");
            roadWorkActivityProperties.dateSubmissionEnd = reader.GetNullableDateTime("date_submission_end");
            roadWorkActivityProperties.dateStartOfConstructionReal = reader.GetNullableDateTime("date_start_of_construction_real");
            roadWorkActivityProperties.dateEndOfConstructionReal = reader.GetNullableDateTime("date_end_of_construction_real");
            roadWorkActivityProperties.dateFinalPavementStart = reader.GetNullableDateTime("date_final_pavement_start");
            roadWorkActivityProperties.dateFinalPavementEnd = reader.GetNullableDateTime("date_final_pavement_end");
            roadWorkActivityProperties.dateProjectBudgetFinalized = reader.GetNullableDateTime("date_project_budget_finalized");
            
            return roadWorkActivityProperties;
        }

        /// <summary>
        /// Adds the parameters the activity command(partial only!).
        /// </summary>
        /// <param name="parameters"></param>
        internal static void AddParameters(NpgsqlParameterCollection parameters, RoadWorkActivityProperties roadWorkActivityProperties)
        {
            // Additional attributes for journal
            parameters.AddWithValue("@planned_tasks", HelperFunctions.ToDbValue(roadWorkActivityProperties.plannedTasks));
            parameters.AddWithValue("@constraints_dependencies", HelperFunctions.ToDbValue(roadWorkActivityProperties.constraintsDependencies));
            parameters.AddWithValue("@acquisition_planned", HelperFunctions.ToDbValue(roadWorkActivityProperties.acquisitionPlanned));

            // Aggloprogramm
            parameters.AddWithValue("@part_of_aggloprogram", HelperFunctions.ToDbValue(roadWorkActivityProperties.partOfAggloprogram));
            parameters.AddWithValue("@aggloprogram_link", HelperFunctions.ToDbValue(roadWorkActivityProperties.aggloprogramLink));
            parameters.AddWithValue("@aggloprogram_generation", HelperFunctions.ToDbValue(roadWorkActivityProperties.aggloprogramGeneration));
            parameters.AddWithValue("@aggloprogram_measure_number", HelperFunctions.ToDbValue(roadWorkActivityProperties.aggloprogramMeasureNumber));
            parameters.AddWithValue("@aggloprogram_are_code", HelperFunctions.ToDbValue(roadWorkActivityProperties.aggloprogramAreCode));
            parameters.AddWithValue("@aggloprogram_are_description", HelperFunctions.ToDbValue(roadWorkActivityProperties.aggloprogramAreDescription));
            parameters.AddWithValue("@aggloprogram_due_date", HelperFunctions.ToDbValue(roadWorkActivityProperties.aggloprogramDueDate));
            parameters.AddWithValue("@aggloprogram_cost_total", HelperFunctions.ToDbValue(roadWorkActivityProperties.aggloprogramCostTotal));
            parameters.AddWithValue("@aggloprogram_cost_canton", HelperFunctions.ToDbValue(roadWorkActivityProperties.aggloprogramCostCanton));
            parameters.AddWithValue("@aggloprogram_comment", HelperFunctions.ToDbValue(roadWorkActivityProperties.aggloprogramComment));

            // Prestudy
            parameters.AddWithValue("@prestudy", HelperFunctions.ToDbValue(roadWorkActivityProperties.prestudy));
            parameters.AddWithValue("@prestudy_sks", HelperFunctions.ToDbValue(roadWorkActivityProperties.prestudySks));
            parameters.AddWithValue("@prestudy_duration", HelperFunctions.ToDbValue(roadWorkActivityProperties.prestudyDuration));
            parameters.AddWithValue("@prestudy_contractor", HelperFunctions.ToDbValue(roadWorkActivityProperties.prestudyContractor));
            parameters.AddWithValue("@prestudy_detail", HelperFunctions.ToDbValue(roadWorkActivityProperties.prestudyDetail));
            parameters.AddWithValue("@prestudy_vk_er_confirmed", HelperFunctions.ToDbValue(roadWorkActivityProperties.prestudyVkErConfirmed));
            parameters.AddWithValue("@date_prestudy_vk_er_confirmed", HelperFunctions.ToDbValue(roadWorkActivityProperties.datePrestudyVkErConfirmed));
            parameters.AddWithValue("@prestudy_vk_er_number", HelperFunctions.ToDbValue(roadWorkActivityProperties.prestudyVkErNumber));

            // Affected entities
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

            // Private entities
            parameters.AddWithValue("@private_entity_affected", HelperFunctions.ToDbValue(roadWorkActivityProperties.privateEntityAffected));
            parameters.AddWithValue("@private_entity_extent", HelperFunctions.ToDbValue(roadWorkActivityProperties.privateEntityExtent));
            parameters.AddWithValue("@private_entity_requirements", HelperFunctions.ToDbValue(roadWorkActivityProperties.privateEntityRequirements));
            parameters.AddWithValue("@private_entity_acquisition", HelperFunctions.ToDbValue(roadWorkActivityProperties.privateEntityAcquisition));
            parameters.AddWithValue("@private_entity_is_initiator", HelperFunctions.ToDbValue(roadWorkActivityProperties.privateEntityIsInitiator));

            // Provis (Abacus)
            parameters.AddWithValue("@erp_number", HelperFunctions.ToDbValue(roadWorkActivityProperties.erpNumber));

            // Ressources
            parameters.AddWithValue("@staff_resources_apr_confirmed", HelperFunctions.ToDbValue(roadWorkActivityProperties.staffResourcesAprConfirmed));
            parameters.AddWithValue("@date_staff_resources_apr_confirmed", HelperFunctions.ToDbValue(roadWorkActivityProperties.dateStaffResourcesAprConfirmed));
            parameters.AddWithValue("@cost_estimate_apr_confirmed", HelperFunctions.ToDbValue(roadWorkActivityProperties.costEstimateAprConfirmed));
            parameters.AddWithValue("@date_cost_estimate_apr_confirmed", HelperFunctions.ToDbValue(roadWorkActivityProperties.dateCostEstimateAprConfirmed));

            // Engineering contract
            parameters.AddWithValue("@core_drilling_contracted", HelperFunctions.ToDbValue(roadWorkActivityProperties.coreDrillingContracted));
            parameters.AddWithValue("@date_core_drilling_contracted", HelperFunctions.ToDbValue(roadWorkActivityProperties.dateCoreDrillingContracted));
            parameters.AddWithValue("@quotes_requested", HelperFunctions.ToDbValue(roadWorkActivityProperties.quotesRequested));
            parameters.AddWithValue("@date_quotes_requested", HelperFunctions.ToDbValue(roadWorkActivityProperties.dateQuotesRequested));
            parameters.AddWithValue("@quotes_reviewed", HelperFunctions.ToDbValue(roadWorkActivityProperties.quotesReviewed));
            parameters.AddWithValue("@date_quotes_reviewed", HelperFunctions.ToDbValue(roadWorkActivityProperties.dateQuotesReviewed));
            parameters.AddWithValue("@afm_checked", HelperFunctions.ToDbValue(roadWorkActivityProperties.afmChecked));
            parameters.AddWithValue("@date_afm_checked", HelperFunctions.ToDbValue(roadWorkActivityProperties.dateAfmChecked));

            // Approval and filing
            parameters.AddWithValue("@pre_reviewed_by_cf", HelperFunctions.ToDbValue(roadWorkActivityProperties.preReviewedByCf));
            parameters.AddWithValue("@date_pre_reviewed_by_cf", HelperFunctions.ToDbValue(roadWorkActivityProperties.datePreReviewedByCf));
            parameters.AddWithValue("@pre_reviewed_by_rd", HelperFunctions.ToDbValue(roadWorkActivityProperties.preReviewedByRd));
            parameters.AddWithValue("@date_pre_reviewed_by_rd", HelperFunctions.ToDbValue(roadWorkActivityProperties.datePreReviewedByRd));
            parameters.AddWithValue("@formally_approved", HelperFunctions.ToDbValue(roadWorkActivityProperties.formallyApproved));
            parameters.AddWithValue("@date_formally_approved", HelperFunctions.ToDbValue(roadWorkActivityProperties.dateFormallyApproved));
            parameters.AddWithValue("@filed_in_fabasoft", HelperFunctions.ToDbValue(roadWorkActivityProperties.filedInFabasoft));
            parameters.AddWithValue("@date_filed_in_fabasoft", HelperFunctions.ToDbValue(roadWorkActivityProperties.dateFiledInFabasoft));
            parameters.AddWithValue("@gis_updated", HelperFunctions.ToDbValue(roadWorkActivityProperties.gisUpdated));
            parameters.AddWithValue("@date_gis_updated", HelperFunctions.ToDbValue(roadWorkActivityProperties.dateGisUpdated));

            // Oks
            parameters.AddWithValue("@oks_active_last_modified", HelperFunctions.ToDbValue(roadWorkActivityProperties.isOksActiveLastModified));

            // Dates
            parameters.AddWithValue("@date_design_assignment_issued", HelperFunctions.ToDbValue(roadWorkActivityProperties.dateDesignAssignmentIssued));
            parameters.AddWithValue("@date_apr_design_completion", HelperFunctions.ToDbValue(roadWorkActivityProperties.dateAprDesignCompletion));
            parameters.AddWithValue("@date_apr_construction_completion", HelperFunctions.ToDbValue(roadWorkActivityProperties.dateAprConstructionCompletion));
            parameters.AddWithValue("@date_quotes_requested", HelperFunctions.ToDbValue(roadWorkActivityProperties.dateQuotesRequested));
            parameters.AddWithValue("@date_quotes_reviewed", HelperFunctions.ToDbValue(roadWorkActivityProperties.dateQuotesReviewed));
            parameters.AddWithValue("@date_prepare_edc_start", HelperFunctions.ToDbValue(roadWorkActivityProperties.datePrepareEdcStart));
            parameters.AddWithValue("@date_prepare_edc_end", HelperFunctions.ToDbValue(roadWorkActivityProperties.datePrepareEdcEnd));
            parameters.AddWithValue("@date_handover_to_apk", HelperFunctions.ToDbValue(roadWorkActivityProperties.dateHandoverToApk));
            parameters.AddWithValue("@date_handover_to_apr", HelperFunctions.ToDbValue(roadWorkActivityProperties.dateHandoverToApr));
            parameters.AddWithValue("@date_request_design_budget", HelperFunctions.ToDbValue(roadWorkActivityProperties.dateRequestDesignBudget));
            parameters.AddWithValue("@date_project_approval_start", HelperFunctions.ToDbValue(roadWorkActivityProperties.dateProjectApprovalStart));
            parameters.AddWithValue("@date_project_approval_end", HelperFunctions.ToDbValue(roadWorkActivityProperties.dateProjectApprovalEnd));
            parameters.AddWithValue("@date_construction_budget_approval_start", HelperFunctions.ToDbValue(roadWorkActivityProperties.dateConstructionBudgetApprovalStart));
            parameters.AddWithValue("@date_construction_budget_approval_end", HelperFunctions.ToDbValue(roadWorkActivityProperties.dateConstructionBudgetApprovalEnd));
            parameters.AddWithValue("@date_submission_start", HelperFunctions.ToDbValue(roadWorkActivityProperties.dateSubmissionStart));
            parameters.AddWithValue("@date_submission_end", HelperFunctions.ToDbValue(roadWorkActivityProperties.dateSubmissionEnd));
            parameters.AddWithValue("@date_start_of_construction_real", HelperFunctions.ToDbValue(roadWorkActivityProperties.dateStartOfConstructionReal));
            parameters.AddWithValue("@date_end_of_construction_real", HelperFunctions.ToDbValue(roadWorkActivityProperties.dateEndOfConstructionReal));
            parameters.AddWithValue("@date_final_pavement_start", HelperFunctions.ToDbValue(roadWorkActivityProperties.dateFinalPavementStart));
            parameters.AddWithValue("@date_final_pavement_end", HelperFunctions.ToDbValue(roadWorkActivityProperties.dateFinalPavementEnd));
            parameters.AddWithValue("@date_project_budget_finalized", HelperFunctions.ToDbValue(roadWorkActivityProperties.dateProjectBudgetFinalized));
        }
    }
}
