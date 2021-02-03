using Microsoft.EntityFrameworkCore.Migrations;

namespace OpeniT.SMTP.Web.Migrations
{
    public partial class AddsIsBodyHtmlInSmtpMailModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsBodyHtml",
                table: "SmtpMails",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsBodyHtml",
                table: "SmtpMails");
        }
    }
}
