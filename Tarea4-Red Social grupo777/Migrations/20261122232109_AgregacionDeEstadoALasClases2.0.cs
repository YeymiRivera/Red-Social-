using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tarea4.Migrations
{
    /// <inheritdoc />
    public partial class AgregacionDeEstadoALasClases20 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Estado",
                table: "CuentasExternas",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Estado",
                table: "Bloqueados",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Estado",
                table: "CuentasExternas");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "Bloqueados");
        }
    }
}
