namespace VentasETL.Core.Entities;

public class Dim_Producto
{
    public int IdProducto { get; set; }
    public string CodigoProductoOrigen { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string? Categoria { get; set; }
    public decimal? PrecioUnitarioActual { get; set; }

    public ICollection<Fact_Ventas> Ventas { get; set; } = [];
}
