namespace Core.Entities;

public class Dim_Cliente
{
    public int IdCliente { get; set; }
    public string CodigoClienteOrigen { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string? Email { get; set; }
    public string? Pais { get; set; }
    public string? Region { get; set; }
    public string? Ciudad { get; set; }

    public ICollection<Fact_Ventas> Ventas { get; set; } = [];
}
