using System.Reflection;

namespace BanchoNET.Commands;

public static class CommandProcessor
{
    private static readonly Commands CommandsClass = new();
    
    static CommandProcessor()
    {
        const BindingFlags flags = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance;
        var typeHandler = typeof(Commands);
        
        foreach (var method in typeHandler.GetMethods(flags))
        {
            CommandMethodsMap.Add(method.Name.ToLower(), method);
        }
    }
    
    private static readonly Dictionary<string, MethodInfo> CommandMethodsMap = new();

    public static void Execute(string command)
    {
        var commandValues = command.Split(" ");
        
        if (CommandMethodsMap.TryGetValue(commandValues[0].ToLower(), out var method))
            method.Invoke(CommandsClass, [commandValues[1..]]);
        else
            Console.WriteLine("[CommandProcessor] Tried to use command that does not exist.");
    }
}