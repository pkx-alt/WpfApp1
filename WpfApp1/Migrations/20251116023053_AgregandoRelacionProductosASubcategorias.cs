using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WpfApp1.Migrations
{
    /// <inheritdoc />
    public partial class AgregandoRelacionProductosASubcategorias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SubcategoriaId",
                table: "Productos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Productos_SubcategoriaId",
                table: "Productos",
                column: "SubcategoriaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_Subcategorias_SubcategoriaId",
                table: "Productos",
                column: "SubcategoriaId",
                principalTable: "Subcategorias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Productos_Subcategorias_SubcategoriaId",
                table: "Productos");

            migrationBuilder.DropIndex(
                name: "IX_Productos_SubcategoriaId",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "SubcategoriaId",
                table: "Productos");
        }
    }
}
