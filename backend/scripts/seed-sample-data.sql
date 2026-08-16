-- ============================================================================
-- UComplain — sample data for the Admin Dashboard (reporters, reports, and
-- their full verification/assignment/status history + a few audit log rows).
--
-- Run this once in the Supabase SQL Editor against your PRODUCTION database.
-- It mirrors the same fictional Sierra Leonean scenario the backend's own
-- DevelopmentSeeder.cs uses locally — that seeder only ever runs against a
-- Development database, never Production, so this is the Production-safe
-- equivalent for populating an otherwise-empty dashboard.
--
-- Prerequisites:
--   - At least one row in admin_users (your own login — already true if
--     you can reach the Admin Portal at all).
--
-- Safe to re-run: every INSERT below is idempotent (categories key off their
-- unique Slug; reporters/reports key off a fixed UUID checked before the
-- second block does anything).
-- ============================================================================

-- --- Categories (matches the icon keys the mobile app's category picker expects) ---
INSERT INTO incident_categories
  ("Id","Name","Description","DefaultPriority","SlaHours","DisplayOrder","Slug","IconKey","IsActive","CreatedAt","UpdatedAt")
VALUES
  (gen_random_uuid(), 'Blocked Drainage',    'Flooded footpaths, blocked culverts, and drainage overflow.',              'Medium',   48, 1, 'drainage', 'drainage', true, now(), now()),
  (gen_random_uuid(), 'Road Damage',         'Potholes, collapsed retaining walls, and road traffic hazards.',           'Medium',   72, 2, 'road',     'road',     true, now(), now()),
  (gen_random_uuid(), 'Power Outage',        'Extended electricity outages affecting a neighbourhood or facility.',      'Low',      72, 3, 'power',    'power',    true, now(), now()),
  (gen_random_uuid(), 'Water Supply',        'Broken pipes, prolonged outages, or unsafe/contaminated water supply.',    'Medium',   24, 4, 'water',    'water',    true, now(), now()),
  (gen_random_uuid(), 'Security Incident',   'Theft, assault, or other public safety and security concerns.',           'Critical',  6, 5, 'crime',    'crime',    true, now(), now()),
  (gen_random_uuid(), 'Fire',                'Structure fires, market fires, and bushfires near residential areas.',    'Critical',  4, 6, 'fire',     'fire',     true, now(), now()),
  (gen_random_uuid(), 'Medical Emergency',   'Suspected disease outbreaks and urgent public health concerns.',          'Critical',  4, 7, 'medical',  'medical',  true, now(), now()),
  (gen_random_uuid(), 'Waste & Sanitation',  'Uncollected waste, blocked latrines, and sanitation hazards.',            'Low',      96, 8, 'waste',    'waste',    true, now(), now())
ON CONFLICT ("Slug") WHERE "Slug" IS NOT NULL DO NOTHING;

DO $$
DECLARE
  v_admin_id uuid;
  v_admin2_id uuid;
  v_fallback_cat uuid;
  v_cat_drainage uuid;
  v_cat_fire uuid;
  v_cat_road uuid;
  v_cat_water uuid;
  v_cat_crime uuid;
  v_cat_power uuid;
  v_now timestamptz := now();

  -- Reporters — fixed IDs so later INSERTs can reference them directly.
  r1 uuid := 'a1000000-0000-4000-8000-000000000001';
  r2 uuid := 'a1000000-0000-4000-8000-000000000002';
  r3 uuid := 'a1000000-0000-4000-8000-000000000003';
  r4 uuid := 'a1000000-0000-4000-8000-000000000004';
  r5 uuid := 'a1000000-0000-4000-8000-000000000005';
  r6 uuid := 'a1000000-0000-4000-8000-000000000006';
  r7 uuid := 'a1000000-0000-4000-8000-000000000007';
  r8 uuid := 'a1000000-0000-4000-8000-000000000008';

  -- Reports
  rep1 uuid := 'b2000000-0000-4000-8000-000000000001';
  rep2 uuid := 'b2000000-0000-4000-8000-000000000002';
  rep3 uuid := 'b2000000-0000-4000-8000-000000000003';
  rep4 uuid := 'b2000000-0000-4000-8000-000000000004';
  rep5 uuid := 'b2000000-0000-4000-8000-000000000005';
  rep6 uuid := 'b2000000-0000-4000-8000-000000000006';
  rep7 uuid := 'b2000000-0000-4000-8000-000000000007';
  rep8 uuid := 'b2000000-0000-4000-8000-000000000008';
  rep9 uuid := 'b2000000-0000-4000-8000-000000000009';
BEGIN
  -- --- Guards ---
  SELECT "Id" INTO v_admin_id FROM admin_users ORDER BY "CreatedAt" LIMIT 1;
  IF v_admin_id IS NULL THEN
    RAISE EXCEPTION 'No admin_users row exists — log into the Admin Portal at least once first.';
  END IF;

  SELECT "Id" INTO v_admin2_id FROM admin_users WHERE "Id" <> v_admin_id ORDER BY "CreatedAt" LIMIT 1;
  v_admin2_id := COALESCE(v_admin2_id, v_admin_id);

  SELECT "Id" INTO v_fallback_cat FROM incident_categories ORDER BY "DisplayOrder" LIMIT 1;
  IF v_fallback_cat IS NULL THEN
    RAISE EXCEPTION 'No incident_categories exist — add categories first (Admin Portal → Categories).';
  END IF;

  SELECT "Id" INTO v_cat_drainage FROM incident_categories WHERE "Slug" = 'drainage' LIMIT 1;
  SELECT "Id" INTO v_cat_fire FROM incident_categories WHERE "Slug" = 'fire' LIMIT 1;
  SELECT "Id" INTO v_cat_road FROM incident_categories WHERE "Slug" = 'road' LIMIT 1;
  SELECT "Id" INTO v_cat_water FROM incident_categories WHERE "Slug" = 'water' LIMIT 1;
  SELECT "Id" INTO v_cat_crime FROM incident_categories WHERE "Slug" = 'crime' LIMIT 1;
  SELECT "Id" INTO v_cat_power FROM incident_categories WHERE "Slug" = 'power' LIMIT 1;

  v_cat_drainage := COALESCE(v_cat_drainage, v_fallback_cat);
  v_cat_fire := COALESCE(v_cat_fire, v_fallback_cat);
  v_cat_road := COALESCE(v_cat_road, v_fallback_cat);
  v_cat_water := COALESCE(v_cat_water, v_fallback_cat);
  v_cat_crime := COALESCE(v_cat_crime, v_fallback_cat);
  v_cat_power := COALESCE(v_cat_power, v_fallback_cat);

  IF EXISTS (SELECT 1 FROM reporters WHERE "Id" = r1) THEN
    RAISE NOTICE 'Sample data already present — skipping (nothing changed).';
    RETURN;
  END IF;

  -- --- Reporters ---
  INSERT INTO reporters
    ("Id","WhatsAppNumberHash","MaskedContactReference","VerificationStatus","ConsentAt","IsRestricted","IsActive","CreatedAt","UpdatedAt")
  VALUES
    (r1, 'wa_hash_demo_0000000000000001', '+232 76 *** 214', 'Verified',     v_now - interval '180 days', false, true, v_now - interval '180 days', v_now - interval '180 days'),
    (r2, 'wa_hash_demo_0000000000000002', '+232 77 *** 558', 'Verified',     v_now - interval '150 days', false, true, v_now - interval '150 days', v_now - interval '150 days'),
    (r3, 'wa_hash_demo_0000000000000003', '+232 78 *** 391', 'Verified',     v_now - interval '120 days', false, true, v_now - interval '120 days', v_now - interval '120 days'),
    (r4, 'wa_hash_demo_0000000000000004', '+232 76 *** 742', 'Verified',     v_now - interval '90 days',  false, true, v_now - interval '90 days',  v_now - interval '90 days'),
    (r5, 'wa_hash_demo_0000000000000005', '+232 79 *** 103', 'Pending',      v_now - interval '10 days',  false, true, v_now - interval '10 days',  v_now - interval '10 days'),
    (r6, 'wa_hash_demo_0000000000000006', '+232 77 *** 876', 'Verified',     v_now - interval '60 days',  false, true, v_now - interval '60 days',  v_now - interval '60 days'),
    (r7, 'wa_hash_demo_0000000000000007', '+232 78 *** 429', 'FlaggedAbuse', v_now - interval '45 days',  true,  true, v_now - interval '45 days',  v_now - interval '45 days'),
    (r8, 'wa_hash_demo_0000000000000008', '+232 76 *** 615', 'Verified',     v_now - interval '200 days', false, true, v_now - interval '200 days', v_now - interval '200 days');

  -- --- Reports (CaseReference is DB-generated — omitted on purpose) ---
  INSERT INTO incident_reports
    ("Id","ReporterId","CategoryId","SourceChannel","Description","IncidentOccurredAt","LocationDescription","Latitude","Longitude",
     "Priority","VerificationStatus","CaseStatus","AssignedAdminId","ResolutionSummary","IsPubliclyVisible","CreatedAt","UpdatedAt","SubmittedAt")
  VALUES
    -- 1. Verified, resolved — flooding, full lifecycle
    (rep1, r1, v_cat_drainage, 'WhatsApp',
     'Heavy rain has flooded the main footpath and several homes near the stream in Kroo Bay; residents report waist-deep water overnight.',
     v_now - interval '20 days' - interval '2 hours', 'Kroo Bay, Freetown', -8.0142, -13.2412,
     'High', 'Verified', 'Resolved', v_admin_id,
     'Community drainage volunteers cleared the blocked culvert and the water level receded within 48 hours; two affected households received temporary shelter support from the ward councillor.',
     true, v_now - interval '20 days', v_now - interval '2 days', v_now - interval '20 days'),

    -- 2. Verified, assigned, in progress, critical fire
    (rep2, r2, v_cat_fire, 'WhatsApp',
     'Fire broke out at the eastern row of market stalls around 6pm; several traders'' stock destroyed, smoke visible from Congo Cross roundabout.',
     v_now - interval '2 days' - interval '2 hours', 'Congo Cross Market, Freetown', -8.4869, -13.2459,
     'Critical', 'Verified', 'InProgress', v_admin_id, NULL,
     true, v_now - interval '2 days', v_now - interval '6 hours', v_now - interval '2 days'),

    -- 3. Verified, under review, unassigned
    (rep3, r3, v_cat_road, 'WhatsApp',
     'Two vehicles collided near the Wellington roundabout, partially blocking the eastbound lane; one motorcyclist reported injured.',
     v_now - interval '1 days' - interval '2 hours', 'Wellington Road, Freetown', -8.4732, -13.1859,
     'High', 'Verified', 'UnderReview', NULL, NULL,
     true, v_now - interval '1 days', v_now - interval '1 days', v_now - interval '1 days'),

    -- 4. Verified, resolved — public health
    (rep4, r4, v_cat_water, 'WhatsApp',
     'Several households report stomach illness after using the community well; residents suspect contamination from the nearby latrine.',
     v_now - interval '20 days' - interval '2 hours', 'Susan''s Bay, Freetown', -8.4880, -13.2338,
     'High', 'Verified', 'Resolved', v_admin2_id,
     'Ministry of Health water team tested and chlorinated the well; illness cases stopped within a week.',
     true, v_now - interval '20 days', v_now - interval '2 days', v_now - interval '20 days'),

    -- 5. Pending verification — newly received
    (rep5, r5, v_cat_road, 'WhatsApp',
     'A retaining wall along Regent Road has developed a large crack after last week''s rain and looks close to collapse onto the road below.',
     v_now - interval '2 hours', 'Regent Road, Freetown', -8.4599, -13.2076,
     'Medium', 'Pending', 'VerificationPending', NULL, NULL,
     false, v_now, v_now, v_now),

    -- 6. Needs clarification — vague location
    (rep6, r6, v_cat_crime, 'WhatsApp',
     'There was a robbery near the beach last night, not sure exactly where, someone should check.',
     v_now - interval '4 days' - interval '2 hours', 'Near Lumley Beach, Freetown', NULL, NULL,
     'Critical', 'NeedsClarification', 'VerificationPending', NULL, NULL,
     false, v_now - interval '4 days', v_now - interval '3 days', v_now - interval '4 days'),

    -- 7. Suspected duplicate
    (rep7, r1, v_cat_water, 'WhatsApp',
     'No water from the standpipe on Aberdeen Road for the third day in a row.',
     v_now - interval '4 days' - interval '2 hours', 'Aberdeen, Freetown', -8.4869, -13.2822,
     'Medium', 'SuspectedDuplicate', 'Duplicate', NULL, NULL,
     false, v_now - interval '4 days', v_now - interval '3 days', v_now - interval '4 days'),

    -- 8. Rejected
    (rep8, r8, v_cat_power, 'WhatsApp',
     'Power has been out for one hour, please help.',
     v_now - interval '4 days' - interval '2 hours', 'Kenema Town', NULL, NULL,
     'Low', 'Rejected', 'Rejected', NULL, NULL,
     false, v_now - interval '4 days', v_now - interval '3 days', v_now - interval '4 days'),

    -- 9. Verified, assigned, awaiting action
    (rep9, r3, v_cat_water, 'WhatsApp',
     'Main pipeline burst near Waterloo junction, flooding the road and cutting supply to nearby streets.',
     v_now - interval '3 days' - interval '2 hours', 'Waterloo, Freetown', -8.3389, -13.0725,
     'Medium', 'Verified', 'Assigned', v_admin2_id, NULL,
     true, v_now - interval '3 days', v_now - interval '1 days', v_now - interval '3 days');

  -- --- Verification events ---
  INSERT INTO verification_events
    ("Id","IncidentReportId","ReporterId","VerificationMethod","Result","AttemptNumber","Notes","PerformedByAdminId","CreatedAt")
  VALUES
    (gen_random_uuid(), rep1, r1, 'AdminReview', 'Approved', 1, 'Location and description corroborated with a follow-up WhatsApp photo.', v_admin_id, v_now - interval '19 days'),
    (gen_random_uuid(), rep2, r2, 'AdminReview', 'Approved', 1, 'Corroborated by two independent WhatsApp reports from the same market area.', v_admin_id, v_now - interval '2 days'),
    (gen_random_uuid(), rep3, r3, 'AdminReview', 'Approved', 1, 'Details consistent with location and time reported.', NULL, v_now - interval '1 days'),
    (gen_random_uuid(), rep4, r4, 'AdminReview', 'Approved', 1, 'Confirmed via a short follow-up call with the reporter.', v_admin2_id, v_now - interval '20 days'),
    (gen_random_uuid(), rep6, r6, 'AdminReview', 'ClarificationRequested', 1, 'Reporter did not provide a specific location or time; requested more detail via WhatsApp follow-up.', v_admin2_id, v_now - interval '3 days'),
    (gen_random_uuid(), rep7, r1, 'AutomatedDuplicateCheck', 'MarkedDuplicate', 1, 'Matches an existing verified report for the same standpipe outage filed two days earlier.', v_admin2_id, v_now - interval '3 days'),
    (gen_random_uuid(), rep8, r8, 'AdminReview', 'Rejected', 1, 'Does not meet the reporting threshold — routine short outage already scheduled for maintenance.', v_admin2_id, v_now - interval '3 days'),
    (gen_random_uuid(), rep9, r3, 'AdminReview', 'Approved', 1, 'Confirmed via a short follow-up call with the reporter.', v_admin2_id, v_now - interval '3 days');

  -- --- Status history ---
  INSERT INTO status_histories
    ("Id","IncidentReportId","PreviousStatus","NewStatus","ChangedByAdminId","Notes","CreatedAt")
  VALUES
    (gen_random_uuid(), rep1, 'VerificationPending', 'UnderReview', v_admin_id, NULL, v_now - interval '19 days'),
    (gen_random_uuid(), rep1, 'UnderReview', 'Assigned', v_admin_id, 'Assigned for field follow-up.', v_now - interval '18 days'),
    (gen_random_uuid(), rep1, 'Assigned', 'InProgress', v_admin_id, 'Ward team notified.', v_now - interval '17 days'),
    (gen_random_uuid(), rep1, 'InProgress', 'Resolved', v_admin_id, 'Community drainage volunteers cleared the blocked culvert and the water level receded within 48 hours; two affected households received temporary shelter support from the ward councillor.', v_now - interval '2 days'),

    (gen_random_uuid(), rep2, 'VerificationPending', 'Assigned', v_admin_id, 'Fire — assigned immediately.', v_now - interval '2 days'),
    (gen_random_uuid(), rep2, 'Assigned', 'InProgress', v_admin_id, 'Fire service and ward disaster team dispatched.', v_now - interval '1 days'),

    (gen_random_uuid(), rep3, 'VerificationPending', 'UnderReview', v_admin_id, NULL, v_now - interval '1 days'),

    (gen_random_uuid(), rep4, 'VerificationPending', 'UnderReview', v_admin2_id, NULL, v_now - interval '19 days'),
    (gen_random_uuid(), rep4, 'UnderReview', 'Assigned', v_admin2_id, 'Assigned to utility liaison.', v_now - interval '17 days'),
    (gen_random_uuid(), rep4, 'Assigned', 'InProgress', v_admin2_id, NULL, v_now - interval '16 days'),
    (gen_random_uuid(), rep4, 'InProgress', 'Resolved', v_admin2_id, 'Ministry of Health water team tested and chlorinated the well; illness cases stopped within a week.', v_now - interval '2 days'),

    (gen_random_uuid(), rep8, 'VerificationPending', 'Rejected', v_admin2_id, 'Does not meet the reporting threshold.', v_now - interval '3 days'),

    (gen_random_uuid(), rep9, 'VerificationPending', 'UnderReview', v_admin2_id, NULL, v_now - interval '3 days'),
    (gen_random_uuid(), rep9, 'UnderReview', 'Assigned', v_admin2_id, 'Assigned to utility liaison.', v_now - interval '1 days');

  -- --- Assignments ---
  INSERT INTO report_assignments
    ("Id","IncidentReportId","AdminUserId","AssignedByAdminId","AssignedAt")
  VALUES
    (gen_random_uuid(), rep1, v_admin_id, v_admin_id, v_now - interval '18 days'),
    (gen_random_uuid(), rep2, v_admin_id, v_admin_id, v_now - interval '2 days'),
    (gen_random_uuid(), rep4, v_admin2_id, v_admin2_id, v_now - interval '17 days'),
    (gen_random_uuid(), rep9, v_admin2_id, v_admin2_id, v_now - interval '1 days');

  -- --- Internal notes ---
  INSERT INTO internal_notes
    ("Id","IncidentReportId","CreatedByAdminId","Content","CreatedAt","UpdatedAt")
  VALUES
    (gen_random_uuid(), rep1, v_admin_id, 'Confirmed resolution with community focal point by phone.', v_now - interval '2 days', v_now - interval '2 days'),
    (gen_random_uuid(), rep2, v_admin_id, 'Coordinating with the National Fire Force liaison for an update.', v_now - interval '6 hours', v_now - interval '6 hours');

  -- --- Audit log (verification decisions only — admin-creation events are skipped
  --     since they'd reference fictional admin rows this script doesn't create) ---
  INSERT INTO audit_logs
    ("Id","AdminUserId","Action","EntityType","EntityId","PreviousValueJson","NewValueJson","CreatedAt")
  VALUES
    (gen_random_uuid(), v_admin_id,  'VerificationDecisionRecorded', 'IncidentReport', rep1::text, '{"verificationStatus":"Pending"}', '{"verificationStatus":"Verified"}', v_now - interval '19 days'),
    (gen_random_uuid(), v_admin_id,  'VerificationDecisionRecorded', 'IncidentReport', rep2::text, '{"verificationStatus":"Pending"}', '{"verificationStatus":"Verified"}', v_now - interval '2 days'),
    (gen_random_uuid(), v_admin2_id, 'VerificationDecisionRecorded', 'IncidentReport', rep4::text, '{"verificationStatus":"Pending"}', '{"verificationStatus":"Verified"}', v_now - interval '19 days'),
    (gen_random_uuid(), v_admin2_id, 'VerificationDecisionRecorded', 'IncidentReport', rep6::text, '{"verificationStatus":"Pending"}', '{"verificationStatus":"NeedsClarification"}', v_now - interval '3 days'),
    (gen_random_uuid(), v_admin2_id, 'VerificationDecisionRecorded', 'IncidentReport', rep7::text, '{"verificationStatus":"Pending"}', '{"verificationStatus":"SuspectedDuplicate"}', v_now - interval '3 days'),
    (gen_random_uuid(), v_admin2_id, 'VerificationDecisionRecorded', 'IncidentReport', rep8::text, '{"verificationStatus":"Pending"}', '{"verificationStatus":"Rejected"}', v_now - interval '3 days'),
    (gen_random_uuid(), v_admin2_id, 'VerificationDecisionRecorded', 'IncidentReport', rep9::text, '{"verificationStatus":"Pending"}', '{"verificationStatus":"Verified"}', v_now - interval '3 days');

  RAISE NOTICE 'Sample data inserted: 8 reporters, 9 reports.';
END $$;
