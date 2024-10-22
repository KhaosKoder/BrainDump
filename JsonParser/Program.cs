
// Top-level example usage (for testing purposes)
using JsonExtractionLanguage;
using Newtonsoft.Json.Linq;

var script = @"
    int x = 10;
    string y = ""hello"";
    x = x + 5;
    y = y + "" world"";
    x = x -1;
    return x;
";

var parser = new JELParser();
var parseTree = parser.Parse(script);

// Print the parse tree if parsing succeeded
if (!parseTree.HasErrors())
{
    var j = new JObject();
    Console.WriteLine("Parsing succeeded. Parse tree:");
    parser.PrintParseTree(parseTree);
var interpreter = new JELInterpreter(j);
    interpreter.Execute(parseTree);
}
else
{
    Console.WriteLine("Parsing failed.");
}
