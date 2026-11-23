-- Apply once in Supabase SQL Editor after assigning app_metadata.role = 'analyst'
-- to the authorized Supabase Auth account used by the analyst app.
-- The existing reporter own-row policy remains in place; this adds read access
-- only to authenticated users whose trusted app_metadata marks them as analysts.
create policy "Analysts can view all submissions"
    on public.reporter_submissions
    as permissive
    for select
    to authenticated
    using ((auth.jwt() -> 'app_metadata' ->> 'role') = 'analyst');
