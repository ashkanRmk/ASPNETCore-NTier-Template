CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

CREATE TABLE "Roles" (
    "Id" serial NOT NULL,
    "Name" character varying(450) NOT NULL,
    CONSTRAINT "PK_Roles" PRIMARY KEY ("Id")
);

CREATE TABLE "Users" (
    "Id" serial NOT NULL,
    "Username" character varying(450) NOT NULL,
    "Password" text NOT NULL,
    "DisplayName" text NULL,
    "IsActive" boolean NOT NULL,
    "Email" text NULL,
    "Mobile" text NULL,
    "LastLoggedIn" timestamp with time zone NULL,
    "SerialNumber" character varying(450) NULL,
    CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
);

CREATE TABLE "UserRoles" (
    "UserId" integer NOT NULL,
    "RoleId" integer NOT NULL,
    CONSTRAINT "PK_UserRoles" PRIMARY KEY ("UserId", "RoleId"),
    CONSTRAINT "FK_UserRoles_Roles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "Roles" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_UserRoles_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "UserTokens" (
    "Id" serial NOT NULL,
    "AccessTokenHash" text NULL,
    "AccessTokenExpiresDateTime" timestamp with time zone NOT NULL,
    "RefreshTokenIdHash" character varying(450) NOT NULL,
    "RefreshTokenIdHashSource" character varying(450) NULL,
    "RefreshTokenExpiresDateTime" timestamp with time zone NOT NULL,
    "UserId" integer NOT NULL,
    CONSTRAINT "PK_UserTokens" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_UserTokens_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_Roles_Name" ON "Roles" ("Name");

CREATE INDEX "IX_UserRoles_RoleId" ON "UserRoles" ("RoleId");

CREATE INDEX "IX_UserRoles_UserId" ON "UserRoles" ("UserId");

CREATE UNIQUE INDEX "IX_Users_Username" ON "Users" ("Username");

CREATE INDEX "IX_UserTokens_UserId" ON "UserTokens" ("UserId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20191027130854_init', '2.2.6-servicing-10079');

