using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CityInfo.Migrations
{
    /// <inheritdoc />
    public partial class CityInfoDBAddPOIDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "PointOfInterests",
                type: "TEXT",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "PointOfInterests");
        }
    }
}
