namespace BanchoNET.Core.Models.Api.Player;

public class Team
{
    public string FlagUrl { get; set; } = null!;
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string ShortName { get; set; } = null!;
}