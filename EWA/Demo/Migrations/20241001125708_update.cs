using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Demo.Migrations
{
    /// <inheritdoc />
    public partial class update : Migration
    {

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Users_MemberId",
                table: "Orders");

            migrationBuilder.AlterColumn<string>(
                name: "MemberId",
                table: "Orders",
                type: "nvarchar(5)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(5)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Users_MemberId",
                table: "Orders",
                column: "MemberId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Users_MemberId",
                table: "Orders");

            migrationBuilder.AlterColumn<string>(
                name: "MemberId",
                table: "Orders",
                type: "nvarchar(5)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(5)");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Users_MemberId",
                table: "Orders",
                column: "MemberId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
