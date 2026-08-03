using System.Text.RegularExpressions;
using BanchoNET.Core.Models.Api.Account;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using BanchoNET.Core.Utils;

namespace BanchoNET.Core.Utils.ModelBinding;

public sealed record NotificationOptionInput(
    string Name,
    Dictionary<string, object> Details
);

public sealed partial class NotificationOptionsModelBinder : IModelBinder
{
    [GeneratedRegex(@"^user_notification_options\[(\d+)\]\[(name|details)\](?:\[([A-Za-z_]+)\](\[\])?)?$")]
    private static partial Regex KeyPattern();

    public Task BindModelAsync(
        ModelBindingContext bindingContext
    ) {
        if (!bindingContext.HttpContext.Request.HasFormContentType)
        {
            bindingContext.Result = ModelBindingResult.Success(new List<NotificationOptionInput>());
            return Task.CompletedTask;
        }

        var form = bindingContext.HttpContext.Request.Form;
        var names = new Dictionary<int, string>();
        var details = new Dictionary<int, Dictionary<string, object>>();

        foreach (var key in form.Keys)
        {
            var match = KeyPattern().Match(key);
            if (!match.Success) continue;

            var index = int.Parse(match.Groups[1].Value);
            var part = match.Groups[2].Value;

            if (part == "name")
            {
                names[index] = form[key].ToString();
                continue;
            }

            var detailKey = match.Groups[3].Value;
            if (string.IsNullOrEmpty(detailKey)) continue;

            if (!details.TryGetValue(index, out var bag))
                details[index] = bag = new Dictionary<string, object>();

            bag[detailKey] = NotificationOptionNames.IsListKey(detailKey)
                ? form[key].Where(v => !string.IsNullOrEmpty(v)).Select(v => v!).ToArray()
                : RequestValues.ParseBool(form[key].ToString()) ?? false;
        }

        var result = names
            .OrderBy(pair => pair.Key)
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
            .Select(pair => new NotificationOptionInput(
                pair.Value,
                details.TryGetValue(pair.Key, out var bag) ? bag : new Dictionary<string, object>()))
            .ToList();

        bindingContext.Result = ModelBindingResult.Success(result);
        return Task.CompletedTask;
    }
}