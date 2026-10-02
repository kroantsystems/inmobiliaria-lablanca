using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LaBlanca.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "lablanca");

            // As tabelas criadas abaixo recebem RLS pelo RlsMigrationsSqlGenerator; o histórico é criado antes pelo EF.
            migrationBuilder.Sql("ALTER TABLE lablanca.\"__EFMigrationsHistory\" ENABLE ROW LEVEL SECURITY;");

            migrationBuilder.CreateTable(
                name: "Owners",
                schema: "lablanca",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    Document = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Owners", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SiteSettings",
                schema: "lablanca",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    WhatsappNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    Address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    OfficeLatitude = table.Column<double>(type: "double precision", nullable: true),
                    OfficeLongitude = table.Column<double>(type: "double precision", nullable: true),
                    OpeningHours = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FacebookUrl = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    InstagramUrl = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    TiktokUrl = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    YoutubeUrl = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    PygPerUsd = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    BrlPerUsd = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    RatesUpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SimulatorAnnualRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    MonthlySalesGoal = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                schema: "lablanca",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Slug = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Zones",
                schema: "lablanca",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    City = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Zones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "lablanca",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    FailedLoginCount = table.Column<int>(type: "integer", nullable: false),
                    LockoutUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastLoginAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "lablanca",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Properties",
                schema: "lablanca",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Operation = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    IsFeatured = table.Column<bool>(type: "boolean", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ZoneId = table.Column<Guid>(type: "uuid", nullable: false),
                    City = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    Bedrooms = table.Column<int>(type: "integer", nullable: true),
                    Bathrooms = table.Column<int>(type: "integer", nullable: true),
                    BuiltAreaM2 = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    LotAreaM2 = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    ParkingSpaces = table.Column<int>(type: "integer", nullable: true),
                    Features = table.Column<List<string>>(type: "text[]", nullable: false),
                    VideoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SoldAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RentedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Properties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Properties_Owners_OwnerId",
                        column: x => x.OwnerId,
                        principalSchema: "lablanca",
                        principalTable: "Owners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Properties_Zones_ZoneId",
                        column: x => x.ZoneId,
                        principalSchema: "lablanca",
                        principalTable: "Zones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ZoneTranslations",
                schema: "lablanca",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ZoneId = table.Column<Guid>(type: "uuid", nullable: false),
                    Locale = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZoneTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ZoneTranslations_Zones_ZoneId",
                        column: x => x.ZoneId,
                        principalSchema: "lablanca",
                        principalTable: "Zones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                schema: "lablanca",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReplacedByTokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "lablanca",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AnalyticsEvents",
                schema: "lablanca",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: true),
                    Path = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Locale = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferrerHost = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalyticsEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalyticsEvents_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalSchema: "lablanca",
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Leads",
                schema: "lablanca",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    Interest = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: true),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Locale = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    ConsentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Leads_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalSchema: "lablanca",
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MediaFiles",
                schema: "lablanca",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: true),
                    OriginalName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AltText = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    IsCover = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UploadedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaFiles_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalSchema: "lablanca",
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PropertyTranslations",
                schema: "lablanca",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Locale = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Slug = table.Column<string>(type: "character varying(90)", maxLength: 90, nullable: false),
                    Description = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    SeoTitle = table.Column<string>(type: "character varying(70)", maxLength: 70, nullable: true),
                    SeoDescription = table.Column<string>(type: "character varying(170)", maxLength: 170, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PropertyTranslations_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalSchema: "lablanca",
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Visits",
                schema: "lablanca",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    LeadId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClientName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Visits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Visits_Leads_LeadId",
                        column: x => x.LeadId,
                        principalSchema: "lablanca",
                        principalTable: "Leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Visits_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalSchema: "lablanca",
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "lablanca",
                table: "SiteSettings",
                columns: new[] { "Id", "Address", "BrlPerUsd", "CompanyName", "CreatedAt", "Email", "FacebookUrl", "InstagramUrl", "MonthlySalesGoal", "OfficeLatitude", "OfficeLongitude", "OpeningHours", "Phone", "PygPerUsd", "RatesUpdatedAt", "SimulatorAnnualRate", "TenantId", "TiktokUrl", "UpdatedAt", "WhatsappNumber", "YoutubeUrl" },
                values: new object[] { new Guid("0b6a0d43-6f1e-4a3c-8d52-7c9e1f2a3b40"), null, 5m, "Inmobiliaria La Blanca", new DateTimeOffset(new DateTime(2026, 10, 2, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, 10, null, null, null, null, 7500m, new DateTimeOffset(new DateTime(2026, 10, 2, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 8m, new Guid("6f1c2a4e-8b3d-4f5a-9c7e-2d1b0a9e8f71"), null, null, null, null });

            migrationBuilder.InsertData(
                schema: "lablanca",
                table: "Tenants",
                columns: new[] { "Id", "Name", "Slug" },
                values: new object[] { new Guid("6f1c2a4e-8b3d-4f5a-9c7e-2d1b0a9e8f71"), "Inmobiliaria La Blanca", "la-blanca" });

            migrationBuilder.InsertData(
                schema: "lablanca",
                table: "Zones",
                columns: new[] { "Id", "City", "Slug", "SortOrder", "TenantId" },
                values: new object[,]
                {
                    { new Guid("a1d0f3c2-1b4e-4c6a-9f20-0a1b2c3d4e01"), "Hernandarias", "parana-country-club", 1, new Guid("6f1c2a4e-8b3d-4f5a-9c7e-2d1b0a9e8f71") },
                    { new Guid("a1d0f3c2-1b4e-4c6a-9f20-0a1b2c3d4e02"), "Ciudad del Este", "centro-cde", 2, new Guid("6f1c2a4e-8b3d-4f5a-9c7e-2d1b0a9e8f71") },
                    { new Guid("a1d0f3c2-1b4e-4c6a-9f20-0a1b2c3d4e03"), "Ciudad del Este", "km-8-km-10", 3, new Guid("6f1c2a4e-8b3d-4f5a-9c7e-2d1b0a9e8f71") },
                    { new Guid("a1d0f3c2-1b4e-4c6a-9f20-0a1b2c3d4e04"), "Ciudad del Este", "area-1-area-4", 4, new Guid("6f1c2a4e-8b3d-4f5a-9c7e-2d1b0a9e8f71") },
                    { new Guid("a1d0f3c2-1b4e-4c6a-9f20-0a1b2c3d4e05"), "Hernandarias", "hernandarias", 5, new Guid("6f1c2a4e-8b3d-4f5a-9c7e-2d1b0a9e8f71") }
                });

            migrationBuilder.InsertData(
                schema: "lablanca",
                table: "ZoneTranslations",
                columns: new[] { "Id", "Description", "Locale", "Name", "TenantId", "ZoneId" },
                values: new object[,]
                {
                    { new Guid("b2e1a4d3-2c5f-4d7b-8a31-010100000000"), null, "es", "Paraná Country Club", new Guid("6f1c2a4e-8b3d-4f5a-9c7e-2d1b0a9e8f71"), new Guid("a1d0f3c2-1b4e-4c6a-9f20-0a1b2c3d4e01") },
                    { new Guid("b2e1a4d3-2c5f-4d7b-8a31-010200000000"), null, "pt", "Paraná Country Club", new Guid("6f1c2a4e-8b3d-4f5a-9c7e-2d1b0a9e8f71"), new Guid("a1d0f3c2-1b4e-4c6a-9f20-0a1b2c3d4e01") },
                    { new Guid("b2e1a4d3-2c5f-4d7b-8a31-010300000000"), null, "en", "Paraná Country Club", new Guid("6f1c2a4e-8b3d-4f5a-9c7e-2d1b0a9e8f71"), new Guid("a1d0f3c2-1b4e-4c6a-9f20-0a1b2c3d4e01") },
                    { new Guid("b2e1a4d3-2c5f-4d7b-8a31-010400000000"), null, "gn", "Paraná Country Club", new Guid("6f1c2a4e-8b3d-4f5a-9c7e-2d1b0a9e8f71"), new Guid("a1d0f3c2-1b4e-4c6a-9f20-0a1b2c3d4e01") },
                    { new Guid("b2e1a4d3-2c5f-4d7b-8a31-020100000000"), null, "es", "Centro de Ciudad del Este", new Guid("6f1c2a4e-8b3d-4f5a-9c7e-2d1b0a9e8f71"), new Guid("a1d0f3c2-1b4e-4c6a-9f20-0a1b2c3d4e02") },
                    { new Guid("b2e1a4d3-2c5f-4d7b-8a31-020200000000"), null, "pt", "Centro de Ciudad del Este", new Guid("6f1c2a4e-8b3d-4f5a-9c7e-2d1b0a9e8f71"), new Guid("a1d0f3c2-1b4e-4c6a-9f20-0a1b2c3d4e02") },
                    { new Guid("b2e1a4d3-2c5f-4d7b-8a31-020300000000"), null, "en", "Downtown Ciudad del Este", new Guid("6f1c2a4e-8b3d-4f5a-9c7e-2d1b0a9e8f71"), new Guid("a1d0f3c2-1b4e-4c6a-9f20-0a1b2c3d4e02") },
                    { new Guid("b2e1a4d3-2c5f-4d7b-8a31-020400000000"), null, "gn", "Centro de Ciudad del Este", new Guid("6f1c2a4e-8b3d-4f5a-9c7e-2d1b0a9e8f71"), new Guid("a1d0f3c2-1b4e-4c6a-9f20-0a1b2c3d4e02") },
                    { new Guid("b2e1a4d3-2c5f-4d7b-8a31-030100000000"), null, "es", "Km 8 / Km 10", new Guid("6f1c2a4e-8b3d-4f5a-9c7e-2d1b0a9e8f71"), new Guid("a1d0f3c2-1b4e-4c6a-9f20-0a1b2c3d4e03") },
                    { new Guid("b2e1a4d3-2c5f-4d7b-8a31-030200000000"), null, "pt", "Km 8 / Km 10", new Guid("6f1c2a4e-8b3d-4f5a-9c7e-2d1b0a9e8f71"), new Guid("a1d0f3c2-1b4e-4c6a-9f20-0a1b2c3d4e03") },
                    { new Guid("b2e1a4d3-2c5f-4d7b-8a31-030300000000"), null, "en", "Km 8 / Km 10", new Guid("6f1c2a4e-8b3d-4f5a-9c7e-2d1b0a9e8f71"), new Guid("a1d0f3c2-1b4e-4c6a-9f20-0a1b2c3d4e03") },
                    { new Guid("b2e1a4d3-2c5f-4d7b-8a31-030400000000"), null, "gn", "Km 8 / Km 10", new Guid("6f1c2a4e-8b3d-4f5a-9c7e-2d1b0a9e8f71"), new Guid("a1d0f3c2-1b4e-4c6a-9f20-0a1b2c3d4e03") },
                    { new Guid("b2e1a4d3-2c5f-4d7b-8a31-040100000000"), null, "es", "Área 1 / Área 4", new Guid("6f1c2a4e-8b3d-4f5a-9c7e-2d1b0a9e8f71"), new Guid("a1d0f3c2-1b4e-4c6a-9f20-0a1b2c3d4e04") },
                    { new Guid("b2e1a4d3-2c5f-4d7b-8a31-040200000000"), null, "pt", "Área 1 / Área 4", new Guid("6f1c2a4e-8b3d-4f5a-9c7e-2d1b0a9e8f71"), new Guid("a1d0f3c2-1b4e-4c6a-9f20-0a1b2c3d4e04") },
                    { new Guid("b2e1a4d3-2c5f-4d7b-8a31-040300000000"), null, "en", "Área 1 / Área 4", new Guid("6f1c2a4e-8b3d-4f5a-9c7e-2d1b0a9e8f71"), new Guid("a1d0f3c2-1b4e-4c6a-9f20-0a1b2c3d4e04") },
                    { new Guid("b2e1a4d3-2c5f-4d7b-8a31-040400000000"), null, "gn", "Área 1 / Área 4", new Guid("6f1c2a4e-8b3d-4f5a-9c7e-2d1b0a9e8f71"), new Guid("a1d0f3c2-1b4e-4c6a-9f20-0a1b2c3d4e04") },
                    { new Guid("b2e1a4d3-2c5f-4d7b-8a31-050100000000"), null, "es", "Hernandarias", new Guid("6f1c2a4e-8b3d-4f5a-9c7e-2d1b0a9e8f71"), new Guid("a1d0f3c2-1b4e-4c6a-9f20-0a1b2c3d4e05") },
                    { new Guid("b2e1a4d3-2c5f-4d7b-8a31-050200000000"), null, "pt", "Hernandarias", new Guid("6f1c2a4e-8b3d-4f5a-9c7e-2d1b0a9e8f71"), new Guid("a1d0f3c2-1b4e-4c6a-9f20-0a1b2c3d4e05") },
                    { new Guid("b2e1a4d3-2c5f-4d7b-8a31-050300000000"), null, "en", "Hernandarias", new Guid("6f1c2a4e-8b3d-4f5a-9c7e-2d1b0a9e8f71"), new Guid("a1d0f3c2-1b4e-4c6a-9f20-0a1b2c3d4e05") },
                    { new Guid("b2e1a4d3-2c5f-4d7b-8a31-050400000000"), null, "gn", "Hernandarias", new Guid("6f1c2a4e-8b3d-4f5a-9c7e-2d1b0a9e8f71"), new Guid("a1d0f3c2-1b4e-4c6a-9f20-0a1b2c3d4e05") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsEvents_PropertyId",
                schema: "lablanca",
                table: "AnalyticsEvents",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsEvents_TenantId_OccurredAt_Type",
                schema: "lablanca",
                table: "AnalyticsEvents",
                columns: new[] { "TenantId", "OccurredAt", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsEvents_TenantId_PropertyId_OccurredAt",
                schema: "lablanca",
                table: "AnalyticsEvents",
                columns: new[] { "TenantId", "PropertyId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Leads_PropertyId",
                schema: "lablanca",
                table: "Leads",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_TenantId_CreatedAt",
                schema: "lablanca",
                table: "Leads",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_PropertyId",
                schema: "lablanca",
                table: "MediaFiles",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_StorageKey",
                schema: "lablanca",
                table: "MediaFiles",
                column: "StorageKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_TenantId_PropertyId",
                schema: "lablanca",
                table: "MediaFiles",
                columns: new[] { "TenantId", "PropertyId" });

            migrationBuilder.CreateIndex(
                name: "IX_Properties_OwnerId",
                schema: "lablanca",
                table: "Properties",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Properties_TenantId_IsFeatured_PublishedAt",
                schema: "lablanca",
                table: "Properties",
                columns: new[] { "TenantId", "IsFeatured", "PublishedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Properties_TenantId_IsPublished_Status_Operation_Type_ZoneI~",
                schema: "lablanca",
                table: "Properties",
                columns: new[] { "TenantId", "IsPublished", "Status", "Operation", "Type", "ZoneId", "Price" });

            migrationBuilder.CreateIndex(
                name: "IX_Properties_ZoneId",
                schema: "lablanca",
                table: "Properties",
                column: "ZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyTranslations_PropertyId_Locale",
                schema: "lablanca",
                table: "PropertyTranslations",
                columns: new[] { "PropertyId", "Locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PropertyTranslations_TenantId_Locale_Slug",
                schema: "lablanca",
                table: "PropertyTranslations",
                columns: new[] { "TenantId", "Locale", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ExpiresAt",
                schema: "lablanca",
                table: "RefreshTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                schema: "lablanca",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                schema: "lablanca",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteSettings_TenantId",
                schema: "lablanca",
                table: "SiteSettings",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Slug",
                schema: "lablanca",
                table: "Tenants",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId_Email",
                schema: "lablanca",
                table: "Users",
                columns: new[] { "TenantId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Visits_LeadId",
                schema: "lablanca",
                table: "Visits",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_PropertyId",
                schema: "lablanca",
                table: "Visits",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_TenantId_StartsAt",
                schema: "lablanca",
                table: "Visits",
                columns: new[] { "TenantId", "StartsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Zones_TenantId_Slug",
                schema: "lablanca",
                table: "Zones",
                columns: new[] { "TenantId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ZoneTranslations_ZoneId_Locale",
                schema: "lablanca",
                table: "ZoneTranslations",
                columns: new[] { "ZoneId", "Locale" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalyticsEvents",
                schema: "lablanca");

            migrationBuilder.DropTable(
                name: "MediaFiles",
                schema: "lablanca");

            migrationBuilder.DropTable(
                name: "PropertyTranslations",
                schema: "lablanca");

            migrationBuilder.DropTable(
                name: "RefreshTokens",
                schema: "lablanca");

            migrationBuilder.DropTable(
                name: "SiteSettings",
                schema: "lablanca");

            migrationBuilder.DropTable(
                name: "Visits",
                schema: "lablanca");

            migrationBuilder.DropTable(
                name: "ZoneTranslations",
                schema: "lablanca");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "lablanca");

            migrationBuilder.DropTable(
                name: "Leads",
                schema: "lablanca");

            migrationBuilder.DropTable(
                name: "Tenants",
                schema: "lablanca");

            migrationBuilder.DropTable(
                name: "Properties",
                schema: "lablanca");

            migrationBuilder.DropTable(
                name: "Owners",
                schema: "lablanca");

            migrationBuilder.DropTable(
                name: "Zones",
                schema: "lablanca");
        }
    }
}
