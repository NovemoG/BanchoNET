namespace BanchoNET.Core.Models.Api.Beatmaps;

public class NominationsSummary
{
    public int Current { get; set; }
    public string[] EligibleMainRulesets { get; set; }
    public RequiredMeta RequiredMeta { get; set; }
}