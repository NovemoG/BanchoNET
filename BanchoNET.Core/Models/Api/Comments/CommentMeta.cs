using System.Text.Json.Serialization;

namespace BanchoNET.Core.Models.Api.Comments;

[JsonDerivedType(typeof(CommentableMeta))]
public class CommentMeta
{
    public required string Title { get; set; }
}