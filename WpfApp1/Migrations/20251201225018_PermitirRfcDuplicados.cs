using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrySiPOS.Migrations
{
    /// <inheritdoc />
    public partial class PermitirRfcDuplicados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ENVOLVEMOS EN SQL PURO PARA QUE NO TRUENE
            // Esto le dice a SQLite: "Borra el índice SOLO SI EXISTE"
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Clientes_RFC;");

            // Creamos el índice nuevo (NO único)
            migrationBuilder.CreateIndex(
                name: "IX_Clientes_RFC",
                table: "Clientes",
                column: "RFC",
                unique: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Si quisiéramos deshacer el cambio (volver a bloquear):

            migrationBuilder.DropIndex(
                name: "IX_Clientes_RFC",
                table: "Clientes");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_RFC",
                table: "Clientes",
                column: "RFC",
                unique: true);
        }
    }
}