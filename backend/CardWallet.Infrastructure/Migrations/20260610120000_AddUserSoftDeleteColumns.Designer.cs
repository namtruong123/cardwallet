using System;
using CardWallet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CardWallet.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260610120000_AddUserSoftDeleteColumns")]
    partial class AddUserSoftDeleteColumns
    {
    }
}
