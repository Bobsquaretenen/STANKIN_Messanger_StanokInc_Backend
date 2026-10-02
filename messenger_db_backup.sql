--
-- PostgreSQL database dump
--

\restrict 15ZN59rQBpvf9wvZgTnCKoq3es1XcrSogpwahBDcCF1UIgbjhaXHdbpN5sJkdZ5

-- Dumped from database version 18.6 (Debian 18.6-1.pgdg13+2)
-- Dumped by pg_dump version 18.6 (Debian 18.6-1.pgdg13+2)

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: chat_members; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.chat_members (
    id_user integer NOT NULL,
    id_chat integer NOT NULL
);


ALTER TABLE public.chat_members OWNER TO postgres;

--
-- Name: group_chat; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.group_chat (
    id_chat integer NOT NULL,
    title character varying(30) NOT NULL,
    id_owner integer NOT NULL,
    rules text
);


ALTER TABLE public.group_chat OWNER TO postgres;

--
-- Name: group_chat_id_chat_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.group_chat_id_chat_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.group_chat_id_chat_seq OWNER TO postgres;

--
-- Name: group_chat_id_chat_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.group_chat_id_chat_seq OWNED BY public.group_chat.id_chat;


--
-- Name: message; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.message (
    id_message integer NOT NULL,
    id_chat integer NOT NULL,
    id_user integer NOT NULL,
    dispatch_time timestamp with time zone DEFAULT now() NOT NULL,
    info text NOT NULL
);


ALTER TABLE public.message OWNER TO postgres;

--
-- Name: message_id_message_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.message_id_message_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.message_id_message_seq OWNER TO postgres;

--
-- Name: message_id_message_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.message_id_message_seq OWNED BY public.message.id_message;


--
-- Name: users; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.users (
    id_user integer NOT NULL,
    login character varying(20) NOT NULL,
    password_hash text NOT NULL,
    full_name character varying(60),
    study_group character(9),
    role text NOT NULL,
    CONSTRAINT users_name_format CHECK (((full_name IS NULL) OR ((full_name)::text ~ '^[А-Яа-яЁё][А-Яа-яЁё -]*$'::text))),
    CONSTRAINT users_role_check CHECK ((role = ANY (ARRAY['admin'::text, 'teacher'::text, 'student'::text]))),
    CONSTRAINT users_study_group_format CHECK (((study_group IS NULL) OR (study_group ~ '^[А-Я]{3}-[0-9]{2}-[0-9]{2}$'::text)))
);


ALTER TABLE public.users OWNER TO postgres;

--
-- Name: users_id_user_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.users_id_user_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.users_id_user_seq OWNER TO postgres;

--
-- Name: users_id_user_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.users_id_user_seq OWNED BY public.users.id_user;


--
-- Name: group_chat id_chat; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.group_chat ALTER COLUMN id_chat SET DEFAULT nextval('public.group_chat_id_chat_seq'::regclass);


--
-- Name: message id_message; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.message ALTER COLUMN id_message SET DEFAULT nextval('public.message_id_message_seq'::regclass);


--
-- Name: users id_user; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.users ALTER COLUMN id_user SET DEFAULT nextval('public.users_id_user_seq'::regclass);


--
-- Data for Name: chat_members; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.chat_members (id_user, id_chat) FROM stdin;
\.


--
-- Data for Name: group_chat; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.group_chat (id_chat, title, id_owner, rules) FROM stdin;
\.


--
-- Data for Name: message; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.message (id_message, id_chat, id_user, dispatch_time, info) FROM stdin;
\.


--
-- Data for Name: users; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.users (id_user, login, password_hash, full_name, study_group, role) FROM stdin;
\.


--
-- Name: group_chat_id_chat_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.group_chat_id_chat_seq', 1, true);


--
-- Name: message_id_message_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.message_id_message_seq', 1, false);


--
-- Name: users_id_user_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.users_id_user_seq', 13, true);


--
-- Name: chat_members chat_members_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.chat_members
    ADD CONSTRAINT chat_members_pkey PRIMARY KEY (id_user, id_chat);


--
-- Name: group_chat group_chat_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.group_chat
    ADD CONSTRAINT group_chat_pkey PRIMARY KEY (id_chat);


--
-- Name: message message_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.message
    ADD CONSTRAINT message_pkey PRIMARY KEY (id_message);


--
-- Name: users users_login_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT users_login_key UNIQUE (login);


--
-- Name: users users_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT users_pkey PRIMARY KEY (id_user);


--
-- Name: chat_members chat_members_id_chat_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.chat_members
    ADD CONSTRAINT chat_members_id_chat_fkey FOREIGN KEY (id_chat) REFERENCES public.group_chat(id_chat);


--
-- Name: chat_members chat_members_id_user_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.chat_members
    ADD CONSTRAINT chat_members_id_user_fkey FOREIGN KEY (id_user) REFERENCES public.users(id_user);


--
-- Name: group_chat group_chat_id_owner_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.group_chat
    ADD CONSTRAINT group_chat_id_owner_fkey FOREIGN KEY (id_owner) REFERENCES public.users(id_user);


--
-- Name: message message_id_chat_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.message
    ADD CONSTRAINT message_id_chat_fkey FOREIGN KEY (id_chat) REFERENCES public.group_chat(id_chat);


--
-- Name: message message_id_user_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.message
    ADD CONSTRAINT message_id_user_fkey FOREIGN KEY (id_user) REFERENCES public.users(id_user);


--
-- PostgreSQL database dump complete
--

\unrestrict 15ZN59rQBpvf9wvZgTnCKoq3es1XcrSogpwahBDcCF1UIgbjhaXHdbpN5sJkdZ5

