using System.Reflection;
using International.EInvoicing.Model;
using International.EInvoicing.Values;

namespace International.EInvoicing.Playground.Services;

/// <summary>One row of the explorer: a field, everything it carries, and how deep it sits.</summary>
public sealed record FieldRow(int Depth, string Name, IField Field);

/// <summary>One node of the explorer: a model object, its fields, and the nodes below it.</summary>
public sealed record NodeRow(int Depth, string Name, IReadOnlyList<FieldRow> Fields, IReadOnlyList<NodeRow> Children);

/// <summary>
/// Walks a model object and lists what it carries.
/// </summary>
/// <remarks>
/// This is the one place reflection is acceptable: it exists to show a person what the model holds, not to
/// read or write documents. Serialisation never uses it.
/// </remarks>
public static class FieldWalker
{
    /// <summary>Walks <paramref name="node"/>, listing its fields and the nodes below it.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="node"/> is <c>null</c>.</exception>
    public static NodeRow Walk(object node, string name, int depth = 0)
    {
        ArgumentNullException.ThrowIfNull(node);

        var fields = new List<FieldRow>();
        var children = new List<NodeRow>();

        foreach (PropertyInfo property in node.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            object? value = Read(property, node);

            switch (value)
            {
                case null:
                    continue;

                case IField field when field.IsSet:
                    fields.Add(new FieldRow(depth + 1, property.Name, field));
                    break;

                case InvoiceNode child:
                    children.Add(Walk(child, property.Name, depth + 1));
                    break;

                case System.Collections.IEnumerable items and not string:
                    children.AddRange(WalkAll(items, property.Name, depth + 1));
                    break;
            }
        }

        return new NodeRow(depth, name, fields, children);
    }

    private static IEnumerable<NodeRow> WalkAll(System.Collections.IEnumerable items, string name, int depth)
    {
        int index = 0;
        foreach (object? item in items)
        {
            if (item is InvoiceNode node)
            {
                yield return Walk(node, $"{name}[{index}]", depth);
            }

            index++;
        }
    }

    private static object? Read(PropertyInfo property, object node)
    {
        try
        {
            return property.GetIndexParameters().Length == 0 ? property.GetValue(node) : null;
        }
        catch (TargetInvocationException)
        {
            return null;
        }
    }
}
