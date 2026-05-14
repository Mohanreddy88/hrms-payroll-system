-- Just mark the migration as complete
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260514070632_InitialCreate', '8.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;
