
CREATE TABLE "Conversations" (
    "Id" uuid NOT NULL,
    "DoctorId" uuid NOT NULL,
    "PatientId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "LastMessageAt" timestamp with time zone,
    "IsActive" boolean NOT NULL,
    CONSTRAINT "PK_Conversations" PRIMARY KEY ("Id")
);

CREATE TABLE "ChatMessages" (
    "Id" uuid NOT NULL,
    "ConversationId" uuid NOT NULL,
    "SenderId" uuid NOT NULL,
    "SenderRole" text NOT NULL,
    "Content" text NOT NULL,
    "MessageType" text NOT NULL,
    "FileUrl" text,
    "IsRead" boolean NOT NULL,
    "SentAt" timestamp with time zone NOT NULL,
    "ReadAt" timestamp with time zone,
    CONSTRAINT "PK_ChatMessages" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ChatMessages_Conversations_ConversationId" FOREIGN KEY ("ConversationId") REFERENCES "Conversations" ("Id") ON DELETE CASCADE
);

CREATE TABLE "VideoCallSessions" (
    "Id" uuid NOT NULL,
    "ConversationId" uuid NOT NULL,
    "InitiatorId" uuid NOT NULL,
    "ReceiverId" uuid NOT NULL,
    "Status" text NOT NULL,
    "StartedAt" timestamp with time zone NOT NULL,
    "EndedAt" timestamp with time zone,
    "DurationSeconds" integer,
    CONSTRAINT "PK_VideoCallSessions" PRIMARY KEY ("Id")
);

