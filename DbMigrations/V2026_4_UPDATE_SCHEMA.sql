-- VERSION 2026.4 ---

-- #608, #609 - Add additional attributes to needs (GEOBOX AG - Simon Meyer, 16.04.2026) 
--ALTER TABLE IF EXISTS wtb_ssp_roadworkneeds ADD COLUMN construction_duration integer;
ALTER TABLE IF EXISTS wtb_ssp_roadworkneeds ADD COLUMN acquisition_planned character varying(40);

-- #617 - Add "Aggloprogramm" to activities (GEOBOX AG - Simon Meyer, 23.04.2026)
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN part_of_aggloprogram boolean;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN aggloprogram_generation integer;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN aggloprogram_are_code character varying(40);
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN aggloprogram_are_description character varying(255);
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN aggloprogram_due_date date;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN aggloprogram_cost_total numeric(20,2);
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN aggloprogram_cost_canton numeric(20,2);

-- #622 - Add affected entities to activities (GEOBOX AG - Simon Meyer, 23.04.2026)
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN bus_stops_shelters_affected boolean;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN structures_affected boolean;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN roadDrainage_affected boolean;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN houseConnections_affected boolean;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN wasteFacilities_affected boolean;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN technical_installations_affected boolean;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN trees_affected boolean;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN street_furniture_affected boolean;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN urban_climate_affected boolean;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN subject_to_depaving boolean;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN pedestrians_cycling_affected boolean;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN disability_equality_affected boolean;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN traffic_regulation_affected boolean;

-- #623 - Add private entities to activities (GEOBOX AG - Simon Meyer, 23.04.2026)
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN private_entity_affected boolean;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN private_entity_extent character varying(255);
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN private_entity_requirements character varying(255);
--ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN private_entity_acquisition character varying(255); Changed in #644 >> V2026_5
--ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN private_entity_is_initiator character varying(255); Changed in #644 >> V2026_5

-- #624 - Add provis (Abacus) to activities (GEOBOX AG - Simon Meyer, 23.04.2026)
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN erp_number bigint;

-- #625 - Add ressources to activities (GEOBOX AG - Simon Meyer, 23.04.2026)
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN staff_resources_apr_confirmed date;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN cost_estimate_apr_confirmed date;

-- #626 - Add engineering contract to activities (GEOBOX AG - Simon Meyer, 23.04.2026)
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN core_drilling_contracted boolean;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN quotes_requested boolean;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN quotes_reviewed boolean;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN apr_checked boolean;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN afm_checked boolean;

-- #626 - Add approval and filing to activities (GEOBOX AG - Simon Meyer, 23.04.2026)
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN cf_done boolean;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN rd_done boolean;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN approved boolean;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN fabasoft_done boolean;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN gis_updated boolean;

-- #610, #611 - Add road work parameters table (GEOBOX AG - Simon Meyer, 16.04.2026)
CREATE TABLE IF NOT EXISTS wtb_ssp_roadwork_approvals
(
    uuid uuid NOT NULL,
    uuid_roadwork_activity uuid,
    uuid_roadwork_need uuid,
    approval_required boolean,
    strg_approval_required boolean,
    bafu_approval_required boolean,
    lsv_approval_required boolean,
    ssv_approval_required boolean,
    wwg_approval_required boolean,
    eri_approval_required boolean,
    pbg_approval_required boolean,
    ebg_approval_required boolean,
    awel_approval_required boolean,
    esti_approval_required boolean,
    other_approval_required boolean,
    other_approval_details character varying(255),
    CONSTRAINT wtb_ssp_roadwork_approvals_pkey PRIMARY KEY (uuid),
    CONSTRAINT wtb_ssp_roadwork_approvals_uuid_roadwork_activity_fkey FOREIGN KEY (uuid_roadwork_activity)
        REFERENCES wtb_ssp_roadworkactivities (uuid) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE,
    CONSTRAINT wtb_ssp_roadwork_approvals_uuid_roadwork_need_fkey FOREIGN KEY (uuid_roadwork_need)
        REFERENCES wtb_ssp_roadworkneeds (uuid) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE,
    CONSTRAINT wtb_ssp_roadwork_approvals_exactly_one_parent_chk CHECK (uuid_roadwork_activity IS NOT NULL AND uuid_roadwork_need IS NULL OR uuid_roadwork_activity IS NULL AND uuid_roadwork_need IS NOT NULL) NOT VALID
);