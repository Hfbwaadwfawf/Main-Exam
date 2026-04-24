using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GreenField.Data.Migrations
{
    /// <inheritdoc />
    public partial class productupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PointsRequired",
                table: "DiscountCodes",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PointsRequired",
                table: "DiscountCodes");
        }
    }
}
