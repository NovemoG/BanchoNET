namespace BanchoNET.Core.Models.Auth;

public static class OAuthScopes
{
    public const string All = "*";
    /// <summary>
    /// Marks an endpoint any valid token may reach, used to opt out of an inherited scope.
    /// </summary>
    public const string None = "";
    public const string Identify = "identify";
    public const string Public = "public";
    public const string FriendsRead = "friends.read";
    public const string ChatRead = "chat.read";
    public const string ChatWrite = "chat.write";
    public const string ChatWriteManage = "chat.write_manage";
    public const string ForumWrite = "forum.write";
    public const string Delegate = "delegate";

    private static readonly HashSet<string> Known = new(StringComparer.Ordinal)
    {
        All, Identify, Public, FriendsRead, ChatRead, ChatWrite, ChatWriteManage, ForumWrite, Delegate
    };

    public static string[] Split(
        string? scope
    ) {
        return string.IsNullOrWhiteSpace(scope)
            ? []
            : scope.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    public static bool AreKnown(
        IEnumerable<string> scopes
    ) {
        return scopes.All(Known.Contains);
    }
    
    public static bool Grants(
        string? granted,
        string required
    ) {
        if (string.IsNullOrEmpty(required)) return true;

        var scopes = Split(granted);

        return scopes.Contains(All) || scopes.Contains(required);
    }
    
    public static string? Restrict(
        string requested,
        string allowed
    ) {
        var allowedScopes = Split(allowed);
        if (allowedScopes.Contains(All)) return requested;

        var requestedScopes = Split(requested);
        if (requestedScopes.Contains(All)) return null;

        return requestedScopes.All(allowedScopes.Contains)
            ? string.Join(' ', requestedScopes)
            : null;
    }
}