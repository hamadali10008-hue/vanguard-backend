using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Saas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ManualCreateTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
        CREATE TABLE [dbo].[Tasks] (
            [Id]             INT            IDENTITY (1, 1) NOT NULL,
            [Title]          NVARCHAR (500) NOT NULL,
            [Status]         NVARCHAR (20)  DEFAULT (N'Todo') NOT NULL,
            [ProjectId]      INT            NOT NULL,
            [TenantId]       INT            NOT NULL,
            [AssignedUserId] INT            NULL,
            [CreatedAt]      DATETIME2 (7)  DEFAULT (getutcdate()) NOT NULL,
            CONSTRAINT [PK_Tasks] PRIMARY KEY CLUSTERED ([Id] ASC)
        );
    ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE [dbo].[Tasks];");
        }
    }
}
