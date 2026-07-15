namespace Core.Entities;

public class Dim_Tiempo
{
    public int IdTiempo { get; set; }
    public DateTime Fecha { get; set; }
    public int Anio { get; set; }
    public int Mes { get; set; }
    public string NombreMes { get; set; } = null!;
    public int Trimestre { get; set; }
    public int Dia { get; set; }
    public string NombreDia { get; set; } = null!;

    public ICollection<Fact_Ventas> Ventas { get; set; } = [];
}
