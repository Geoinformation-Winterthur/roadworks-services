-- #659 Fields for comment and suspension (Sistieren) date (watson-dr, 10.08.2026)
ALTER TABLE wtb_ssp_roadworkactivities ADD COLUMN status_before_suspended varchar(50);
ALTER TABLE wtb_ssp_roadworkactivities ADD COLUMN comment_start_suspended text;


