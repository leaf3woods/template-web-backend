using System.Buffers.Binary;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Org.Product.Domain.Shared.Expressions;

public class FingerprintVisitor : ExpressionVisitor
{
    private readonly List<byte> _fingerprintbuffer = [];
    private readonly Dictionary<ParameterExpression, int> _visitedParameters = [];
    private bool _isSupported = true;

    public static bool TryCreate(Expression expression, out string fingerprint)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var visitor = new FingerprintVisitor();
        visitor.Visit(expression);
        if (!visitor._isSupported)
        {
            fingerprint = string.Empty;
            return false;
        }

        fingerprint = visitor.ToString();
        return true;
    }

    public override Expression? Visit(Expression? node)
    {
        if (node is null)
        {
            _fingerprintbuffer.Add(0);
            return null;
        }

        if (!IsSupportedNodeType(node.NodeType))
        {
            _isSupported = false;
            return node;
        }

        _fingerprintbuffer.Add(1);
        AddInt32((int)node.NodeType);
        AddType(node.Type);
        return base.Visit(node);
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        if (!_visitedParameters.TryGetValue(node, out var id))
        {
            id = _visitedParameters.Count;
            _visitedParameters.Add(node, id);
        }

        AddInt32(id);
        return base.VisitParameter(node);
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        AddValue(node.Value);
        return base.VisitConstant(node);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (TryGetCapturedValue(node, out var value))
        {
            _fingerprintbuffer.Add(1);
            AddValue(value);
            return node;
        }

        if (node.Expression is null)
        {
            _isSupported = false;
            return node;
        }

        _fingerprintbuffer.Add(0);
        AddMember(node.Member);
        return base.VisitMember(node);
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        AddOptionalMethod(node.Method);
        _fingerprintbuffer.Add(node.IsLifted ? (byte)1 : (byte)0);
        _fingerprintbuffer.Add(node.IsLiftedToNull ? (byte)1 : (byte)0);
        return base.VisitBinary(node);
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        AddOptionalMethod(node.Method);
        _fingerprintbuffer.Add(node.IsLifted ? (byte)1 : (byte)0);
        _fingerprintbuffer.Add(node.IsLiftedToNull ? (byte)1 : (byte)0);
        return base.VisitUnary(node);
    }

    private static bool IsSupportedNodeType(ExpressionType nodeType) =>
        nodeType
            is ExpressionType.Lambda
                or ExpressionType.Parameter
                or ExpressionType.Constant
                or ExpressionType.MemberAccess
                or ExpressionType.Equal
                or ExpressionType.NotEqual
                or ExpressionType.GreaterThan
                or ExpressionType.GreaterThanOrEqual
                or ExpressionType.LessThan
                or ExpressionType.LessThanOrEqual
                or ExpressionType.AndAlso
                or ExpressionType.OrElse
                or ExpressionType.Not
                or ExpressionType.Convert
                or ExpressionType.ConvertChecked;

    private static bool TryGetCapturedValue(MemberExpression expression, out object? value)
    {
        if (expression.Expression is ConstantExpression constant)
        {
            value = GetMemberValue(constant.Value, expression.Member);
            return true;
        }

        if (
            expression.Expression is MemberExpression parent
            && TryGetCapturedValue(parent, out var owner)
        )
        {
            value = GetMemberValue(owner, expression.Member);
            return true;
        }

        value = null;
        return false;
    }

    private static object? GetMemberValue(object? owner, MemberInfo member) =>
        member switch
        {
            FieldInfo field => field.GetValue(owner),
            PropertyInfo property => property.GetValue(owner),
            _ => throw new NotSupportedException($"Unsupported captured member: {member.Name}"),
        };

    private void AddMember(MemberInfo member)
    {
        AddNullableType(member.DeclaringType);
        AddInt32((int)member.MemberType);
        AddString(member.Name);

        var memberType = member switch
        {
            FieldInfo field => field.FieldType,
            PropertyInfo property => property.PropertyType,
            _ => throw new NotSupportedException($"Unsupported member: {member.Name}"),
        };
        AddType(memberType);
    }

    private void AddOptionalMethod(MethodInfo? method)
    {
        if (method is null)
        {
            _fingerprintbuffer.Add(0);
            return;
        }

        _fingerprintbuffer.Add(1);
        AddMethod(method);
    }

    private void AddMethod(MethodInfo method)
    {
        AddNullableType(method.DeclaringType);
        AddString(method.Name);
        AddType(method.ReturnType);
        _fingerprintbuffer.Add(method.IsStatic ? (byte)1 : (byte)0);

        var genericArguments = method.GetGenericArguments();
        AddInt32(genericArguments.Length);
        foreach (var argument in genericArguments)
        {
            AddType(argument);
        }

        var parameters = method.GetParameters();
        AddInt32(parameters.Length);
        foreach (var parameter in parameters)
        {
            AddType(parameter.ParameterType);
        }
    }

    private void AddValue(object? value)
    {
        if (value is null)
        {
            _fingerprintbuffer.Add(0);
            return;
        }

        _fingerprintbuffer.Add(1);
        var type = value.GetType();
        AddType(type);

        if (value is byte[] bytes)
        {
            AddBytes(bytes);
            return;
        }

        if (!IsSupportedScalar(type))
        {
            _isSupported = false;
            return;
        }

        var text = value switch
        {
            DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString(
                "O",
                CultureInfo.InvariantCulture
            ),
            DateOnly date => date.ToString("O", CultureInfo.InvariantCulture),
            TimeOnly time => time.ToString("O", CultureInfo.InvariantCulture),
            TimeSpan duration => duration.ToString("c", CultureInfo.InvariantCulture),
            Guid guid => guid.ToString("D"),
            Enum enumeration => enumeration.ToString("D"),
            string valueText => valueText,
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => null,
        };

        if (text is null)
        {
            _isSupported = false;
            return;
        }

        AddString(text);
    }

    private static bool IsSupportedScalar(Type type) =>
        type.IsPrimitive
        || type.IsEnum
        || type == typeof(string)
        || type == typeof(decimal)
        || type == typeof(Guid)
        || type == typeof(DateTime)
        || type == typeof(DateTimeOffset)
        || type == typeof(DateOnly)
        || type == typeof(TimeOnly)
        || type == typeof(TimeSpan);

    private void AddNullableType(Type? type)
    {
        if (type is null)
        {
            AddString(null);
            return;
        }

        AddType(type);
    }

    private void AddType(Type type) =>
        AddString(type.AssemblyQualifiedName ?? type.FullName ?? type.Name);

    private void AddString(string? value)
    {
        if (value is null)
        {
            AddInt32(-1);
            return;
        }

        AddBytes(Encoding.UTF8.GetBytes(value));
    }

    private void AddBytes(ReadOnlySpan<byte> bytes)
    {
        AddInt32(bytes.Length);
        foreach (var item in bytes)
        {
            _fingerprintbuffer.Add(item);
        }
    }

    private void AddInt32(int value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
        foreach (var item in bytes)
        {
            _fingerprintbuffer.Add(item);
        }
    }

    public override string ToString()
    {
        if (!_isSupported)
        {
            throw new InvalidOperationException(
                "The expression cannot be fingerprinted deterministically."
            );
        }

        var hash = SHA256.HashData([.. _fingerprintbuffer]);
        return Convert.ToBase64String(hash);
    }
}
