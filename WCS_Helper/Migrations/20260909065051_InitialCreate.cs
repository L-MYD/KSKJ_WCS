using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WCS_Helper.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AddPod",
                columns: table => new
                {
                    PodID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PodHeight = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AddPod", x => x.PodID);
                });

            migrationBuilder.CreateTable(
                name: "AgvPalletStation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PSStationCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AGVStationCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StationName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StationStatu = table.Column<int>(type: "int", nullable: false),
                    OccupyStatus = table.Column<int>(type: "int", nullable: false),
                    Podid = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgvPalletStation", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Aisles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AislesName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WmsReference = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FloorNum = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Aisles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApiServer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IP = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiServer", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConveVisu",
                columns: table => new
                {
                    DataGuid = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PLCIP = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IndexNum = table.Column<int>(type: "int", nullable: false),
                    DeviceNum = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StateType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Podid = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConveVisu", x => x.DataGuid);
                });

            migrationBuilder.CreateTable(
                name: "EmptyPodIDApply",
                columns: table => new
                {
                    PodID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PodNum = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmptyPodIDApply", x => x.PodID);
                });

            migrationBuilder.CreateTable(
                name: "logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    usertype = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    logtype = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    level = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    module = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    operation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    details = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    userid = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ipaddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    createtime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    isarchived = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "retry_messages",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    source = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    msgtype = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    targeturl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    payload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    bizkey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<int>(type: "int", nullable: false),
                    retrycount = table.Column<int>(type: "int", nullable: false),
                    maxretry = table.Column<int>(type: "int", nullable: false),
                    nextretrytime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    lasterror = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    createtime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updatetime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_retry_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "StationCodeWithNum",
                columns: table => new
                {
                    code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    nodes_uni = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StationCodeWithNum", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "stationstatus",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    stationcode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    wmsstationname = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    tescheckname = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stationstatus", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tescorresponds_wms",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    wcs_id = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    tes_task_id = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    wms_task_id = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    wms_task_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    current_position = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    statu = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    creation_time = table.Column<DateTime>(type: "datetime2", nullable: true),
                    completion_time = table.Column<DateTime>(type: "datetime2", nullable: true),
                    target_location = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    pod_id = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tescorresponds_wms", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserRole = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastLoginTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AddPod");

            migrationBuilder.DropTable(
                name: "AgvPalletStation");

            migrationBuilder.DropTable(
                name: "Aisles");

            migrationBuilder.DropTable(
                name: "ApiServer");

            migrationBuilder.DropTable(
                name: "ConveVisu");

            migrationBuilder.DropTable(
                name: "EmptyPodIDApply");

            migrationBuilder.DropTable(
                name: "logs");

            migrationBuilder.DropTable(
                name: "retry_messages");

            migrationBuilder.DropTable(
                name: "StationCodeWithNum");

            migrationBuilder.DropTable(
                name: "stationstatus");

            migrationBuilder.DropTable(
                name: "tescorresponds_wms");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
