-- VERSION 2026.6 ---

-- DANGER: The following part changes data, make sure to backup before execution
-- #643 - Migrate values in wtb_ssp_roadworkactivities.projectkind (GEOBOX AG - Simon Meyer, 28.05.2026)
UPDATE wtb_ssp_roadworkactivities SET projectkind = 'ROAD_PROJECT' WHERE projectkind = 'ROAD_NEW_REGIONAL';
UPDATE wtb_ssp_roadworkactivities SET projectkind = 'ROAD_PROJECT' WHERE projectkind = 'ROAD_NEW_COMMUNAL';
UPDATE wtb_ssp_roadworkactivities SET projectkind = 'ROAD_PROJECT' WHERE projectkind = 'ROAD_MAINTENANCE_REGIONAL';
UPDATE wtb_ssp_roadworkactivities SET projectkind = 'ROAD_PROJECT' WHERE projectkind = 'ROAD_MAINTENANCE_COMMUNAL';
UPDATE wtb_ssp_roadworkactivities SET projectkind = 'UTILITY_CONSTRUCTION' WHERE projectkind = 'TRENCH_WITH_RESURFACING';

-- #649 Add column implementation_by_third
ALTER TABLE IF EXISTS wikis.wtb_ssp_roadworkactivities ADD COLUMN implementation_by_third boolean;

-- #649 Add responsibilities table
CREATE TABLE wikis.wtb_ssp_activity_responsibilities
(
    uuid uuid NOT NULL,
    uuid_roadwork_activity uuid,
    uuid_organisationalunit uuid,
    uuid_user uuid,
    responsibility_type character varying(40),
    phase character varying(40),
    sort_order smallint,
    CONSTRAINT wtb_ssp_activity_responsibilities_pkey PRIMARY KEY (uuid),
    CONSTRAINT wtb_ssp_activity_responsibilities_uuid_roadwork_activity_fkey FOREIGN KEY (uuid_roadwork_activity)
        REFERENCES wtb_ssp_roadworkactivities (uuid) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE,
    CONSTRAINT wtb_ssp_activity_responsibilities_uuid_organisationalunit_fkey FOREIGN KEY (uuid_organisationalunit)
        REFERENCES wikis.wtb_ssp_organisationalunits (uuid) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
        NOT VALID,
    CONSTRAINT wtb_ssp_activity_responsibilities_uuid_user_fkey FOREIGN KEY (uuid_user)
        REFERENCES wikis.wtb_ssp_users (uuid) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
        NOT VALID
);