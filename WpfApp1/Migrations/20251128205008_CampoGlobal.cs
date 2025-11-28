using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrySiPOS.Migrations
{
    /// <inheritdoc />
    public partial class CampoGlobal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ProcesadaEnGlobal",
                table: "Ventas",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProcesadaEnGlobal",
                table: "Ventas");
        }
    }
}
