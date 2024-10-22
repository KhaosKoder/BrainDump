using Irony.Parsing;

namespace JsonExtractionLanguage;

public class JELGrammar : Grammar
{
    public JELGrammar()
    {
        // Define comment terminal
        var comment = new CommentTerminal("comment", "--", "\n", "\r\n");
        NonGrammarTerminals.Add(comment);  // This tells Irony to completely ignore comments

        // Define term types
        var number = TerminalFactory.CreateCSharpNumber("number");
        var str = TerminalFactory.CreateCSharpString("string");
        var identifier = new IdentifierTerminal("identifier");

        // Define variable types
        var intType = ToTerm("int");
        var stringType = ToTerm("string");

        // Define operations
        var plus = ToTerm("+");
        var minus = ToTerm("-");
        var assign = ToTerm("=");

        // Define keywords
        var returnKeyword = ToTerm("return");

        // Non-terminals
        var program = new NonTerminal("program");
        var statement = new NonTerminal("statement");
        var declaration = new NonTerminal("declaration");
        var assignment = new NonTerminal("assignment");
        var expression = new NonTerminal("expression");
        var returnStmt = new NonTerminal("return");
        var varType = new NonTerminal("varType");
        var value = new NonTerminal("value");

        // Define rules
        program.Rule = MakeStarRule(program, statement);

        statement.Rule = declaration | assignment | returnStmt;

        // Simplify the type definition
        varType.Rule = intType | stringType;

        // Simplify value definition
        value.Rule = number | str | identifier;

        // Expression can be a simple value or an operation
        expression.Rule = value
            | expression + plus + value
            | expression + minus + value;

        // Declaration now includes both simple declaration and declaration with assignment
        declaration.Rule = varType + identifier + ToTerm(";")
            | varType + identifier + assign + expression + ToTerm(";");

        // Assignment is now simpler
        assignment.Rule = identifier + assign + expression + ToTerm(";");

        // Return statement
        returnStmt.Rule = returnKeyword + expression + ToTerm(";");

        // Set the root
        this.Root = program;

        // Mark reserved words and punctuation
        MarkReservedWords("int", "string", "return");
        RegisterOperators(1, "+", "-");
        MarkPunctuation(";", "=");
    }
}

// JELParser class to handle parsing of JEL scripts
public class JELParser
{
    private readonly Parser _parser;

    public JELParser()
    {
        // Initialize the grammar and parser
        var grammar = new JELGrammar();
        _parser = new Parser(grammar);
    }

    // Parse method to process a script and return a ParseTree
    public ParseTree Parse(string script)
    {
        // Parse the script
        var parseTree = _parser.Parse(script);

        // If there are errors, report them
        if (parseTree.HasErrors())
        {
            foreach (var error in parseTree.ParserMessages)
            {
                var errorText = $"Error at line {error.Location.Line}, column {error.Location.Column}: {error.Message}";

                // Try to extract variable names from the error context
                if (error.ParserState != null && error.ParserState.ExpectedTerminals != null)
                {
                    var expectedSymbols = string.Join(", ", error.ParserState.ExpectedTerminals.Select(t => t.Name));
                    errorText += $" (Expected: {expectedSymbols})";
                }

                // Print the relevant portion of the script (if possible)
                if (error.Location.Line >= 0 && error.Location.Column >= 0 && script != null)
                {
                    var scriptLines = script.Split('\n');
                    if (error.Location.Line < scriptLines.Length)
                    {
                        var codeLine = scriptLines[error.Location.Line].Trim();
                        errorText += $" | Code: {codeLine}";
                    }
                }

                Console.WriteLine(errorText);
            }
        }

        // Return the parse tree
        return parseTree;
    }

    // Method to print a formatted ParseTree
    // Method to print a formatted ParseTree
    public void PrintParseTree(ParseTree tree)
    {
        if (tree == null || tree.Root == null)
        {
            Console.WriteLine("Parse tree is empty or null.");
            return;
        }

        // Recursive method to print the tree with variable names and values
        void PrintNode(ParseTreeNode node, string indent = "", bool last = true)
        {
            // Draw the tree structure
            Console.Write(indent);
            if (last)
            {
                Console.Write("└─");
                indent += "  ";
            }
            else
            {
                Console.Write("├─");
                indent += "│ ";
            }

            // Print the node content with variable names and values
            string nodeValue = node.Term.Name;

            // Check if the node is an identifier or a value
            if (node.Term is IdentifierTerminal)
            {
                nodeValue += $" (identifier: {node.Token.Text})";
            }
            else if (node.Term is NumberLiteral || node.Term is StringLiteral)
            {
                nodeValue += $" (value: {node.Token.Text})";
            }
            else if (node.Term.Name == "=" || node.Term.Name == "+" || node.Term.Name == "-")
            {
                nodeValue += $" (operator: {node.Token.Text})";
            }

            Console.WriteLine(nodeValue);

            // Recursively print children
            for (var i = 0; i < node.ChildNodes.Count; i++)
            {
                PrintNode(node.ChildNodes[i], indent, i == node.ChildNodes.Count - 1);
            }
        }

        PrintNode(tree.Root);
    }

}