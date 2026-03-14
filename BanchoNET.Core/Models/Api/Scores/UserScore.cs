namespace BanchoNET.Core.Models.Api.Scores;

public class UserScore
{
    public int Position { get; set; }
    public required ApiScore Score { get; set; }
}