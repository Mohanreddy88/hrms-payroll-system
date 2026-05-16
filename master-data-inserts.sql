-- ═══════════════════════════════════════════════════════════════
-- HRMS Payroll System - Master Data Insert Scripts
-- Run these scripts in Railway PostgreSQL Query window
-- ═══════════════════════════════════════════════════════════════

-- ═══════════════════════════════════════════════════════════════
-- 1. BANK MASTER DATA (Major Indian Banks)
-- ═══════════════════════════════════════════════════════════════

INSERT INTO "BankMaster" ("Name", "IsActive", "CreatedDate", "CreatedBy")
VALUES 
    ('State Bank of India (SBI)', true, NOW(), 'admin'),
    ('HDFC Bank', true, NOW(), 'admin'),
    ('ICICI Bank', true, NOW(), 'admin'),
    ('Axis Bank', true, NOW(), 'admin'),
    ('Punjab National Bank (PNB)', true, NOW(), 'admin'),
    ('Bank of Baroda', true, NOW(), 'admin'),
    ('Canara Bank', true, NOW(), 'admin'),
    ('Union Bank of India', true, NOW(), 'admin'),
    ('Bank of India', true, NOW(), 'admin'),
    ('Indian Bank', true, NOW(), 'admin'),
    ('Central Bank of India', true, NOW(), 'admin'),
    ('Indian Overseas Bank', true, NOW(), 'admin'),
    ('UCO Bank', true, NOW(), 'admin'),
    ('Bank of Maharashtra', true, NOW(), 'admin'),
    ('Punjab & Sind Bank', true, NOW(), 'admin'),
    ('Kotak Mahindra Bank', true, NOW(), 'admin'),
    ('IndusInd Bank', true, NOW(), 'admin'),
    ('Yes Bank', true, NOW(), 'admin'),
    ('IDFC First Bank', true, NOW(), 'admin'),
    ('Federal Bank', true, NOW(), 'admin'),
    ('RBL Bank', true, NOW(), 'admin'),
    ('South Indian Bank', true, NOW(), 'admin'),
    ('Karur Vysya Bank', true, NOW(), 'admin'),
    ('Tamilnad Mercantile Bank', true, NOW(), 'admin'),
    ('City Union Bank', true, NOW(), 'admin'),
    ('IDBI Bank', true, NOW(), 'admin'),
    ('Bandhan Bank', true, NOW(), 'admin'),
    ('Jammu & Kashmir Bank', true, NOW(), 'admin'),
    ('DCB Bank', true, NOW(), 'admin'),
    ('Dhanlaxmi Bank', true, NOW(), 'admin')
ON CONFLICT DO NOTHING;

-- ═══════════════════════════════════════════════════════════════
-- 2. DEPARTMENTS (Common Company Departments)
-- ═══════════════════════════════════════════════════════════════

INSERT INTO "Departments" ("Name", "Description", "IsActive", "CreatedAt")
VALUES 
    ('Human Resources', 'HR and Employee Management', true, NOW()),
    ('Finance & Accounts', 'Financial Management and Accounting', true, NOW()),
    ('Information Technology', 'IT Infrastructure and Software Development', true, NOW()),
    ('Sales & Marketing', 'Sales Operations and Marketing', true, NOW()),
    ('Operations', 'Business Operations and Process Management', true, NOW()),
    ('Administration', 'General Administration and Facilities', true, NOW()),
    ('Customer Support', 'Customer Service and Support', true, NOW()),
    ('Research & Development', 'Product Research and Development', true, NOW()),
    ('Quality Assurance', 'Quality Control and Testing', true, NOW()),
    ('Legal & Compliance', 'Legal Affairs and Regulatory Compliance', true, NOW()),
    ('Procurement', 'Purchasing and Vendor Management', true, NOW()),
    ('Production', 'Manufacturing and Production', true, NOW()),
    ('Logistics', 'Supply Chain and Logistics', true, NOW()),
    ('Training & Development', 'Employee Training and Development', true, NOW()),
    ('Business Development', 'Business Growth and Partnerships', true, NOW())
ON CONFLICT DO NOTHING;

-- ═══════════════════════════════════════════════════════════════
-- 3. LEAVE TYPES (Already seeded, but including for reference)
-- ═══════════════════════════════════════════════════════════════

