using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BanchoNET.Core.Models.Api.Account;

public class FormError
{
    public Dictionary<string, Dictionary<string, List<string>>> Errors { get; }

    private FormError(
        Dictionary<string, Dictionary<string, List<string>>> errors
    ) {
        Errors = errors;
    }

    public object ToPayload() => new { form_error = Errors };

    public static object Single(
        string group,
        string field,
        string message
    ) => new FormError(new Dictionary<string, Dictionary<string, List<string>>>
    {
        [group] = new() { [field] = [message] }
    }).ToPayload();

    public static object FromModelState(
        ModelStateDictionary modelState,
        string defaultGroup
    ) {
        var errors = new Dictionary<string, Dictionary<string, List<string>>>();

        foreach (var (key, entry) in modelState)
        {
            if (entry.Errors.Count == 0) continue;

            var (group, field) = SplitKey(key, defaultGroup);
            var messages = entry.Errors
                .Select(e => string.IsNullOrEmpty(e.ErrorMessage) ? "is invalid" : e.ErrorMessage)
                .ToList();

            if (!errors.TryGetValue(group, out var fields))
                errors[group] = fields = new Dictionary<string, List<string>>();

            if (fields.TryGetValue(field, out var existing))
                existing.AddRange(messages);
            else
                fields[field] = messages;
        }

        return new FormError(errors).ToPayload();
    }
    
    private static (string Group, string Field) SplitKey(
        string key,
        string defaultGroup
    ) {
        var open = key.IndexOf('[');
        if (open <= 0) return (defaultGroup, key);

        var close = key.IndexOf(']', open);
        if (close < 0) return (defaultGroup, key);

        return (key[..open], key[(open + 1)..close]);
    }
}