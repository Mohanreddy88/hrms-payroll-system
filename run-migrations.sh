#!/bin/bash
# Railway Migration Runner
# This script runs inside Railway and applies migrations

set -e

echo "════════════════════════════════════════════════════════════════"
echo "  HRMS Database Migration Runner"
echo "════════════════════════════════════════════════════════════════"
echo ""

if [ -z "$DATABASE_URL" ]; then
  echo "❌ ERROR: DATABASE_URL environment variable not set"
  exit 1
fi

echo "📦 Database URL found"
echo "🔗 Connection: ${DATABASE_URL:0:25}..."
echo ""

# Parse postgresql://user:pass@host:port/db
if [[ $DATABASE_URL =~ postgresql://([^:]+):([^@]+)@([^:]+):([^/]+)/(.+) ]]; then
  USER="${BASH_REMATCH[1]}"
  PASS="${BASH_REMATCH[2]}"
  HOST="${BASH_REMATCH[3]}"
  PORT="${BASH_REMATCH[4]}"
  DB="${BASH_REMATCH[5]}"
  
  CONN_STRING="Host=$HOST;Port=$PORT;Database=$DB;Username=$USER;Password=$PASS;SSL Mode=Require;Trust Server Certificate=true"
  
  echo "🔄 Running database migrations..."
  echo ""
  
  ./efbundle --connection "$CONN_STRING"
  
  if [ $? -eq 0 ]; then
    echo ""
    echo "════════════════════════════════════════════════════════════════"
    echo "  ✅ MIGRATIONS COMPLETED SUCCESSFULLY!"
    echo "════════════════════════════════════════════════════════════════"
    echo ""
    echo "Tables created:"
    echo "  • Users, Employees, Departments"
    echo "  • LeaveTypes, LeaveRequests, Attendance"
    echo "  • Payroll, Timesheets, PublicHolidays"
    echo "  • BankMaster, and more..."
    echo ""
    echo "Initial data seeded:"
    echo "  • Admin user: admin@hrms.local / Admin@123"
    echo "  • 10 Leave types"
    echo ""
  else
    echo ""
    echo "❌ Migration failed!"
    exit 1
  fi
else
  echo "❌ Could not parse DATABASE_URL"
  exit 1
fi
