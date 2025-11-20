using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WpfApp1.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCamposCFDI : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FormaPagoSAT",
                table: "Ventas",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MetodoPagoSAT",
                table: "Ventas",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Moneda",
                table: "Ventas",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ClaveSat",
                table: "Productos",
                type: "TEXT",
                maxLength: 8,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ClaveUnidad",
                table: "Productos",
                type: "TEXT",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CodigoPostal",
                table: "Clientes",
                type: "TEXT",
                maxLength: 5,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RegimenFiscal",
                table: "Clientes",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UsoCFDI",
                table: "Clientes",
                type: "TEXT",
                maxLength: 3,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FormaPagoSAT",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "MetodoPagoSAT",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "Moneda",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "ClaveSat",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "ClaveUnidad",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "CodigoPostal",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "RegimenFiscal",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "UsoCFDI",
                table: "Clientes");
        }
    }
}
