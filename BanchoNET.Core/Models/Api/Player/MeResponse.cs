namespace BanchoNET.Core.Models.Api.Player;

public class MeResponse : ApiPlayer
{
    public object? SessionVerificationMethod { get; set; } //TODO
    public bool SessionVerified { get; set; }
}