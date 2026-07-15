namespace Core.Entities;

public class Cliente
{
    public int IdCliente { get; set; }
    public string Nombre { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Region { get; set; } = null!;
}
