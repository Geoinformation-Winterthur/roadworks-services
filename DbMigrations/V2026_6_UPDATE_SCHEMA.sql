-- VERSION 2026.6 ---

-- #643 - Migrate values in wtb_ssp_roadworkactivities.projectkind (GEOBOX AG - Simon Meyer, 28.05.2026)
UPDATE wtb_ssp_roadworkactivities SET projectkind = 'ROAD_PROJECT' WHERE projectkind = 'ROAD_NEW_REGIONAL';
UPDATE wtb_ssp_roadworkactivities SET projectkind = 'ROAD_PROJECT' WHERE projectkind = 'ROAD_NEW_COMMUNAL';
UPDATE wtb_ssp_roadworkactivities SET projectkind = 'ROAD_PROJECT' WHERE projectkind = 'ROAD_MAINTENANCE_REGIONAL';
UPDATE wtb_ssp_roadworkactivities SET projectkind = 'ROAD_PROJECT' WHERE projectkind = 'ROAD_MAINTENANCE_COMMUNAL';
UPDATE wtb_ssp_roadworkactivities SET projectkind = 'UTILITY_CONSTRUCTION' WHERE projectkind = 'TRENCH_WITH_RESURFACING';