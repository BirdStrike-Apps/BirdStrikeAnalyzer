create table if not exists public.sample_items (
    id uuid primary key default gen_random_uuid(),
    name text not null
);

alter table public.sample_items enable row level security;
grant select on public.sample_items to anon, authenticated;

drop policy if exists "Anyone can read sample items" on public.sample_items;
create policy "Anyone can read sample items"
    on public.sample_items for select to anon, authenticated using (true);
