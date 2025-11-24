using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WpfApp1.Migrations
{
    /// <inheritdoc />
    public partial class AgregarEsFactura : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsFactura",
                table: "Clientes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EsFactura",
                table: "Clientes");
        }
    }
}
