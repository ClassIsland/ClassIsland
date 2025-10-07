namespace ClassIsland.Core.Helpers.SimpleExpression;

internal class Expression(string functionName, string[] args)
{
    public string FunctionName { get; } = functionName;
    public string[] Arguments { get; } = args;
}