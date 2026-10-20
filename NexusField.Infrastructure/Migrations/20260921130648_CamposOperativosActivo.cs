using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusField.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CamposOperativosActivo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LinkGoogleMaps",
                table: "Activos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TelefonoContacto",
                table: "Activos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Activos",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "LinkGoogleMaps", "TelefonoContacto" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Activos",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "LinkGoogleMaps", "TelefonoContacto" },
                values: new object[] { null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LinkGoogleMaps",
                table: "Activos");

            migrationBuilder.DropColumn(
                name: "TelefonoContacto",
                table: "Activos");
        }
    }
}
