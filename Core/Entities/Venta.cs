namespace Core.Entities;

public class Venta
{
    public int IdVenta { get; set; }
    public int IdCliente { get; set; }
    public int IdProducto { get; set; }
    public int IdFuente { get; set; }
    public int Cantidad { get; set; }
    public decimal Precio { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Total { get; set; }
}