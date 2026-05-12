using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HrmsApi.Migrations
{
    /// <inheritdoc />
    public partial class AddTotalMedicalLeaveToTimesheets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Only add TotalMedicalLeave column to Timesheets table if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Timesheets' AND COLUMN_NAME = 'TotalMedicalLeave')
                BEGIN
                    ALTER TABLE Timesheets ADD TotalMedicalLeave INT NOT NULL DEFAULT 0
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalMedicalLeave",
                table: "Timesheets");
        }
    }
}
