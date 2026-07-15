using Microsoft.EntityFrameworkCore;
using global::Core.Entities;

namespace VentasETL.Infrastructure.Data;

public class VentasDbContext(DbContextOptions<VentasDbContext> options) : DbContext(options)
{
    public DbSet<Fact_Ventas> FactVentas { get; set; } = null!;
    public DbSet<Dim_Cliente> DimClientes { get; set; } = null!;
    public DbSet<Dim_Producto> DimProductos { get; set; } = null!;
    public DbSet<Dim_Tiempo> DimTiempos { get; set; } = null!;
    public DbSet<Dim_Fuente> DimFuentes { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Dim_Cliente configuration
        modelBuilder.Entity<Dim_Cliente>(entity =>
        {
            entity.ToTable("Dim_Cliente");
            entity.HasKey(e => e.IdCliente);
            entity.Property(e => e.IdCliente).ValueGeneratedNever();
            entity.Property(e => e.CodigoClienteOrigen).HasMaxLength(50).IsRequired().IsUnicode(false);
            entity.Property(e => e.Nombre).HasMaxLength(100).IsRequired().IsUnicode(false);
            entity.Property(e => e.Email).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.Pais).HasMaxLength(50).IsUnicode(false);
            entity.Property(e => e.Region).HasMaxLength(50).IsUnicode(false);
            entity.Property(e => e.Ciudad).HasMaxLength(50).IsUnicode(false);
        });

        // Dim_Producto configuration
        modelBuilder.Entity<Dim_Producto>(entity =>
        {
            entity.ToTable("Dim_Producto");
            entity.HasKey(e => e.IdProducto);
            entity.Property(e => e.IdProducto).ValueGeneratedNever();
            entity.Property(e => e.CodigoProductoOrigen).HasMaxLength(50).IsRequired().IsUnicode(false);
            entity.Property(e => e.Nombre).HasMaxLength(100).IsRequired().IsUnicode(false);
            entity.Property(e => e.Categoria).HasMaxLength(50).IsUnicode(false);
            entity.Property(e => e.PrecioUnitarioActual).HasColumnType("decimal(18, 2)");
        });

        // Dim_Fuente configuration
        modelBuilder.Entity<Dim_Fuente>(entity =>
        {
            entity.ToTable("Dim_Fuente");
            entity.HasKey(e => e.IdFuente);
            entity.Property(e => e.IdFuente).ValueGeneratedNever();
            entity.Property(e => e.NombreFuente).HasMaxLength(50).IsRequired().IsUnicode(false);
            entity.Property(e => e.TipoFuente).HasMaxLength(50).IsRequired().IsUnicode(false);
            entity.Property(e => e.FechaCarga).HasColumnType("datetime");
        });

        // Dim_Tiempo configuration
        modelBuilder.Entity<Dim_Tiempo>(entity =>
        {
            entity.ToTable("Dim_Tiempo");
            entity.HasKey(e => e.IdTiempo);
            entity.Property(e => e.IdTiempo).ValueGeneratedNever();
            entity.Property(e => e.Fecha).HasColumnType("date");
            entity.Property(e => e.NombreMes).HasMaxLength(20).IsRequired().IsUnicode(false);
            entity.Property(e => e.NombreDia).HasMaxLength(20).IsRequired().IsUnicode(false);
        });

        // Fact_Ventas configuration
        modelBuilder.Entity<Fact_Ventas>(entity =>
        {
            entity.ToTable("Fact_Ventas");
            entity.HasKey(e => e.IdFactVenta);
            entity.Property(e => e.IdFactVenta).ValueGeneratedNever();
            entity.Property(e => e.NumeroFactura).HasMaxLength(50).IsRequired().IsUnicode(false);
            entity.Property(e => e.Precio).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Total).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Cliente)
                  .WithMany(p => p.Ventas)
                  .HasForeignKey(d => d.IdCliente)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_FactVentas_Cliente");

            entity.HasOne(d => d.Producto)
                  .WithMany(p => p.Ventas)
                  .HasForeignKey(d => d.IdProducto)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_FactVentas_Producto");

            entity.HasOne(d => d.Tiempo)
                  .WithMany(p => p.Ventas)
                  .HasForeignKey(d => d.IdTiempo)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_FactVentas_Tiempo");

            entity.HasOne(d => d.Fuente)
                  .WithMany(p => p.Ventas)
                  .HasForeignKey(d => d.IdFuente)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_FactVentas_Fuente");
        });
    }
}