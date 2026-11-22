using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tarea4.Migrations
{
    /// <inheritdoc />
    public partial class m3UsuarioCuentaF : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "GoogleId",
                table: "Usuarios",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "GoogleId",
                table: "Usuarios",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
