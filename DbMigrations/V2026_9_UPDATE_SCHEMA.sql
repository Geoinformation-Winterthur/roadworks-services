-- VERSION 2026.9 ---

-- #663 - Split prestudy: Sitzungen (SKS) and Journal (GEOBOX AG - Simon Meyer, 10.07.2026)
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN prestudy_sks boolean;
UPDATE wtb_ssp_roadworkactivities SET prestudy_sks = prestudy;

-- #667 - Add oks active modification date (GEOBOX AG - Simon Meyer, 10.07.2026)
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN oks_active_last_modified date;

-- #650 Remove "Begehrensäusserung § 45" and "Infoversand" incl fields (GEOBOX AG - Simon Meyer, 06.08.2026)
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities DROP COLUMN IF EXISTS is_desire;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities DROP COLUMN IF EXISTS date_desire_start;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities DROP COLUMN IF EXISTS date_desire_end;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities DROP COLUMN IF EXISTS date_info_start;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities DROP COLUMN IF EXISTS date_info_end;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities DROP COLUMN IF EXISTS date_info_close;

-- #650 Consolidate date_of_acceptance and date_guarantee to date_guarantee (GEOBOX AG - Simon Meyer, 07.08.2026)
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities DROP COLUMN IF EXISTS date_of_acceptance;

-- #650 Add new fields for "Termine" (GEOBOX AG - Simon Meyer, 07.08.2026)
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN date_design_assignment_issued date;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN date_apr_design_completion date;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN date_apr_construction_completion date;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN date_quotes_requested date;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN date_quotes_reviewed date;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN date_prepare_edc_start date;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN date_prepare_edc_end date;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN date_handover_to_apk date;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN date_handover_to_apr date;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN date_request_design_budget date;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN date_project_approval_start date;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN date_project_approval_end date;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN date_construction_budget_approval_start date;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN date_construction_budget_approval_end date;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN date_submission_start date;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN date_submission_end date;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN date_start_of_construction_real date;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN date_end_of_construction_real date;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN date_final_pavement_start date;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN date_final_pavement_end date;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN date_project_budget_finalized date;