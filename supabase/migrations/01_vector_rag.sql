create extension if not exists vector;

create table if not exists public.document_sections (
    id bigint generated always as identity primary key,
    source_type text not null,
    source_id text not null,
    chunk_index integer not null default 0,
    title text not null,
    content text not null,
    embedding vector(768),
    created_at timestamptz not null default now(),
    unique (source_type, source_id, chunk_index)
);

create index if not exists ix_document_sections_embedding_hnsw
    on public.document_sections using hnsw (embedding vector_cosine_ops)
    where embedding is not null;

create or replace function public.match_documents(
    query_embedding vector(768),
    match_threshold float,
    match_count int
)
returns table (id bigint, title text, content text, similarity float)
language sql
stable
as $$
    select d.id, d.title, d.content, 1 - (d.embedding <=> query_embedding) as similarity
    from public.document_sections d
    where d.embedding is not null
      and 1 - (d.embedding <=> query_embedding) >= match_threshold
      and (
          d.source_type <> 'event'
          or exists (
              select 1
              from public.eventos e
              join public.usuarios u on u.usuario_id = e.organizador_id
              where e.evento_id = d.source_id::integer
                and e.estado = 'Publicado'
                and e.estado_moderacion = 'Aprobado'
                and e.fecha_inicio > now()
                and u.estado = 'Activo'
                and e.aforo > (
                    select count(*)
                    from public.entradas t
                    where t.evento_id = e.evento_id
                      and t.estado in ('Valida', 'Utilizada')
                )
          )
      )
    order by d.embedding <=> query_embedding
    limit greatest(0, least(match_count, 20));
$$;

create or replace function public.queue_faq_document_section()
returns trigger
language plpgsql
as $$
begin
    if new.activo then
        insert into public.document_sections (source_type, source_id, title, content, embedding)
        values ('faq', new.faq_id::text, new.pregunta, new.pregunta || E'\n' || new.respuesta, null)
        on conflict (source_type, source_id, chunk_index) do update
            set title = excluded.title, content = excluded.content, embedding = null;
        delete from public.document_sections
        where source_type = 'faq' and source_id = new.faq_id::text and chunk_index > 0;
    else
        delete from public.document_sections
        where source_type = 'faq' and source_id = new.faq_id::text;
    end if;
    return new;
end;
$$;

drop trigger if exists trg_queue_faq_document_section on public.faqs;
create trigger trg_queue_faq_document_section
after insert or update of pregunta, respuesta, activo on public.faqs
for each row
execute function public.queue_faq_document_section();

create or replace function public.remove_faq_document_section()
returns trigger
language plpgsql
as $$
begin
    delete from public.document_sections
    where source_type = 'faq' and source_id = old.faq_id::text;
    return old;
end;
$$;

drop trigger if exists trg_remove_faq_document_section on public.faqs;
create trigger trg_remove_faq_document_section
after delete on public.faqs
for each row
execute function public.remove_faq_document_section();

insert into public.document_sections (source_type, source_id, title, content)
select 'faq', f.faq_id::text, f.pregunta, f.pregunta || E'\n' || f.respuesta
from public.faqs f
where f.activo
on conflict (source_type, source_id, chunk_index) do nothing;

create or replace function public.queue_event_document_section()
returns trigger
language plpgsql
as $$
begin
    if new.estado = 'Publicado'
       and new.estado_moderacion = 'Aprobado'
       and new.fecha_inicio > now()
       and exists (
           select 1 from public.usuarios u
           where u.usuario_id = new.organizador_id and u.estado = 'Activo'
       )
       and new.aforo > (
           select count(*) from public.entradas t
           where t.evento_id = new.evento_id and t.estado in ('Valida', 'Utilizada')
       ) then
        insert into public.document_sections (source_type, source_id, title, content, embedding)
        values ('event', new.evento_id::text, new.nombre, public.fn_texto_embedding(new.evento_id), null)
        on conflict (source_type, source_id, chunk_index) do update
            set title = excluded.title, content = excluded.content, embedding = null;
        delete from public.document_sections
        where source_type = 'event' and source_id = new.evento_id::text and chunk_index > 0;
    else
        delete from public.document_sections
        where source_type = 'event' and source_id = new.evento_id::text;
    end if;
    return new;
end;
$$;

drop trigger if exists trg_queue_event_document_section on public.eventos_embeddings;
drop trigger if exists trg_queue_event_document_section on public.eventos;
create trigger trg_queue_event_document_section
after insert or update on public.eventos
for each row
execute function public.queue_event_document_section();

insert into public.document_sections (source_type, source_id, title, content)
select 'event', e.evento_id::text, e.nombre, public.fn_texto_embedding(e.evento_id)
from public.eventos e
join public.usuarios u on u.usuario_id = e.organizador_id
where e.estado = 'Publicado'
  and e.estado_moderacion = 'Aprobado'
  and e.fecha_inicio > now()
  and u.estado = 'Activo'
  and e.aforo > (
      select count(*) from public.entradas t
      where t.evento_id = e.evento_id and t.estado in ('Valida', 'Utilizada')
  )
on conflict (source_type, source_id, chunk_index) do nothing;

