--
-- PostgreSQL database dump
--

CREATE DATABASE diaryentities;
\c diaryentities;

-- Dumped from database version 16.4
-- Dumped by pg_dump version 16.4

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
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
-- Name: attendance; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.attendance (
    attendanceid integer NOT NULL,
    studentid integer NOT NULL,
    classid integer NOT NULL,
    date date NOT NULL,
    ispresent boolean NOT NULL,
    isexcusedabsence boolean NOT NULL,
    sessionnumber integer NOT NULL
);


ALTER TABLE public.attendance OWNER TO postgres;

--
-- Name: attendance_attendanceid_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.attendance_attendanceid_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.attendance_attendanceid_seq OWNER TO postgres;

--
-- Name: attendance_attendanceid_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.attendance_attendanceid_seq OWNED BY public.attendance.attendanceid;


--
-- Name: classes; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.classes (
    classid integer NOT NULL,
    subjectname character varying(255) NOT NULL,
    instructorid integer NOT NULL,
    groupnumber character varying(255) NOT NULL,
    semester integer NOT NULL,
    academicyear character varying(255) NOT NULL,
    typelesson integer NOT NULL
);


ALTER TABLE public.classes OWNER TO postgres;

--
-- Name: classes_classid_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.classes_classid_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.classes_classid_seq OWNER TO postgres;

--
-- Name: classes_classid_seq1; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.classes_classid_seq1
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.classes_classid_seq1 OWNER TO postgres;

--
-- Name: classes_classid_seq1; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.classes_classid_seq1 OWNED BY public.classes.classid;


--
-- Name: groupheads; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.groupheads (
    groupheadid integer NOT NULL,
    studentid integer NOT NULL,
    userid character varying(255)
);


ALTER TABLE public.groupheads OWNER TO postgres;

--
-- Name: groupheads_groupheadid_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.groupheads_groupheadid_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.groupheads_groupheadid_seq OWNER TO postgres;

--
-- Name: groupheads_groupheadid_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.groupheads_groupheadid_seq OWNED BY public.groupheads.groupheadid;


--
-- Name: person_contact_users; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.person_contact_users (
    id integer NOT NULL,
    personcontactid integer NOT NULL,
    userid character varying(255) NOT NULL
);


ALTER TABLE public.person_contact_users OWNER TO postgres;

--
-- Name: person_contact_users_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.person_contact_users_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.person_contact_users_id_seq OWNER TO postgres;

--
-- Name: person_contact_users_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.person_contact_users_id_seq OWNED BY public.person_contact_users.id;


--
-- Name: student_absences; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.student_absences (
    requestid integer NOT NULL,
    studentid integer NOT NULL,
    groupnumber character varying(255) NOT NULL,
    reason text NOT NULL,
    startdate date NOT NULL,
    enddate date NOT NULL,
    status integer NOT NULL
);


ALTER TABLE public.student_absences OWNER TO postgres;

--
-- Name: student_absences_requestid_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.student_absences_requestid_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.student_absences_requestid_seq OWNER TO postgres;

--
-- Name: student_absences_requestid_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.student_absences_requestid_seq OWNED BY public.student_absences.requestid;


--
-- Name: students; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.students (
    studentid integer NOT NULL,
    universitystudentid character varying(255),
    name character varying(255),
    groupnumber character varying(255)
);


ALTER TABLE public.students OWNER TO postgres;

--
-- Name: students_studentid_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.students_studentid_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.students_studentid_seq OWNER TO postgres;

--
-- Name: students_studentid_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.students_studentid_seq OWNED BY public.students.studentid;


--
-- Name: attendance attendanceid; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.attendance ALTER COLUMN attendanceid SET DEFAULT nextval('public.attendance_attendanceid_seq'::regclass);


--
-- Name: classes classid; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.classes ALTER COLUMN classid SET DEFAULT nextval('public.classes_classid_seq1'::regclass);


--
-- Name: groupheads groupheadid; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.groupheads ALTER COLUMN groupheadid SET DEFAULT nextval('public.groupheads_groupheadid_seq'::regclass);


--
-- Name: person_contact_users id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.person_contact_users ALTER COLUMN id SET DEFAULT nextval('public.person_contact_users_id_seq'::regclass);


--
-- Name: student_absences requestid; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.student_absences ALTER COLUMN requestid SET DEFAULT nextval('public.student_absences_requestid_seq'::regclass);


--
-- Name: students studentid; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.students ALTER COLUMN studentid SET DEFAULT nextval('public.students_studentid_seq'::regclass);


--
-- Data for Name: attendance; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.attendance (attendanceid, studentid, classid, date, ispresent, isexcusedabsence, sessionnumber) FROM stdin;
\.


--
-- Data for Name: classes; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.classes (classid, subjectname, instructorid, groupnumber, semester, academicyear, typelesson) FROM stdin;
\.


--
-- Data for Name: groupheads; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.groupheads (groupheadid, studentid, userid) FROM stdin;
\.


--
-- Data for Name: person_contact_users; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.person_contact_users (id, personcontactid, userid) FROM stdin;
5	668	a8649950-2381-485c-82a4-d415247e169c
\.


--
-- Data for Name: student_absences; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.student_absences (requestid, studentid, groupnumber, reason, startdate, enddate, status) FROM stdin;
\.


--
-- Data for Name: students; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.students (studentid, universitystudentid, name, groupnumber) FROM stdin;
\.


--
-- Name: attendance_attendanceid_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.attendance_attendanceid_seq', 1, false);


--
-- Name: classes_classid_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.classes_classid_seq', 2, true);


--
-- Name: classes_classid_seq1; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.classes_classid_seq1', 1, false);


--
-- Name: groupheads_groupheadid_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.groupheads_groupheadid_seq', 1, false);


--
-- Name: person_contact_users_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.person_contact_users_id_seq', 8, true);


--
-- Name: student_absences_requestid_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.student_absences_requestid_seq', 1, false);


--
-- Name: students_studentid_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.students_studentid_seq', 1, false);


--
-- Name: attendance attendance_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.attendance
    ADD CONSTRAINT attendance_pkey PRIMARY KEY (attendanceid);


--
-- Name: classes classes_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.classes
    ADD CONSTRAINT classes_pkey PRIMARY KEY (classid);


--
-- Name: groupheads groupheads_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.groupheads
    ADD CONSTRAINT groupheads_pkey PRIMARY KEY (groupheadid);


--
-- Name: person_contact_users person_contact_users_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.person_contact_users
    ADD CONSTRAINT person_contact_users_pkey PRIMARY KEY (id);


--
-- Name: student_absences student_absences_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.student_absences
    ADD CONSTRAINT student_absences_pkey PRIMARY KEY (requestid);


--
-- Name: students students_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.students
    ADD CONSTRAINT students_pkey PRIMARY KEY (studentid);


--
-- Name: attendance attendance_classid_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.attendance
    ADD CONSTRAINT attendance_classid_fkey FOREIGN KEY (classid) REFERENCES public.classes(classid);


--
-- Name: attendance attendance_studentid_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.attendance
    ADD CONSTRAINT attendance_studentid_fkey FOREIGN KEY (studentid) REFERENCES public.students(studentid);


--
-- Name: groupheads groupheads_studentid_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.groupheads
    ADD CONSTRAINT groupheads_studentid_fkey FOREIGN KEY (studentid) REFERENCES public.students(studentid);


--
-- Name: student_absences student_absences_studentid_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.student_absences
    ADD CONSTRAINT student_absences_studentid_fkey FOREIGN KEY (studentid) REFERENCES public.students(studentid);


--
-- PostgreSQL database dump complete
--

