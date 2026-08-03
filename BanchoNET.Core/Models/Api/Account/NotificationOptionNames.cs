namespace BanchoNET.Core.Models.Api.Account;

public static class NotificationOptionNames
{
    public const string BeatmapOwnerChange = "beatmap_owner_change";
    public const string BeatmapsetModding = "beatmapset:modding";
    public const string ChannelMessage = "channel_message";
    public const string ChannelMention = "channel_mention";
    public const string ChannelTeam = "channel_team";
    public const string CommentNew = "comment_new";
    public const string ForumTopicReply = "forum_topic_reply";
    public const string Mapping = "mapping";
    public const string NewsPost = "news_post";
    public const string BeatmapsetDisqualify = "beatmapset_disqualify";
    public const string BeatmapsetDiscussionQualifiedProblem = "beatmapset_discussion_qualified_problem";

    public const string MailKey = "mail";
    public const string PushKey = "push";
    public const string ModesKey = "modes";
    public const string SeriesKey = "series";
    public const string CommentReplyKey = "comment_reply";

    public static readonly string[] DeliveryRows = [
        BeatmapOwnerChange,
        BeatmapsetModding,
        ChannelMessage,
        ChannelMention,
        ChannelTeam,
        CommentNew,
        ForumTopicReply,
        Mapping,
        NewsPost
    ];

    private static readonly string[] Modes = ["osu", "taiko", "fruits", "mania"];
    private static readonly string[] NewsSeries = ["osu", "community", "dev", "store"];

    private static readonly Dictionary<string, string[]> AllowedKeys = new()
    {
        [BeatmapOwnerChange] = [MailKey, PushKey],
        [BeatmapsetModding] = [MailKey, PushKey],
        [ChannelMessage] = [MailKey, PushKey],
        [ChannelMention] = [MailKey, PushKey],
        [ChannelTeam] = [MailKey, PushKey],
        [CommentNew] = [MailKey, PushKey, CommentReplyKey],
        [ForumTopicReply] = [MailKey, PushKey],
        [Mapping] = [MailKey, PushKey],
        [NewsPost] = [MailKey, PushKey, SeriesKey],
        [BeatmapsetDisqualify] = [ModesKey],
        [BeatmapsetDiscussionQualifiedProblem] = [ModesKey]
    };

    public static bool IsKnown(
        string name
    ) => AllowedKeys.ContainsKey(name);

    public static bool IsKeyAllowed(
        string name,
        string key
    ) => AllowedKeys.TryGetValue(name, out var keys) && keys.Contains(key);

    public static bool IsListKey(
        string key
    ) => key is ModesKey or SeriesKey;

    public static bool AreListValuesValid(
        string key,
        IEnumerable<string> values
    ) {
        var allowed = key == ModesKey ? Modes : NewsSeries;

        return values.All(allowed.Contains);
    }
    
    public static bool DefaultMail(
        string name
    ) => DeliveryRows.Contains(name);

    public static bool DefaultPush(
        string name
    ) => name.StartsWith("channel_", StringComparison.Ordinal);
}