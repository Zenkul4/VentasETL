using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
using VentasETL.Core.Entities;

namespace VentasETL.Infrastructure.Data;

public class VentasDbContext : IdentityDbContext<IdentityUser>
{
    public VentasDbContext(DbContextOptions<VentasDbContext> options) : base(options) { }

    public DbSet<Venta> Ventas { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Venta>().ToTable("Ventas").HasKey(v => v.IdVenta);
        builder.Entity<Venta>().Property(v => v.Total).HasColumnType("decimal(12, 2)");
        builder.Entity<Venta>().Property(v => v.Precio).HasColumnType("decimal(10, 2)");
    }
}