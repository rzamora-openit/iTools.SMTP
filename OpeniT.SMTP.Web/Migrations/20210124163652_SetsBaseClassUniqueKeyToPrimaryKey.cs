using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace OpeniT.SMTP.Web.Migrations
{
    public partial class SetsBaseClassUniqueKeyToPrimaryKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SmtpMailAddresses_SmtpMails_SmtpMailId",
                table: "SmtpMailAddresses");

            migrationBuilder.DropForeignKey(
                name: "FK_SmtpMailAddresses_SmtpMails_SmtpMailId1",
                table: "SmtpMailAddresses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SmtpMails",
                table: "SmtpMails");

            migrationBuilder.DropIndex(
                name: "IX_SmtpMails_Guid",
                table: "SmtpMails");

            migrationBuilder.DropIndex(
                name: "IX_SmtpMailAddresses_SmtpMailId",
                table: "SmtpMailAddresses");

            migrationBuilder.DropIndex(
                name: "IX_SmtpMailAddresses_SmtpMailId1",
                table: "SmtpMailAddresses");

            migrationBuilder.AddColumn<Guid>(
                name: "SmtpMailGuid",
                table: "SmtpMailAddresses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SmtpMailGuid1",
                table: "SmtpMailAddresses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SmtpMails",
                table: "SmtpMails",
                columns: new[] { "Id", "Guid" });

            migrationBuilder.CreateIndex(
                name: "IX_SmtpMailAddresses_SmtpMailId_SmtpMailGuid",
                table: "SmtpMailAddresses",
                columns: new[] { "SmtpMailId", "SmtpMailGuid" });

            migrationBuilder.CreateIndex(
                name: "IX_SmtpMailAddresses_SmtpMailId1_SmtpMailGuid1",
                table: "SmtpMailAddresses",
                columns: new[] { "SmtpMailId1", "SmtpMailGuid1" });

            migrationBuilder.AddForeignKey(
                name: "FK_SmtpMailAddresses_SmtpMails_SmtpMailId_SmtpMailGuid",
                table: "SmtpMailAddresses",
                columns: new[] { "SmtpMailId", "SmtpMailGuid" },
                principalTable: "SmtpMails",
                principalColumns: new[] { "Id", "Guid" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SmtpMailAddresses_SmtpMails_SmtpMailId1_SmtpMailGuid1",
                table: "SmtpMailAddresses",
                columns: new[] { "SmtpMailId1", "SmtpMailGuid1" },
                principalTable: "SmtpMails",
                principalColumns: new[] { "Id", "Guid" },
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SmtpMailAddresses_SmtpMails_SmtpMailId_SmtpMailGuid",
                table: "SmtpMailAddresses");

            migrationBuilder.DropForeignKey(
                name: "FK_SmtpMailAddresses_SmtpMails_SmtpMailId1_SmtpMailGuid1",
                table: "SmtpMailAddresses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SmtpMails",
                table: "SmtpMails");

            migrationBuilder.DropIndex(
                name: "IX_SmtpMailAddresses_SmtpMailId_SmtpMailGuid",
                table: "SmtpMailAddresses");

            migrationBuilder.DropIndex(
                name: "IX_SmtpMailAddresses_SmtpMailId1_SmtpMailGuid1",
                table: "SmtpMailAddresses");

            migrationBuilder.DropColumn(
                name: "SmtpMailGuid",
                table: "SmtpMailAddresses");

            migrationBuilder.DropColumn(
                name: "SmtpMailGuid1",
                table: "SmtpMailAddresses");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SmtpMails",
                table: "SmtpMails",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_SmtpMails_Guid",
                table: "SmtpMails",
                column: "Guid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SmtpMailAddresses_SmtpMailId",
                table: "SmtpMailAddresses",
                column: "SmtpMailId");

            migrationBuilder.CreateIndex(
                name: "IX_SmtpMailAddresses_SmtpMailId1",
                table: "SmtpMailAddresses",
                column: "SmtpMailId1");

            migrationBuilder.AddForeignKey(
                name: "FK_SmtpMailAddresses_SmtpMails_SmtpMailId",
                table: "SmtpMailAddresses",
                column: "SmtpMailId",
                principalTable: "SmtpMails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SmtpMailAddresses_SmtpMails_SmtpMailId1",
                table: "SmtpMailAddresses",
                column: "SmtpMailId1",
                principalTable: "SmtpMails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
