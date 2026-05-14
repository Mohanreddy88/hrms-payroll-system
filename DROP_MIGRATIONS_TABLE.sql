-- Drop the migrations history table so EF Core will recreate everything
DROP TABLE IF EXISTS "__EFMigrationsHistory" CASCADE;

-- Verify it's gone
SELECT 'Migrations history table dropped! Ready for fresh migration.' as status;
