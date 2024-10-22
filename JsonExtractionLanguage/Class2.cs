using Irony.Parsing;
using Newtonsoft.Json.Linq;
using System;

namespace JsonExtractionLanguage;

public class VariableManager
{
    private readonly JObject _symbolTable;

    public VariableManager(JObject existingJson)
    {
        _symbolTable = existingJson ?? throw new ArgumentNullException(nameof(existingJson));
    }

    public void DeclareVariable(string name, JToken value)
    {
        if (_symbolTable.ContainsKey(name))
            throw new InvalidOperationException($"Variable '{name}' is already declared.");

        _symbolTable[name] = value;
    }

    public JToken GetVariable(string name)
    {
        if (_symbolTable.TryGetValue(name, out var value))
            return value;

        throw new InvalidOperationException($"Variable '{name}' is not declared.");
    }

    public void SetVariable(string name, JToken value)
    {
        if (!_symbolTable.ContainsKey(name))
            throw new InvalidOperationException($"Variable '{name}' is not declared.");

        _symbolTable[name] = value;
    }

    public JObject GetSymbolTable() => _symbolTable;
}

public class ExpressionEvaluator
{
    private readonly VariableManager _variableManager;

    public ExpressionEvaluator(VariableManager variableManager)
    {
        _variableManager = variableManager;
    }

    public JToken Evaluate(ParseTreeNode node)
    {
        if (node == null) return null;

        return node.Term.Name switch
        {
            "expression" => EvaluateExpression(node),
            "value" => EvaluateValue(node),
            _ => throw new InvalidOperationException($"Unknown node type: {node.Term.Name}")
        };
    }

    private JToken EvaluateExpression(ParseTreeNode node)
    {
        // If it's a simple expression with just one value
        if (node.ChildNodes.Count == 1)
        {
            return Evaluate(node.ChildNodes[0]);
        }

        // It's an operation
        var left = Evaluate(node.ChildNodes[0]);
        var operation = node.ChildNodes[1].Term.Name;
        var right = Evaluate(node.ChildNodes[2]);

        return operation switch
        {
            "+" => AddValues(left, right),
            "-" => SubtractValues(left, right),
            _ => throw new InvalidOperationException($"Unknown operation: {operation}")
        };
    }

    private JToken EvaluateValue(ParseTreeNode node)
    {
        var child = node.ChildNodes[0];
        if (child.Term is Terminal term)
        {
            return term.Name switch
            {
                "number" => new JValue(Convert.ToInt32(child.Token.Value)),
                "string" => new JValue(child.Token.Value.ToString()),
                "identifier" => _variableManager.GetVariable(child.Token.Value.ToString()),
                _ => throw new InvalidOperationException($"Invalid value type: {term.Name}")
            };
        }
        throw new InvalidOperationException("Invalid value node structure");
    }

    private JToken AddValues(JToken left, JToken right)
    {
        if (left.Type == JTokenType.Integer && right.Type == JTokenType.Integer)
        {
            return new JValue(left.Value<int>() + right.Value<int>());
        }
        if (left.Type == JTokenType.String || right.Type == JTokenType.String)
        {
            return new JValue(left.ToString() + right.ToString());
        }
        throw new InvalidOperationException($"Cannot add values of types {left.Type} and {right.Type}");
    }

    private JToken SubtractValues(JToken left, JToken right)
    {
        if (left.Type == JTokenType.Integer && right.Type == JTokenType.Integer)
        {
            return new JValue(left.Value<int>() - right.Value<int>());
        }
        throw new InvalidOperationException($"Cannot subtract values of types {left.Type} and {right.Type}");
    }
}

public class StatementExecutor
{
    private readonly VariableManager _variableManager;
    private readonly ExpressionEvaluator _expressionEvaluator;

    public StatementExecutor(VariableManager variableManager, ExpressionEvaluator expressionEvaluator)
    {
        _variableManager = variableManager;
        _expressionEvaluator = expressionEvaluator;
    }

    public void ExecuteStatement(ParseTreeNode node)
    {
        if (node.Term.Name == "statement")
        {
            ExecuteStatement(node.ChildNodes[0]);
            return;
        }

        switch (node.Term.Name)
        {
            case "declaration":
                ExecuteDeclaration(node);
                break;
            case "assignment":
                ExecuteAssignment(node);
                break;
            case "return":
                ExecuteReturn(node);
                break;
            default:
                throw new InvalidOperationException($"Unknown statement type: {node.Term.Name}");
        }
    }

    private void ExecuteDeclaration(ParseTreeNode node)
    {
        // Debug check
        if (node.ChildNodes.Count < 3)
        {
            throw new InvalidOperationException($"Declaration node has insufficient children: {node.ChildNodes.Count}");
        }

        // For varType, we need to get the actual keyword value
        var typeNode = node.ChildNodes[0];
        var varType = typeNode.ChildNodes[0].Token.ValueString;  // Changed this line

        var identifierNode = node.ChildNodes[1];
        var identifier = identifierNode.Token.ValueString;

        var expressionNode = node.ChildNodes[2];
        var value = _expressionEvaluator.Evaluate(expressionNode);

        ValidateType(varType, value);
        _variableManager.DeclareVariable(identifier, value);
    }

    private void ExecuteAssignment(ParseTreeNode node)
    {
        var identifierNode = node.ChildNodes[0];
        var expressionNode = node.ChildNodes[1];

        var identifier = identifierNode.Token.ValueString;
        var value = _expressionEvaluator.Evaluate(expressionNode);

        var existingValue = _variableManager.GetVariable(identifier);
        if (existingValue.Type != value.Type)
        {
            throw new InvalidOperationException(
                $"Type mismatch in assignment. Cannot assign {value.Type} to variable of type {existingValue.Type}");
        }

        _variableManager.SetVariable(identifier, value);
    }

    private void ExecuteReturn(ParseTreeNode node)
    {
        var expressionNode = node.ChildNodes[1];
        var value = _expressionEvaluator.Evaluate(expressionNode);
        Console.WriteLine($"Return value: {value}");
    }

    private void ValidateType(string expectedType, JToken value)
    {
        bool isValid = expectedType switch
        {
            "int" => value.Type == JTokenType.Integer,
            "string" => value.Type == JTokenType.String,
            _ => throw new InvalidOperationException($"Unsupported type: {expectedType}")
        };

        if (!isValid)
        {
            throw new InvalidOperationException(
                $"Type mismatch. Expected {expectedType} but got {value.Type}");
        }
    }
}
public class JELInterpreter
{
    private readonly VariableManager _variableManager;
    private readonly ExpressionEvaluator _expressionEvaluator;
    private readonly StatementExecutor _statementExecutor;

    public JELInterpreter(JObject existingJson)
    {
        _variableManager = new VariableManager(existingJson);
        _expressionEvaluator = new ExpressionEvaluator(_variableManager);
        _statementExecutor = new StatementExecutor(_variableManager, _expressionEvaluator);
    }

    public void Execute(ParseTree parseTree)
    {
        if (parseTree.HasErrors())
        {
            throw new InvalidOperationException("Cannot execute parse tree with errors");
        }

        foreach (var node in parseTree.Root.ChildNodes)
        {
            _statementExecutor.ExecuteStatement(node);
        }
    }

    public JObject GetResult() => _variableManager.GetSymbolTable();
}