namespace Core.Entities;

public class Dim_Fuente
{
    public int IdFuente { get; set; }
    public string NombreFuente { get; set; } = null!;
    public string TipoFuente { get; set; } = null!;
    public DateTime FechaCarga { get; set; }

    public ICollection<Fact_Ventas> Ventas { get; set; } = [];
}
