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
    pr_url text,
    status text not null default 'pending'
        check (status in ('pending', 'approved', 'rejected'))
);

alter table public.document_sections enable row level security;
alter table public.admin_code_patches enable row level security;
revoke all on public.document_sections, public.admin_code_patches from anon, authenticated;
revoke all on function public.match_documents(vector, float, int) from public, anon, authenticated;
