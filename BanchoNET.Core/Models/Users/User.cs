using BanchoNET.Core.Abstractions;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Core.Models.Users;

#nullable disable

public class User : IUser,
    IEquatable<User>
{
    public int Id { get; set; } = 1;
    public int OnlineId => Id;
    public Guid SessionId { get; set; } = Guid.NewGuid();

    public string Username { get; set; } = string.Empty;
    public string[] PreviousUsernames;
    
    public ClientType ClientType { get; set; }
    
    private string _countryCodeString;

    public CountryCode CountryCode
    {
        get => Enum.TryParse(_countryCodeString, out CountryCode result) ? result : CountryCode.Unknown;
        set => _countryCodeString = value.ToString();
    }
    
    public bool IsBot { get; set; }
    
    public bool IsSupporter;
    
    public int SupportLevel;
    
    public override string ToString() => Username;
    
    public bool Equals(User other) => this.MatchesOnlineID(other);
    public override int GetHashCode() => Id.GetHashCode(); //TODO
}