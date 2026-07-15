namespace Core.Entities;

public class Fact_Ventas
{
    public int IdFactVenta { get; set; }
    public int IdCliente { get; set; }
    public int IdProducto { get; set; }
    public int IdTiempo { get; set; }
    public int IdFuente { get; set; }
    public string NumeroFactura { get; set; } = null!;
    public int Cantidad { get; set; }
    public decimal Precio { get; set; }
    public decimal Total { get; set; }

    // Navigation properties
    public Dim_Cliente Cliente { get; set; } = null!;
    public Dim_Producto Producto { get; set; } = null!;
    public Dim_Tiempo Tiempo { get; set; } = null!;
    public Dim_Fuente Fuente { get; set; } = null!;
}
