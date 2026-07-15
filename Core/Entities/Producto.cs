namespace Core.Entities;

public class Producto
{
    public int IdProducto { get; set; }
    public string Nombre { get; set; } = null!;
    public string Categoria { get; set; } = null!;
    public decimal Precio { get; set; }
}
