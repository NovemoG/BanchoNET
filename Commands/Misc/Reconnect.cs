using BanchoNET.Attributes;
using BanchoNET.Objects.Privileges;

namespace BanchoNET.Commands;

public partial class CommandProcessor
{
    [Command("reconnect",
        Privileges.Unrestricted,
        "Instantly reconnects player with given username. Syntax: reconnect [<username>]",
        "If you don't have enough permissions this command can only be used to reconnect yourself,\n" +
        "otherwise you can reconnect any player by providing their username.",
        ["rc"])]
    private async Task<string> Reconnect(params string[] args)
    {
        return "";
    }
}