create table if not exists public.admin_code_patches (
    id bigint generated always as identity primary key,
    created_at timestamptz not null default now(),
    error_summary text not null,
    target_repo text not null check (target_repo in ('frontend', 'backend')),
    file_path text not null,
    diff_content text not null,
    original_content text,
    validation_output text,
    pr_url text,
    status text not null default 'pending'
        check (status in ('pending', 'approved', 'rejected'))
);

alter table public.admin_code_patches add column if not exists original_content text;
alter table public.admin_code_patches add column if not exists validation_output text;

create table if not exists public.system_bug_reports (
    id bigint generated always as identity primary key,
    reporter_user_id integer not null references public.usuarios(usuario_id) on delete cascade,
    description varchar(1500) not null,
    fingerprint varchar(64) not null,
    report_day date not null,
    contains_prompt_injection boolean not null default false,
    created_at timestamptz not null default now(),
    unique (reporter_user_id, fingerprint, report_day)
);

create index if not exists ix_system_bug_reports_fingerprint_created
    on public.system_bug_reports (fingerprint, created_at desc);

create table if not exists public.system_bug_incidents (
    fingerprint varchar(64) primary key,
    first_reported_at timestamptz not null,
    last_reported_at timestamptz not null,
    report_count integer not null,
    distinct_reporters integer not null,
    status text not null default 'pending'
        check (status in ('pending', 'processing', 'awaiting_restart', 'resolved', 'no_patch', 'rejected')),
    admin_code_patch_id bigint references public.admin_code_patches(id),
    summary text not null,
    validation_output text,
    last_attempt_at timestamptz,
    attempt_count integer not null default 0
);

create index if not exists ix_system_bug_incidents_ready
    on public.system_bug_incidents (status, last_attempt_at)
    where status = 'pending';

create or replace function public.queue_repeated_bug_incident()
returns trigger
language plpgsql
security definer
set search_path = pg_catalog, public, pg_temp
as $$
declare
    recent_report_count integer;
    recent_reporters integer;
    earliest_report timestamptz;
    latest_report timestamptz;
begin
    select count(*)::integer,
           count(distinct reporter_user_id)::integer,
           min(created_at),
           max(created_at)
      into recent_report_count, recent_reporters, earliest_report, latest_report
      from public.system_bug_reports
     where fingerprint = new.fingerprint
       and created_at >= now() - interval '24 hours';

    if recent_reporters >= 3 then
        insert into public.system_bug_incidents (
            fingerprint, first_reported_at, last_reported_at, report_count,
            distinct_reporters, status, summary
        )
        values (
            new.fingerprint, earliest_report, latest_report, recent_report_count,
            recent_reporters, 'pending', new.description
        )
        on conflict (fingerprint) do update
            set first_reported_at = excluded.first_reported_at,
                last_reported_at = excluded.last_reported_at,
                report_count = excluded.report_count,
                distinct_reporters = excluded.distinct_reporters,
                status = case
                    when system_bug_incidents.status in ('resolved', 'no_patch', 'rejected')
                     and system_bug_incidents.last_reported_at < now() - interval '24 hours'
                    then 'pending'
                    else system_bug_incidents.status
                end,
                admin_code_patch_id = case
                    when system_bug_incidents.status in ('resolved', 'no_patch', 'rejected')
                     and system_bug_incidents.last_reported_at < now() - interval '24 hours'
                    then null
                    else system_bug_incidents.admin_code_patch_id
                end,
                attempt_count = case
                    when system_bug_incidents.status in ('resolved', 'no_patch', 'rejected')
                     and system_bug_incidents.last_reported_at < now() - interval '24 hours'
                    then 0
                    else system_bug_incidents.attempt_count
                end,
                last_attempt_at = case
                    when system_bug_incidents.status in ('resolved', 'no_patch', 'rejected')
                     and system_bug_incidents.last_reported_at < now() - interval '24 hours'
                    then null
                    else system_bug_incidents.last_attempt_at
                end,
                summary = case
                    when system_bug_incidents.status in ('resolved', 'no_patch', 'rejected')
                     and system_bug_incidents.last_reported_at < now() - interval '24 hours'
                    then excluded.summary
                    else system_bug_incidents.summary
                end
        where system_bug_incidents.status in ('pending', 'processing', 'awaiting_restart')
           or system_bug_incidents.last_reported_at < now() - interval '24 hours';
    end if;

    return new;
end;
$$;

drop trigger if exists trg_queue_repeated_bug_incident on public.system_bug_reports;
create trigger trg_queue_repeated_bug_incident
after insert on public.system_bug_reports
for each row
execute function public.queue_repeated_bug_incident();

alter table public.document_sections enable row level security;
alter table public.admin_code_patches enable row level security;
alter table public.system_bug_reports enable row level security;
alter table public.system_bug_incidents enable row level security;
revoke all on public.document_sections, public.admin_code_patches from anon, authenticated;
revoke all on public.system_bug_reports, public.system_bug_incidents from anon, authenticated;
revoke all on function public.match_documents(vector, float, int) from public, anon, authenticated;
revoke all on function public.queue_repeated_bug_incident() from public, anon, authenticated;
