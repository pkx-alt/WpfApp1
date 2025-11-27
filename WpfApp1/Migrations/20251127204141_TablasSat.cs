using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrySiPOS.Migrations
{
    /// <inheritdoc />
    public partial class TablasSat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SatProductos",
                columns: table => new
                {
                    Clave = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SatProductos", x => x.Clave);
                });

            migrationBuilder.CreateTable(
                name: "SatUnidades",
                columns: table => new
                {
                    Clave = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SatUnidades", x => x.Clave);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SatProductos");

            migrationBuilder.DropTable(
                name: "SatUnidades");
        }
    }
}
