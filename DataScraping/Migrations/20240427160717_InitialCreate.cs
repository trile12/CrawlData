using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace DataScraping.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataInfos",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    CompanyName = table.Column<string>(nullable: true),
                    CUI = table.Column<string>(nullable: false),
                    RegistDate = table.Column<string>(nullable: true),
                    MFINANCE = table.Column<string>(nullable: true),
                    Localitate = table.Column<string>(nullable: true),
                    District = table.Column<string>(nullable: true),
                    CodPostal = table.Column<string>(nullable: true),
                    SediuSocial = table.Column<string>(nullable: true),
                    CompanyStatus = table.Column<string>(nullable: true),
                    SocialCapital = table.Column<string>(nullable: true),
                    Phone = table.Column<string>(nullable: true),
                    Email = table.Column<string>(nullable: true),
                    Web = table.Column<string>(nullable: true),
                    ExtendedData = table.Column<string>(nullable: true),
                    NrOfBranches = table.Column<string>(nullable: true),
                    Owners = table.Column<string>(nullable: true),
                    Url = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataInfos", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DataInfos_CUI",
                table: "DataInfos",
                column: "CUI");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataInfos");
        }
    }
}