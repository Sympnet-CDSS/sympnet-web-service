using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SympNet.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedAtToDoctor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "Doctors");

            migrationBuilder.AddColumn<int>(
                name: "TotalConsultations",
                table: "Doctors",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalPatients",
                table: "Doctors",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalConsultations",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "TotalPatients",
                table: "Doctors");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Doctors",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Doctors",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "Doctors",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
