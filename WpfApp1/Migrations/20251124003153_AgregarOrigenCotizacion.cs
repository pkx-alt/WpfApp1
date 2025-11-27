using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrySiPOS.Migrations
{
    /// <inheritdoc />
    public partial class AgregarOrigenCotizacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Origen",
                table: "Cotizaciones",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Origen",
                table: "Cotizaciones");
        }
    }
}
