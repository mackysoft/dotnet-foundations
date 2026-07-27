using System.Globalization;
using System.Text;
using MackySoft.JsonSchema.Generation.Annotations;
using MackySoft.JsonSchema.Generation.Internal.Determinism;

namespace MackySoft.JsonSchema.Generation.Internal.Metadata.Declarations;

internal sealed class ContractAttributeComparer : IComparer<Attribute>
{
    private ContractAttributeComparer ()
    {
    }

    internal static ContractAttributeComparer Instance { get; } = new();

    public int Compare (
        Attribute? left,
        Attribute? right)
    {
        if (ReferenceEquals(left, right))
        {
            return 0;
        }

        if (left is null)
        {
            return -1;
        }

        if (right is null)
        {
            return 1;
        }

        int typeComparison = UnicodeCodePointComparer.Instance.Compare(
            left.GetType().FullName,
            right.GetType().FullName);
        return typeComparison != 0
            ? typeComparison
            : string.CompareOrdinal(
                GetAttributeSortKey(left),
                GetAttributeSortKey(right));
    }

    private static string GetAttributeSortKey (Attribute attribute)
    {
        var key = new StringBuilder();
        switch (attribute)
        {
            case TitleAttribute title:
                AppendKeyPart(key, title.Title);
                break;

            case DescriptionAttribute description:
                AppendKeyPart(key, description.Description);
                break;

            case ExampleAttribute example:
                AppendKeyPart(key, example.Json);
                break;

            case ConstAttribute constant:
                AppendKeyPart(key, constant.Json);
                break;

            case EnumAttribute finiteSet:
                foreach (string value in finiteSet.JsonValues)
                {
                    AppendKeyPart(key, value);
                }
                break;

            case RangeAttribute range:
                AppendKeyPart(key, range.MinimumJson);
                AppendKeyPart(key, range.MaximumJson);
                key.Append(range.ExclusiveMinimum ? '1' : '0');
                key.Append(range.ExclusiveMaximum ? '1' : '0');
                break;

            case LengthAttribute length:
                AppendKeyPart(key, length.Minimum);
                AppendKeyPart(key, length.Maximum);
                break;

            case PatternAttribute pattern:
                AppendKeyPart(key, pattern.Pattern);
                break;

            case ItemCountAttribute itemCount:
                AppendKeyPart(key, itemCount.Minimum);
                AppendKeyPart(key, itemCount.Maximum);
                break;

            case PropertyCountAttribute propertyCount:
                AppendKeyPart(key, propertyCount.Minimum);
                AppendKeyPart(key, propertyCount.Maximum);
                break;

            case OneOfBranchAttribute branch:
                AppendKeyPart(key, branch.Name);
                foreach (string propertyName in branch.RequiredPropertyNames)
                {
                    AppendKeyPart(key, propertyName);
                }
                AppendKeyPart(key, branch.DiscriminatorValueJson);
                AppendKeyPart(key, branch.Description);
                AppendKeyPart(key, branch.ExampleJson);
                break;

            case DiscriminatorAttribute discriminator:
                AppendKeyPart(key, discriminator.PropertyName);
                break;
        }

        return key.ToString();
    }

    private static void AppendKeyPart (
        StringBuilder builder,
        string? value)
    {
        if (value is null)
        {
            builder.Append("-:");
            return;
        }

        builder.Append(value.Length.ToString(CultureInfo.InvariantCulture));
        builder.Append(':');
        builder.Append(value);
    }

    private static void AppendKeyPart (
        StringBuilder builder,
        int value)
    {
        AppendKeyPart(builder, value.ToString(CultureInfo.InvariantCulture));
    }
}
