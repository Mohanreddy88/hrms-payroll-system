-- Reset admin password to Admin@123
-- Run this in Railway PostgreSQL Data → Query tab

UPDATE "Users" 
SET "PasswordHash" = '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy'
WHERE "Username" = 'admin';

-- Verify the update
SELECT "Username", "Email", "Role", "IsActive" 
FROM "Users" 
WHERE "Username" = 'admin';
