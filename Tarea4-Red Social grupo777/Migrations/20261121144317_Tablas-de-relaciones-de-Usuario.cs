using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Tarea4.Migrations
{
    /// <inheritdoc />
    public partial class TablasderelacionesdeUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GoogleId",
                table: "Usuarios");

            migrationBuilder.CreateTable(
                name: "Bloqueados",
                columns: table => new
                {
                    UsuarioBloqueadorId = table.Column<int>(type: "integer", nullable: false),
                    UsuarioBloqueadoId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bloqueados", x => new { x.UsuarioBloqueadorId, x.UsuarioBloqueadoId });
                    table.ForeignKey(
                        name: "FK_Bloqueados_Usuarios_UsuarioBloqueadoId",
                        column: x => x.UsuarioBloqueadoId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bloqueados_Usuarios_UsuarioBloqueadorId",
                        column: x => x.UsuarioBloqueadorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CuentasExternas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    ProveedorId = table.Column<string>(type: "text", nullable: false),
                    Proveedor = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuentasExternas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CuentasExternas_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Seguidores",
                columns: table => new
                {
                    UsuarioSeguidorId = table.Column<int>(type: "integer", nullable: false),
                    UsuarioSeguidoId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Seguidores", x => new { x.UsuarioSeguidorId, x.UsuarioSeguidoId });
                    table.ForeignKey(
                        name: "FK_Seguidores_Usuarios_UsuarioSeguidoId",
                        column: x => x.UsuarioSeguidoId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Seguidores_Usuarios_UsuarioSeguidorId",
                        column: x => x.UsuarioSeguidorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bloqueados_UsuarioBloqueadoId",
                table: "Bloqueados",
                column: "UsuarioBloqueadoId");

            migrationBuilder.CreateIndex(
                name: "IX_CuentasExternas_UsuarioId",
                table: "CuentasExternas",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Seguidores_UsuarioSeguidoId",
                table: "Seguidores",
                column: "UsuarioSeguidoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bloqueados");

            migrationBuilder.DropTable(
                name: "CuentasExternas");

            migrationBuilder.DropTable(
                name: "Seguidores");

            migrationBuilder.AddColumn<string>(
                name: "GoogleId",
                table: "Usuarios",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
