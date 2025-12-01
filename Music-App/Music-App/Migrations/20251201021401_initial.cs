using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Music_App.Migrations
{
    public partial class initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Musics",
                columns: table => new
                {
                    TrackId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrackFile = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TrackTitle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TrackArtist = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TrackLength = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Musics", x => x.TrackId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Musics");
        }
    }
}
