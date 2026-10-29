-- VERSION 2026.13 ---

-- #647 - Remove unused fields (GEOBOX AG - Simon Meyer, 26.10.2026)
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities DROP COLUMN IF EXISTS apr_checked;

-- #647 - Rename fields for journal, add date fields (GEOBOX AG - Simon Meyer, 27.10.2026)
-- date_staff_resources_apr_confirmed
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities RENAME staff_resources_apr_confirmed TO date_staff_resources_apr_confirmed;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN staff_resources_apr_confirmed boolean;
-- cost_estimate_apr_confirmed
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities RENAME cost_estimate_apr_confirmed TO date_cost_estimate_apr_confirmed;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN cost_estimate_apr_confirmed boolean;
-- core_drilling_contracted
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN date_core_drilling_contracted date;
-- afm_checked
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN date_afm_checked date;
-- pre_reviewed_by_cf
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities RENAME cf_done TO pre_reviewed_by_cf;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN date_pre_reviewed_by_cf date;
-- pre_reviewed_by_rd
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities RENAME rd_done TO pre_reviewed_by_rd;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN date_pre_reviewed_by_rd date;
-- formally_approved
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities RENAME approved TO formally_approved;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN date_formally_approved date;
-- filed_in_fabasoft
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities RENAME fabasoft_done TO filed_in_fabasoft;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN date_filed_in_fabasoft date;
-- gis_updated
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN date_gis_updated date;
-- prestudy_vk_er_confirmed
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities RENAME prestudy_vk_er_confirmed TO date_prestudy_vk_er_confirmed;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN prestudy_vk_er_confirmed boolean;
