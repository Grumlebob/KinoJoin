using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgeRatings",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    Censorship = table.Column<string>(
                        type: "character varying(50)",
                        maxLength: 50,
                        nullable: false
                    )
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgeRatings", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Cinemas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(
                        type: "character varying(260)",
                        maxLength: 260,
                        nullable: false
                    )
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cinemas", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Genres",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(
                        type: "character varying(260)",
                        maxLength: 260,
                        nullable: false
                    )
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Genres", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Hosts",
                columns: table => new
                {
                    AuthId = table.Column<string>(
                        type: "character varying(260)",
                        maxLength: 260,
                        nullable: false
                    ),
                    Username = table.Column<string>(
                        type: "character varying(260)",
                        maxLength: 260,
                        nullable: false
                    ),
                    Email = table.Column<string>(
                        type: "character varying(260)",
                        maxLength: 260,
                        nullable: true
                    )
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hosts", x => x.AuthId);
                }
            );

            migrationBuilder.CreateTable(
                name: "Playtimes",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    StartTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    )
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Playtimes", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Rooms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(
                        type: "character varying(260)",
                        maxLength: 260,
                        nullable: false
                    )
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "SelectOptions",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    VoteOption = table.Column<string>(
                        type: "character varying(260)",
                        maxLength: 260,
                        nullable: false
                    ),
                    Color = table.Column<string>(
                        type: "character varying(260)",
                        maxLength: 260,
                        nullable: false
                    )
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SelectOptions", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Versions",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    Type = table.Column<string>(
                        type: "character varying(260)",
                        maxLength: 260,
                        nullable: false
                    )
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Versions", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Movies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    AgeRatingId = table.Column<int>(type: "integer", nullable: true),
                    Title = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: false
                    ),
                    ImageUrl = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: true
                    ),
                    KinoUrl = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: true
                    ),
                    DurationInMinutes = table.Column<int>(type: "integer", nullable: false),
                    PremiereDate = table.Column<string>(
                        type: "character varying(100)",
                        maxLength: 100,
                        nullable: true
                    ),
                    IsSpecialShow = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Movies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Movies_AgeRatings_AgeRatingId",
                        column: x => x.AgeRatingId,
                        principalTable: "AgeRatings",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "JoinEvents",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    HostId = table.Column<string>(
                        type: "character varying(260)",
                        maxLength: 260,
                        nullable: false
                    ),
                    Title = table.Column<string>(
                        type: "character varying(60)",
                        maxLength: 60,
                        nullable: false
                    ),
                    Description = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: false
                    ),
                    ChosenShowtimeId = table.Column<int>(type: "integer", nullable: true),
                    DefaultSelectOptionId = table.Column<int>(type: "integer", nullable: false),
                    Deadline = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    )
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JoinEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JoinEvents_Hosts_HostId",
                        column: x => x.HostId,
                        principalTable: "Hosts",
                        principalColumn: "AuthId",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_JoinEvents_SelectOptions_DefaultSelectOptionId",
                        column: x => x.DefaultSelectOptionId,
                        principalTable: "SelectOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Showtimes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    MovieId = table.Column<int>(type: "integer", nullable: false),
                    CinemaId = table.Column<int>(type: "integer", nullable: false),
                    PlaytimeId = table.Column<int>(type: "integer", nullable: false),
                    VersionTagId = table.Column<int>(type: "integer", nullable: false),
                    RoomId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Showtimes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Showtimes_Cinemas_CinemaId",
                        column: x => x.CinemaId,
                        principalTable: "Cinemas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_Showtimes_Movies_MovieId",
                        column: x => x.MovieId,
                        principalTable: "Movies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_Showtimes_Playtimes_PlaytimeId",
                        column: x => x.PlaytimeId,
                        principalTable: "Playtimes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_Showtimes_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_Showtimes_Versions_VersionTagId",
                        column: x => x.VersionTagId,
                        principalTable: "Versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "JoinEventSelectOption",
                columns: table => new
                {
                    JoinEventId = table.Column<int>(type: "integer", nullable: false),
                    SelectOptionsId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_JoinEventSelectOption",
                        x => new { x.JoinEventId, x.SelectOptionsId }
                    );
                    table.ForeignKey(
                        name: "FK_JoinEventSelectOption_JoinEvents_JoinEventId",
                        column: x => x.JoinEventId,
                        principalTable: "JoinEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_JoinEventSelectOption_SelectOptions_SelectOptionsId",
                        column: x => x.SelectOptionsId,
                        principalTable: "SelectOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Participants",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    AuthId = table.Column<string>(
                        type: "character varying(260)",
                        maxLength: 260,
                        nullable: true
                    ),
                    JoinEventId = table.Column<int>(type: "integer", nullable: false),
                    Nickname = table.Column<string>(
                        type: "character varying(60)",
                        maxLength: 60,
                        nullable: false
                    ),
                    Email = table.Column<string>(
                        type: "character varying(60)",
                        maxLength: 60,
                        nullable: true
                    ),
                    Note = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: true
                    )
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Participants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Participants_JoinEvents_JoinEventId",
                        column: x => x.JoinEventId,
                        principalTable: "JoinEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "JoinEventShowtime",
                columns: table => new
                {
                    JoinEventId = table.Column<int>(type: "integer", nullable: false),
                    ShowtimesId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_JoinEventShowtime",
                        x => new { x.JoinEventId, x.ShowtimesId }
                    );
                    table.ForeignKey(
                        name: "FK_JoinEventShowtime_JoinEvents_JoinEventId",
                        column: x => x.JoinEventId,
                        principalTable: "JoinEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_JoinEventShowtime_Showtimes_ShowtimesId",
                        column: x => x.ShowtimesId,
                        principalTable: "Showtimes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "ParticipantVotes",
                columns: table => new
                {
                    ParticipantId = table.Column<int>(type: "integer", nullable: false),
                    ShowtimeId = table.Column<int>(type: "integer", nullable: false),
                    SelectedOptionId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_ParticipantVotes",
                        x => new { x.ParticipantId, x.ShowtimeId }
                    );
                    table.ForeignKey(
                        name: "FK_ParticipantVotes_Participants_ParticipantId",
                        column: x => x.ParticipantId,
                        principalTable: "Participants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_ParticipantVotes_SelectOptions_SelectedOptionId",
                        column: x => x.SelectedOptionId,
                        principalTable: "SelectOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_ParticipantVotes_Showtimes_ShowtimeId",
                        column: x => x.ShowtimeId,
                        principalTable: "Showtimes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_AgeRatings_Censorship",
                table: "AgeRatings",
                column: "Censorship",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_JoinEvents_DefaultSelectOptionId",
                table: "JoinEvents",
                column: "DefaultSelectOptionId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_JoinEvents_HostId",
                table: "JoinEvents",
                column: "HostId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_JoinEventSelectOption_SelectOptionsId",
                table: "JoinEventSelectOption",
                column: "SelectOptionsId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_JoinEventShowtime_ShowtimesId",
                table: "JoinEventShowtime",
                column: "ShowtimesId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Movies_AgeRatingId",
                table: "Movies",
                column: "AgeRatingId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Participants_JoinEventId",
                table: "Participants",
                column: "JoinEventId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantVotes_SelectedOptionId",
                table: "ParticipantVotes",
                column: "SelectedOptionId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantVotes_ShowtimeId",
                table: "ParticipantVotes",
                column: "ShowtimeId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Playtimes_StartTime",
                table: "Playtimes",
                column: "StartTime",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_SelectOptions_VoteOption_Color",
                table: "SelectOptions",
                columns: new[] { "VoteOption", "Color" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_Showtimes_CinemaId",
                table: "Showtimes",
                column: "CinemaId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Showtimes_MovieId",
                table: "Showtimes",
                column: "MovieId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Showtimes_PlaytimeId",
                table: "Showtimes",
                column: "PlaytimeId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Showtimes_RoomId",
                table: "Showtimes",
                column: "RoomId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Showtimes_VersionTagId",
                table: "Showtimes",
                column: "VersionTagId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Versions_Type",
                table: "Versions",
                column: "Type",
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Genres");

            migrationBuilder.DropTable(name: "JoinEventSelectOption");

            migrationBuilder.DropTable(name: "JoinEventShowtime");

            migrationBuilder.DropTable(name: "ParticipantVotes");

            migrationBuilder.DropTable(name: "Participants");

            migrationBuilder.DropTable(name: "Showtimes");

            migrationBuilder.DropTable(name: "JoinEvents");

            migrationBuilder.DropTable(name: "Cinemas");

            migrationBuilder.DropTable(name: "Movies");

            migrationBuilder.DropTable(name: "Playtimes");

            migrationBuilder.DropTable(name: "Rooms");

            migrationBuilder.DropTable(name: "Versions");

            migrationBuilder.DropTable(name: "Hosts");

            migrationBuilder.DropTable(name: "SelectOptions");

            migrationBuilder.DropTable(name: "AgeRatings");
        }
    }
}
