using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zouq.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class DesignDerivedFrom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DerivedFromDesignId",
                table: "Designs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Designs_DerivedFromDesignId",
                table: "Designs",
                column: "DerivedFromDesignId");

            migrationBuilder.AddForeignKey(
                name: "FK_Designs_Designs_DerivedFromDesignId",
                table: "Designs",
                column: "DerivedFromDesignId",
                principalTable: "Designs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Designs_Designs_DerivedFromDesignId",
                table: "Designs");

            migrationBuilder.DropIndex(
                name: "IX_Designs_DerivedFromDesignId",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "DerivedFromDesignId",
                table: "Designs");
        }
    }
}
