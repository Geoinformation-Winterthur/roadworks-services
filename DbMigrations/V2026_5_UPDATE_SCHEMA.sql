-- VERSION 2026.5 ---

-- #616 - Add additional attributes to activities (GEOBOX AG - Simon Meyer, 11.05.2026)
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN planned_tasks character varying(255);
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN constraints_dependencies character varying(255);

-- #617 - Add "Aggloprogramm" to activities (GEOBOX AG - Simon Meyer, 20.05.2026)
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN aggloprogram_link character varying(255);

-- #621 - Add prestudy to activities (GEOBOX AG - Simon Meyer, 11.05.2026)
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN prestudy_required boolean;
--ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN prestudy_required_changed_after_sks boolean;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN prestudy_duration character varying(80);
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN prestudy_contractor character varying(255);
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN prestudy_detail character varying(255);
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN prestudy_vk_er_confirmed date;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN prestudy_vk_er_number bigint;

-- #644 (#623) - Add private entities to activities (GEOBOX AG - Simon Meyer, 11.05.2026)
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN private_entity_acquisition boolean;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN private_entity_is_initiator boolean;


-- #615 - Add journal entries table (GEOBOX AG - Simon Meyer, 11.05.2026)
CREATE SEQUENCE wikis.wtb_ssp_journal_entries_sort_order_seq
    INCREMENT 1
    START 1
    MINVALUE 1
    CACHE 1;

CREATE TABLE wikis.wtb_ssp_journal_entries
(
    uuid uuid NOT NULL,
    uuid_roadwork_activity uuid,
    content character varying(2048),
    created date,
    last_modified date,
    created_by uuid,
    sort_order bigint DEFAULT nextval('wtb_ssp_journal_entries_sort_order_seq'),
    CONSTRAINT wtb_ssp_journal_entries_pkey PRIMARY KEY (uuid),
    CONSTRAINT wtb_ssp_journal_entries_created_by_fkey FOREIGN KEY (created_by)
        REFERENCES wikis.wtb_ssp_users (uuid) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
        NOT VALID
);