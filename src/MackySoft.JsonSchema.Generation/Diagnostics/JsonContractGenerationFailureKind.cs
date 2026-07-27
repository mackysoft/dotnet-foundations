namespace MackySoft.JsonSchema.Generation.Diagnostics;

/// <summary> Classifies a contract generation failure without prescribing product-specific handling. </summary>
public enum JsonContractGenerationFailureKind
{
    /// <summary> The requested contract identifier violates the public identifier syntax. </summary>
    InvalidContractId,

    /// <summary> One generation set associates a contract identifier with more than one contract. </summary>
    DuplicateContractId,

    /// <summary> A registered extension has an invalid stable identifier or contract version. </summary>
    InvalidExtensionIdentity,

    /// <summary> More than one extension of the same category uses the same stable identifier. </summary>
    DuplicateExtensionId,

    /// <summary> Serializer type information cannot describe a supported deterministic contract. </summary>
    UnsupportedTypeInfo,

    /// <summary> A serializer converter has no built-in interpretation or explicit type mapping. </summary>
    UnsupportedConverter,

    /// <summary> Metadata sources declare incompatible facts for the same contract target. </summary>
    ConflictingMetadata,

    /// <summary> More than one registered type mapper claims the same serializer contract. </summary>
    MultipleTypeMappers,

    /// <summary> A metadata value is malformed or incompatible with its target contract shape. </summary>
    InvalidMetadataValue,

    /// <summary> A model contribution targets an invalid location or contains an invalid declaration. </summary>
    InvalidModelContribution,

    /// <summary> Model contributions declare different values for the same target and name. </summary>
    ModelContributionConflict,

    /// <summary> A document extension is not an additive delivery-only vendor annotation. </summary>
    InvalidDocumentExtension,

    /// <summary> Document extensions declare different values for the same target and name. </summary>
    DocumentExtensionConflict,

    /// <summary> The canonical semantic projection could not be hashed into a contract digest. </summary>
    DigestGenerationFailed,
}
