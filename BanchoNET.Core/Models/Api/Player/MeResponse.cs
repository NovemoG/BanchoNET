namespace BanchoNET.Core.Models.Api.Player;

public class MeResponse : ApiPlayer
{
    public object? SessionVerificationMethod { get; set; }
    public bool SessionVerified { get; set; }
    public StatisticsRulesets StatisticsRulesets { get; set; } = new();
}