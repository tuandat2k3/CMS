using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Migrations
{
    /// <inheritdoc />
    public partial class CASCADE : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Files_Contracts_ContractID",
                table: "Files");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Contracts_ContractID",
                table: "Invoices");

            migrationBuilder.AddForeignKey(
                name: "FK_Files_Contracts_ContractID",
                table: "Files",
                column: "ContractID",
                principalTable: "Contracts",
                principalColumn: "AutoID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Contracts_ContractID",
                table: "Invoices",
                column: "ContractID",
                principalTable: "Contracts",
                principalColumn: "AutoID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Files_Contracts_ContractID",
                table: "Files");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Contracts_ContractID",
                table: "Invoices");

            migrationBuilder.AddForeignKey(
                name: "FK_Files_Contracts_ContractID",
                table: "Files",
                column: "ContractID",
                principalTable: "Contracts",
                principalColumn: "AutoID");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Contracts_ContractID",
                table: "Invoices",
                column: "ContractID",
                principalTable: "Contracts",
                principalColumn: "AutoID");
        }
    }
}
