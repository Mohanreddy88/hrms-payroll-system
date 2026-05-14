-- Mark the migration as already applied in Railway PostgreSQL
-- This tells EF Core that the InitialCreate migration has already been run

-- Insert the migration record into __EFMigrationsHistory
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260514070632_InitialCreate', '8.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;

-- Verify it was inserted
SELECT "MigrationId", "ProductVersion" 
FROM "__EFMigrationsHistory" 
ORDER BY "MigrationId";
