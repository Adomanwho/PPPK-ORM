using Npgsql;
using ORM.Core.Metadata;
using System.Linq.Expressions;
using System.Reflection;

namespace ORM.Core.Querying;

/*
Rekurzivno hoda Expression stablo i gradi SQL WHERE fragment s parametrima.
Podržava: ==, !=, <, <=, >, >=, &&, ||, !, member access, konstante, null provjere.
*/
public class ExpressionParser
{
    private readonly EntityMetadata _metadata;
    private readonly List<NpgsqlParameter> _params = [];
    private int _paramCounter;

    public ExpressionParser(EntityMetadata metadata)
    {
        _metadata = metadata;
    }

    public (string Sql, IReadOnlyList<NpgsqlParameter> Parameters) Parse(Expression expression)
    {
        _params.Clear();
        _paramCounter = 0;
        var sql = ParseNode(expression);
        return (sql, _params.AsReadOnly());
    }

    private string ParseNode(Expression node) => node switch
    {
        LambdaExpression lambda       => ParseNode(lambda.Body),
        BinaryExpression binary       => ParseBinary(binary),
        UnaryExpression unary         => ParseUnary(unary),
        MemberExpression member       => ParseMember(member),
        ConstantExpression constant   => AddParam(constant.Value),
        MethodCallExpression call     => ParseMethodCall(call),
        _                             => throw new NotSupportedException(
                                            $"Expression tipa '{node.NodeType}' nije podržan.")
    };

    private string ParseBinary(BinaryExpression node)
    {
        // Null provjere: x.Prop == null → "Prop" IS NULL
        if (node.NodeType is ExpressionType.Equal or ExpressionType.NotEqual)
        {
            if (IsNullConstant(node.Right))
                return NullCheck(node.Left, node.NodeType == ExpressionType.Equal);
            if (IsNullConstant(node.Left))
                return NullCheck(node.Right, node.NodeType == ExpressionType.Equal);
        }

        var left  = ParseNode(node.Left);
        var op    = node.NodeType switch
        {
            ExpressionType.Equal              => "=",
            ExpressionType.NotEqual           => "<>",
            ExpressionType.LessThan           => "<",
            ExpressionType.LessThanOrEqual    => "<=",
            ExpressionType.GreaterThan        => ">",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.AndAlso            => "AND",
            ExpressionType.OrElse             => "OR",
            _ => throw new NotSupportedException($"Binarni operator '{node.NodeType}' nije podržan.")
        };
        var right = ParseNode(node.Right);

        return node.NodeType is ExpressionType.AndAlso or ExpressionType.OrElse
            ? $"({left} {op} {right})"
            : $"{left} {op} {right}";
    }

    private string ParseUnary(UnaryExpression node)
    {
        if (node.NodeType == ExpressionType.Not)
            return $"NOT ({ParseNode(node.Operand)})";

        // Convert / TypeAs — samo proslijedi dalje
        if (node.NodeType is ExpressionType.Convert or ExpressionType.TypeAs)
            return ParseNode(node.Operand);

        throw new NotSupportedException($"Unarni operator '{node.NodeType}' nije podržan.");
    }

    private string ParseMember(MemberExpression node)
    {
        // Property na parametru lambde (npr. p.Ime)
        if (node.Expression is ParameterExpression)
        {
            var col = ResolveColumn(node.Member.Name);
            return $"\"{col}\"";
        }

        // Closure / captured variable — evaluiraj vrijednost
        var value = EvaluateMember(node);
        return AddParam(value);
    }

    private string ParseMethodCall(MethodCallExpression node)
    {
        // string.Contains → LIKE '%value%'
        if (node.Method.Name == "Contains" && node.Object is MemberExpression containsMember)
        {
            var col = ResolveColumn(containsMember.Member.Name);
            var val = EvaluateMember((MemberExpression)node.Arguments[0]);
            return $"\"{col}\" LIKE {AddParam($"%{val}%")}";
        }

        // string.StartsWith → LIKE 'value%'
        if (node.Method.Name == "StartsWith" && node.Object is MemberExpression startsMember)
        {
            var col = ResolveColumn(startsMember.Member.Name);
            var val = EvaluateMember((MemberExpression)node.Arguments[0]);
            return $"\"{col}\" LIKE {AddParam($"{val}%")}";
        }

        // string.EndsWith → LIKE '%value'
        if (node.Method.Name == "EndsWith" && node.Object is MemberExpression endsMember)
        {
            var col = ResolveColumn(endsMember.Member.Name);
            var val = EvaluateMember((MemberExpression)node.Arguments[0]);
            return $"\"{col}\" LIKE {AddParam($"%{val}")}";
        }

        throw new NotSupportedException($"Metoda '{node.Method.Name}' nije podržana u WHERE izrazu.");
    }

    private string NullCheck(Expression memberExpr, bool isEqual)
    {
        var col = ResolveColumn(((MemberExpression)memberExpr).Member.Name);
        return isEqual ? $"\"{col}\" IS NULL" : $"\"{col}\" IS NOT NULL";
    }

    private string ResolveColumn(string propertyName)
    {
        var col = _metadata.Columns.FirstOrDefault(c => c.Property.Name == propertyName);
        return col?.ColumnName ?? propertyName;
    }

    private string AddParam(object? value)
    {
        var name = $"@p{_paramCounter++}";
        _params.Add(new NpgsqlParameter(name, value ?? DBNull.Value));
        return name;
    }

    private static bool IsNullConstant(Expression node) =>
        node is ConstantExpression { Value: null };

    private static object? EvaluateMember(MemberExpression node)
    {
        // Kompajlira i izvršava expression da dobijemo stvarnu vrijednost closure-a
        var lambda = Expression.Lambda(node);
        return lambda.Compile().DynamicInvoke();
    }
}
