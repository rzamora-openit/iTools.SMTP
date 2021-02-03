using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace OpeniT.SMTP.Web.Migrations
{
    public partial class AddsBCCInSmtpMailModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SmtpMailGuid2",
                table: "SmtpMailAddresses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SmtpMailId2",
                table: "SmtpMailAddresses",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SmtpMailAddresses_SmtpMailId2_SmtpMailGuid2",
                table: "SmtpMailAddresses",
                columns: new[] { "SmtpMailId2", "SmtpMailGuid2" });

            migrationBuilder.AddForeignKey(
                name: "FK_SmtpMailAddresses_SmtpMails_SmtpMailId2_SmtpMailGuid2",
                table: "SmtpMailAddresses",
                columns: new[] { "SmtpMailId2", "SmtpMailGuid2" },
                principalTable: "SmtpMails",
                principalColumns: new[] { "Id", "Guid" },
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SmtpMailAddresses_SmtpMails_SmtpMailId2_SmtpMailGuid2",
                table: "SmtpMailAddresses");

            migrationBuilder.DropIndex(
                name: "IX_SmtpMailAddresses_SmtpMailId2_SmtpMailGuid2",
                table: "SmtpMailAddresses");

            migrationBuilder.DropColumn(
                name: "SmtpMailGuid2",
                table: "SmtpMailAddresses");

            migrationBuilder.DropColumn(
                name: "SmtpMailId2",
                table: "SmtpMailAddresses");
        }
    }
}
