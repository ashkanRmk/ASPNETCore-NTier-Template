CREATE TABLE "ShortLinks" (
    "Id" serial NOT NULL,
    "UserId" integer NOT NULL,
    "IsDeleted" boolean NOT NULL,
    "CreatedOn" timestamp without time zone NOT NULL,
    "ModifiedOn" timestamp without time zone NOT NULL,
    "Source" text NULL,
    "Target" text NULL,
    "VisitedCount" integer NOT NULL,
    "Type" integer NOT NULL,
    "CretorUserId" integer NOT NULL,
    CONSTRAINT "PK_ShortLinks" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "IX_ShortLinks_Source" ON "ShortLinks" ("Source");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20191213161950_add-shortlink-table', '2.2.6-servicing-10079');

