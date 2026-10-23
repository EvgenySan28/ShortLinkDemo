using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShortLinks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OriginalLink = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RedirectCount = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShortLinks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShortLinks_Code",
                table: "ShortLinks",
                column: "Code",
                unique: true)
                .Annotation("Npgsql:IndexInclude", new[] { "OriginalLink" });

            migrationBuilder.CreateIndex(
                name: "IX_ShortLinks_OriginalLink",
                table: "ShortLinks",
                column: "OriginalLink",
                unique: true)
                .Annotation("Npgsql:IndexInclude", new[] { "Code" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShortLinks");
        }
    }
}
