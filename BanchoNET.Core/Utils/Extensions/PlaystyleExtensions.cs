using BanchoNET.Core.Models.Players;

namespace BanchoNET.Core.Utils.Extensions;

public static class PlaystyleExtensions
{
    private static readonly (Playstyle Flag, string Name)[] Map = [
        (Playstyle.Mouse, "mouse"),
        (Playstyle.Keyboard, "keyboard"),
        (Playstyle.Tablet, "tablet"),
        (Playstyle.Touch, "touch")
    ];

    public static string[] ToNames(
        this Playstyle style
    ) {
        return Map
            .Where(entry => style.HasFlag(entry.Flag))
            .Select(entry => entry.Name)
            .ToArray();
    }

    public static bool TryParseNames(
        IEnumerable<string> names,
        out Playstyle style
    ) {
        style = Playstyle.None;

        foreach (var name in names)
        {
            if (string.IsNullOrWhiteSpace(name)) continue;

            var match = Map.FirstOrDefault(entry =>
                string.Equals(entry.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));

            if (match.Name == null) return false;

            style |= match.Flag;
        }

        return true;
    }
}