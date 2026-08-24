using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGateHandoverSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Purpose",
                table: "Visits",
                type: "TEXT",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<int>(
                name: "HostEmployeeId",
                table: "Visits",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<int>(
                name: "DepartmentId",
                table: "Visits",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.CreateTable(
                name: "VisitEquipment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VisitId = table.Column<int>(type: "INTEGER", nullable: false),
                    HasLaptop = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeviceType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    DeviceBrand = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    AssetTag = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    PcConfirmedReturned = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitEquipment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VisitEquipment_Visits_VisitId",
                        column: x => x.VisitId,
                        principalTable: "Visits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VisitEquipment_VisitId",
                table: "VisitEquipment",
                column: "VisitId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VisitEquipment");

            migrationBuilder.AlterColumn<string>(
                name: "Purpose",
                table: "Visits",
                type: "TEXT",
                maxLength: 30,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "HostEmployeeId",
                table: "Visits",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "DepartmentId",
                table: "Visits",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);
        }
    }
}
