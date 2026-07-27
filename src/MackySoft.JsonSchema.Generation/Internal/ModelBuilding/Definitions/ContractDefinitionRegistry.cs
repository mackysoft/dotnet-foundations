using System.Globalization;
using System.Text.Json;
using MackySoft.JsonSchema.Generation.ContractModel;

namespace MackySoft.JsonSchema.Generation.Internal.ModelBuilding.Definitions;

/// <summary>
/// Owns deterministic definition identities, pending traversal work, and
/// completion ordering.
/// </summary>
internal sealed class ContractDefinitionRegistry
{
    private readonly Dictionary<DefinitionKey, DefinitionRegistration>
        registrations = new();
    private readonly Dictionary<string, DefinitionRegistration>
        registrationsById = new(StringComparer.Ordinal);
    private readonly Queue<DefinitionRegistration> pending = new();

    internal DefinitionRegistration GetOrAdd (
        DefinitionKey key,
        JsonElement? discriminatorValue)
    {
        if (registrations.TryGetValue(
            key,
            out DefinitionRegistration? existing))
        {
            return existing;
        }

        int ordinal = registrations.Count;
        var registration = new DefinitionRegistration(
            key,
            $"d{ordinal.ToString(CultureInfo.InvariantCulture)}",
            ordinal,
            discriminatorValue);
        registrations.Add(key, registration);
        registrationsById.Add(registration.Id, registration);
        pending.Enqueue(registration);
        return registration;
    }

    internal bool TryDequeuePending (
        out DefinitionRegistration? registration)
    {
        return pending.TryDequeue(out registration);
    }

    internal JsonContractNode ResolveCompleted (string id)
    {
        if (id is null)
        {
            throw new ArgumentNullException(nameof(id));
        }

        if (!registrationsById.TryGetValue(
                id,
                out DefinitionRegistration? registration))
        {
            throw new InvalidOperationException(
                $"Definition '{id}' was not registered.");
        }

        return registration.Value
            ?? throw new InvalidOperationException(
                $"Definition '{id}' was not completed.");
    }

    internal IReadOnlyList<JsonContractDefinition> GetCompletedDefinitions ()
    {
        JsonContractDefinition[] completed = registrations.Values
            .OrderBy(static registration => registration.Ordinal)
            .Select(
                static registration =>
                    new JsonContractDefinition(
                        registration.Id,
                        registration.Value
                            ?? throw new InvalidOperationException(
                                $"Definition '{registration.Id}' was not completed."),
                        new JsonContractSource(
                            registration.Key.Type,
                            member: null)))
            .ToArray();
        return Array.AsReadOnly(completed);
    }
}