-- Check if leave types already exist
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM "LeaveTypes" WHERE "Code" = 'AL') THEN
        INSERT INTO "LeaveTypes" ("Name", "Code", "Description", "DefaultDaysPerYear", "IsActive", "RequiresApproval", "IsPaid", "CreatedAt")
        VALUES 
            ('Annual Leave', 'AL', 'Annual paid leave', 14, true, true, true, NOW()),
            ('Medical Leave', 'ML', 'Medical/Sick leave with certificate', 14, true, true, true, NOW()),
            ('Emergency Leave', 'EL', 'Emergency leave for urgent matters', 5, true, true, true, NOW()),
            ('Unpaid Leave', 'UL', 'Leave without pay', 0, true, true, false, NOW()),
            ('Maternity Leave', 'MAT', 'Maternity leave for female employees', 60, true, true, true, NOW()),
            ('Paternity Leave', 'PAT', 'Paternity leave for male employees', 7, true, true, true, NOW()),
            ('Casual Leave', 'CL', 'Casual leave for personal reasons', 12, true, true, true, NOW()),
            ('Compensatory Off', 'CO', 'Compensatory off for overtime work', 0, true, true, true, NOW()),
            ('Study Leave', 'SL', 'Leave for educational purposes', 5, true, true, true, NOW()),
            ('Bereavement Leave', 'BL', 'Leave for family member demise', 3, true, true, true, NOW());
    END IF;
END $$;

-- ═══════════════════════════════════════════════════════════════
-- 4. PUBLIC HOLIDAYS 2026 (India - National & Common State Holidays)
-- ═══════════════════════════════════════════════════════════════

INSERT INTO "PublicHolidays" ("Name", "Date", "Year", "IsNational", "State", "Description", "CreatedAt")
VALUES 
    -- National Holidays
    ('Republic Day', '2026-01-26', 2026, true, NULL, 'Republic Day of India', NOW()),
    ('Independence Day', '2026-08-15', 2026, true, NULL, 'Independence Day of India', NOW()),
    ('Gandhi Jayanti', '2026-10-02', 2026, true, NULL, 'Birth Anniversary of Mahatma Gandhi', NOW()),
    
    -- Religious Holidays (Approximate dates - may vary by lunar calendar)
    ('Holi', '2026-03-14', 2026, true, NULL, 'Festival of Colors', NOW()),
    ('Mahavir Jayanti', '2026-04-06', 2026, true, NULL, 'Birth Anniversary of Lord Mahavir', NOW()),
    ('Good Friday', '2026-04-03', 2026, true, NULL, 'Good Friday', NOW()),
    ('Eid ul-Fitr', '2026-04-21', 2026, true, NULL, 'Festival marking end of Ramadan', NOW()),
    ('Buddha Purnima', '2026-05-04', 2026, true, NULL, 'Birth Anniversary of Gautama Buddha', NOW()),
    ('Eid ul-Adha', '2026-06-28', 2026, true, NULL, 'Festival of Sacrifice', NOW()),
    ('Muharram', '2026-07-18', 2026, true, NULL, 'Islamic New Year', NOW()),
    ('Raksha Bandhan', '2026-08-09', 2026, false, 'Multiple', 'Festival of Brother-Sister Bond', NOW()),
    ('Janmashtami', '2026-08-25', 2026, true, NULL, 'Birth of Lord Krishna', NOW()),
    ('Ganesh Chaturthi', '2026-09-05', 2026, false, 'Maharashtra,Karnataka', 'Birthday of Lord Ganesha', NOW()),
    ('Dussehra', '2026-10-12', 2026, true, NULL, 'Victory of Good over Evil', NOW()),
    ('Diwali', '2026-10-31', 2026, true, NULL, 'Festival of Lights', NOW()),
    ('Guru Nanak Jayanti', '2026-11-15', 2026, true, NULL, 'Birth Anniversary of Guru Nanak', NOW()),
    ('Christmas', '2026-12-25', 2026, true, NULL, 'Birth of Jesus Christ', NOW()),
    
    -- Additional Common Holidays
    ('Pongal', '2026-01-14', 2026, false, 'Tamil Nadu', 'Harvest Festival', NOW()),
    ('Ugadi', '2026-03-22', 2026, false, 'Karnataka,Andhra Pradesh,Telangana', 'Telugu New Year', NOW()),
    ('Vishu', '2026-04-14', 2026, false, 'Kerala', 'Malayalam New Year', NOW()),
    ('Onam', '2026-08-31', 2026, false, 'Kerala', 'Harvest Festival of Kerala', NOW()),
    ('Durga Puja', '2026-10-08', 2026, false, 'West Bengal,Assam', 'Worship of Goddess Durga', NOW())
ON CONFLICT DO NOTHING;

-- ═══════════════════════════════════════════════════════════════
-- VERIFICATION QUERIES
-- ═══════════════════════════════════════════════════════════════

-- Count records inserted
SELECT 
    'BankMaster' as table_name, 
    COUNT(*) as record_count 
FROM "BankMaster"
UNION ALL
SELECT 
    'Departments', 
    COUNT(*) 
FROM "Departments"
UNION ALL
SELECT 
    'LeaveTypes', 
    COUNT(*) 
FROM "LeaveTypes"
UNION ALL
SELECT 
    'PublicHolidays', 
    COUNT(*) 
FROM "PublicHolidays"
ORDER BY table_name;

-- ═══════════════════════════════════════════════════════════════
-- END OF MASTER DATA INSERTS
-- ═══════════════════════════════════════════════════════════════
