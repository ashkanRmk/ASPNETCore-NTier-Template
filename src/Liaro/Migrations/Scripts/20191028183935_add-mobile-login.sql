ALTER TABLE "Users" ADD "LoginCode" text NULL;

ALTER TABLE "Users" ADD "MobileLoginExpire" timestamp with time zone NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20191028183935_add-mobile-login', '2.2.6-servicing-10079');

