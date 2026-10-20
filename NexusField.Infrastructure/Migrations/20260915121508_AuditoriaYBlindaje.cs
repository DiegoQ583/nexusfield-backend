using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace NexusField.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AuditoriaYBlindaje : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCreacion",
                table: "Repuestos",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaModificacion",
                table: "Repuestos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsuarioModificacionId",
                table: "Repuestos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaModificacion",
                table: "OrdenesTrabajo",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsuarioModificacionId",
                table: "OrdenesTrabajo",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Descripcion",
                table: "Activos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCreacion",
                table: "Activos",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaModificacion",
                table: "Activos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsuarioModificacionId",
                table: "Activos",
                type: "int",
                nullable: true);

            migrationBuilder.InsertData(
                table: "Activos",
                columns: new[] { "Id", "Activo", "ClienteActual", "Descripcion", "Estado", "FechaCreacion", "FechaModificacion", "Nombre", "Ubicacion", "UsuarioModificacionId" },
                values: new object[,]
                {
                    { 1, true, "Consorcio A", null, "Operativo", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Ascensor Torre A", "Sótano", null },
                    { 2, true, "Consorcio B", null, "Operativo", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Planta Eléctrica B", "Azotea", null }
                });

            migrationBuilder.InsertData(
                table: "Repuestos",
                columns: new[] { "Id", "Activo", "Descripcion", "FechaCreacion", "FechaModificacion", "FotoUrl", "Nombre", "Precio", "StockReservado", "StockTotal", "UsuarioModificacionId" },
                values: new object[,]
                {
                    { 1, true, null, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Filtro de Aceite", 15.50m, 0, 100, null },
                    { 2, true, null, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Botonera", 45.00m, 0, 20, null }
                });

            migrationBuilder.InsertData(
                table: "OrdenesTrabajo",
                columns: new[] { "Id", "ActivoId", "ClienteHistorico", "CostoTotal", "Descripcion", "Estado", "FechaCompletada", "FechaCreacion", "FechaModificacion", "FirmaClienteBase64", "FotoUrl", "HorasTrabajadas", "NotasTecnico", "Prioridad", "TecnicoId", "Titulo", "UsuarioModificacionId" },
                values: new object[,]
                {
                    { 1, 1, "Consorcio A", 0m, "No cierran", "Pendiente", null, new DateTime(2026, 9, 11, 12, 0, 0, 0, DateTimeKind.Utc), null, null, null, 0m, null, "Alta", 1, "Falla en puertas", null },
                    { 2, 2, "Consorcio B", 150m, "Revisión mensual", "Completada", new DateTime(2026, 9, 8, 16, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 7, 8, 0, 0, 0, DateTimeKind.Utc), null, null, null, 4m, null, "Media", 1, "Mantenimiento preventivo", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "OrdenesTrabajo",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "OrdenesTrabajo",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Repuestos",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Repuestos",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Activos",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Activos",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DropColumn(
                name: "FechaCreacion",
                table: "Repuestos");

            migrationBuilder.DropColumn(
                name: "FechaModificacion",
                table: "Repuestos");

            migrationBuilder.DropColumn(
                name: "UsuarioModificacionId",
                table: "Repuestos");

            migrationBuilder.DropColumn(
                name: "FechaModificacion",
                table: "OrdenesTrabajo");

            migrationBuilder.DropColumn(
                name: "UsuarioModificacionId",
                table: "OrdenesTrabajo");

            migrationBuilder.DropColumn(
                name: "Descripcion",
                table: "Activos");

            migrationBuilder.DropColumn(
                name: "FechaCreacion",
                table: "Activos");

            migrationBuilder.DropColumn(
                name: "FechaModificacion",
                table: "Activos");

            migrationBuilder.DropColumn(
                name: "UsuarioModificacionId",
                table: "Activos");
        }
    }
}
