namespace CP2077SaveExporter;

// -------------------------------------------------------------------------
// Root
// -------------------------------------------------------------------------

/// <summary>Full export: header plus raw / normalized / derived analysis layers.</summary>
public sealed class ExportSnapshot
{
    public SaveHeaderDto Header { get; init; } = new();
    public RawSectionDto Raw { get; init; } = new();
    public NormalizedSectionDto Normalized { get; init; } = new();
    public DerivedSectionDto Derived { get; init; } = new();
}

public sealed class SaveHeaderDto
{
    public uint SaveVersion { get; init; }
    public uint GameVersion { get; init; }
    public string GameDefPath { get; init; } = "";
    public ulong TimeStamp { get; init; }
    public uint ArchiveVersion { get; init; }
}

// -------------------------------------------------------------------------
// Raw (verbatim IDs, hashes, unmerged lists)
// -------------------------------------------------------------------------

public sealed class RawSectionDto
{
    public int? Level { get; init; }
    public int? StreetCred { get; init; }
    public MoneyDto Money { get; init; } = new();

    /// <summary>All player-development attribute rows as stored (stat enum name + value).</summary>
    public IReadOnlyList<AttributeFlatDto> AttributesFlat { get; init; } = Array.Empty<AttributeFlatDto>();

    public InventoryRawDto Inventory { get; init; } = new();
    public EquipmentRawDto Equipment { get; init; } = new();

    public IReadOnlyList<QuestFactRawDto> QuestFactsAll { get; init; } = Array.Empty<QuestFactRawDto>();

    public IReadOnlyList<FastTravelPointRawDto> FastTravelPoints { get; init; } = Array.Empty<FastTravelPointRawDto>();
}

public sealed class AttributeFlatDto
{
    public string StatType { get; init; } = "";
    public int Value { get; init; }
}

public sealed class InventoryRawDto
{
    public InventorySummaryDto Summary { get; init; } = new();
    public IReadOnlyList<InventoryItemRawDto> ItemRows { get; init; } = Array.Empty<InventoryItemRawDto>();
}

public sealed class InventorySummaryDto
{
    public int SubInventoryCount { get; init; }
    public int TotalItemStacks { get; init; }
    public IReadOnlyList<SubInventorySummaryDto> SubInventories { get; init; } = Array.Empty<SubInventorySummaryDto>();
}

public sealed class SubInventorySummaryDto
{
    public string InventoryId { get; init; } = "";
    public int ItemCount { get; init; }
}

/// <summary>One inventory stack as read from save (hashes preserved).</summary>
public sealed class InventoryItemRawDto
{
    public string InventoryId { get; init; } = "";
    /// <summary>Full 64-bit TweakDB id value (same as game).</summary>
    public string TweakDbIdUlong { get; init; } = "";
    public string? ItemIdResolved { get; init; }
    /// <summary>RED string form of TweakDBID (often includes hash and length).</summary>
    public string ItemIdPresentation { get; init; } = "";
    public ulong Quantity { get; init; }
    public string Flags { get; init; } = "";
    public uint CreationTime { get; init; }
    public string ItemStructure { get; init; } = "";
}

public sealed class EquipmentRawDto
{
    public IReadOnlyList<EquippedSlotRawDto> AllSlots { get; init; } = Array.Empty<EquippedSlotRawDto>();
    public IReadOnlyList<CyberwareSlotRawDto> CyberwareSlots { get; init; } = Array.Empty<CyberwareSlotRawDto>();
}

public sealed class EquippedSlotRawDto
{
    public string AreaTypeLabel { get; init; } = "";
    public string AreaTypeEnum { get; init; } = "";
    public string TweakDbIdUlong { get; init; } = "";
    public string? ItemIdResolved { get; init; }
    public string ItemIdPresentation { get; init; } = "";
}

public sealed class CyberwareSlotRawDto
{
    public string AreaTypeEnum { get; init; } = "";
    public string TweakDbIdUlong { get; init; } = "";
    public string? ItemIdResolved { get; init; }
    public string ItemIdPresentation { get; init; } = "";
}

public sealed class QuestFactRawDto
{
    public uint FactHash { get; init; }
    public string FactHashHex { get; init; } = "";
    public string? Name { get; init; }
    public uint Value { get; init; }
}

public sealed class FastTravelPointRawDto
{
    public string? PointRecordResolved { get; init; }
    public string PointRecordTweakDbUlong { get; init; } = "";
    public string? MarkerRefResolved { get; init; }
    public string MarkerRefUlong { get; init; } = "";
    public bool IsEp1 { get; init; }
}

public sealed class MoneyDto
{
    public ulong? InventoryItemsMoneyQuantity { get; init; }
    public float? StatPoolsSystemCurrency { get; init; }
}

// -------------------------------------------------------------------------
// Normalized (cleaner shapes, categories, deduped equipment)
// -------------------------------------------------------------------------

public sealed class NormalizedSectionDto
{
    public CoreAttributesDto CoreAttributes { get; init; } = new();
    /// <summary>Non-core attribute stats (enum name -> points).</summary>
    public IReadOnlyDictionary<string, int> OtherStats { get; init; } =
        new Dictionary<string, int>();

    public InventoryNormalizedDto Inventory { get; init; } = new();
    public EquipmentNormalizedDto Equipment { get; init; } = new();

    public IReadOnlyList<QuestFactRawDto> QuestFactsNamedNonZero { get; init; } = Array.Empty<QuestFactRawDto>();
}

/// <summary>Gameplay-oriented labels; raw <see cref="AttributeFlatDto"/> still uses engine enum names (Strength, TechnicalAbility).</summary>
public sealed class CoreAttributesDto
{
    public int? Body { get; init; }
    public int? Reflexes { get; init; }
    public int? Intelligence { get; init; }
    public int? Technical { get; init; }
    public int? Cool { get; init; }
}

public sealed class InventoryNormalizedDto
{
    public InventorySummaryDto Summary { get; init; } = new();
    public IReadOnlyList<InventoryItemNormalizedDto> ItemRows { get; init; } = Array.Empty<InventoryItemNormalizedDto>();
}

public sealed class InventoryItemNormalizedDto
{
    public string InventoryId { get; init; } = "";
    public string TweakDbIdUlong { get; init; } = "";
    public string? ItemIdResolved { get; init; }
    public string ItemIdPresentation { get; init; } = "";
    public ulong Quantity { get; init; }
    public string InferredCategory { get; init; } = "";
    /// <summary>True when this stack's TweakDB id matches any equipped weapon/clothing slot or cyberware slot (best-effort; same base id as equipped).</summary>
    public bool AppearsEquipped { get; init; }

    /// <summary>tweakdbid_match | not_matched — see <see cref="AppearsEquipped"/>.</summary>
    public string AppearsEquippedReason { get; init; } = "not_matched";
}

public sealed class EquipmentNormalizedDto
{
    /// <summary>Equipped items with duplicate (area + item) rows merged.</summary>
    public IReadOnlyList<EquippedSlotNormalizedDto> SlotsDeduped { get; init; } = Array.Empty<EquippedSlotNormalizedDto>();
    public IReadOnlyList<CyberwareSlotNormalizedDto> CyberwareDeduped { get; init; } = Array.Empty<CyberwareSlotNormalizedDto>();

    /// <summary>One entry per equipped slot row (no TweakDB id merge).</summary>
    public IReadOnlyList<EquippedSlotInstanceDto> SlotInstances { get; init; } = Array.Empty<EquippedSlotInstanceDto>();

    /// <summary>One entry per cyberware slot row (no TweakDB id merge).</summary>
    public IReadOnlyList<CyberwareSlotInstanceDto> CyberwareInstances { get; init; } = Array.Empty<CyberwareSlotInstanceDto>();
}

/// <summary>Slot-faithful loadout row (normalized copy of raw equipped slot).</summary>
public sealed class EquippedSlotInstanceDto
{
    public string AreaTypeLabel { get; init; } = "";
    public string AreaTypeEnum { get; init; } = "";
    public string TweakDbIdUlong { get; init; } = "";
    public string? ItemIdResolved { get; init; }
    public string ItemIdPresentation { get; init; } = "";
}

/// <summary>Slot-faithful cyberware row (normalized copy of raw cyberware slot).</summary>
public sealed class CyberwareSlotInstanceDto
{
    public string AreaTypeEnum { get; init; } = "";
    public string TweakDbIdUlong { get; init; } = "";
    public string? ItemIdResolved { get; init; }
    public string ItemIdPresentation { get; init; } = "";
}

public sealed class EquippedSlotNormalizedDto
{
    public string AreaTypeLabel { get; init; } = "";
    public string AreaTypeEnum { get; init; } = "";
    public string TweakDbIdUlong { get; init; } = "";
    public string? ItemIdResolved { get; init; }
    /// <summary>How many raw rows were merged for this TweakDB id (cross-slot duplicates).</summary>
    public int DuplicateCount { get; init; }
    /// <summary>Non-canonical area enums merged into this row (e.g. WeaponWheel when Weapon was chosen).</summary>
    public IReadOnlyList<string> SuppressedAreaTypeEnums { get; init; } = Array.Empty<string>();
}

public sealed class CyberwareSlotNormalizedDto
{
    public string AreaTypeEnum { get; init; } = "";
    public string TweakDbIdUlong { get; init; } = "";
    public string? ItemIdResolved { get; init; }
    public int DuplicateCount { get; init; }
    public IReadOnlyList<string> SuppressedAreaTypeEnums { get; init; } = Array.Empty<string>();
}

// -------------------------------------------------------------------------
// Derived (heuristic tags, aggregates — not read directly from save bytes)
// -------------------------------------------------------------------------

public sealed class DerivedSectionDto
{
    public IReadOnlyList<string> BuildTags { get; init; } = Array.Empty<string>();
    public string? ProgressionStage { get; init; }
    public QuestFactSummaryDto QuestFactSummary { get; init; } = new();
    public QuestSignalSummaryDto QuestSignals { get; init; } = new();
    public DerivedInventorySummaryDto InventorySummary { get; init; } = new();
    public DerivedEquipmentSummaryDto EquipmentSummary { get; init; } = new();

    public DerivedCompletionDto Completion { get; init; } = new();
    public DerivedExpansionDto Expansion { get; init; } = new();
    public DerivedInventoryInsightsDto InventoryInsights { get; init; } = new();
    public DerivedEquipmentMaturityDto EquipmentMaturity { get; init; } = new();
    public DerivedBuildProfileDto BuildProfile { get; init; } = new();
    public DerivedExplorationDto Exploration { get; init; } = new();
}

public sealed class DerivedCompletionDto
{
    public CompletionQuestBucketDto MainQuest { get; init; } = new();
    public CompletionQuestBucketDto SideQuest { get; init; } = new();
    public CompletionQuestBucketDto Gigs { get; init; } = new();

    /// <summary>inProgress counts are substring-based heuristics, not exact game state.</summary>
    public string InProgressInterpretation { get; init; } = "approximate";

    /// <summary>low | medium | high — confidence in inProgress totals only.</summary>
    public string InProgressConfidence { get; init; } = "low";
}

public sealed class CompletionQuestBucketDto
{
    public int Completed { get; init; }

    /// <summary>
    /// Counts facts whose names look in-progress (_active / _start). Many games reuse similar tokens;
    /// treat as a loose signal, not an exact active-quest list.
    /// </summary>
    public int InProgress { get; init; }
}

public sealed class DerivedExpansionDto
{
    public PhantomLibertySignalsDto PhantomLiberty { get; init; } = new();
}

public sealed class PhantomLibertySignalsDto
{
    public bool Detected { get; init; }
    public int ProgressionSignals { get; init; }
    public int FastTravelUnlocked { get; init; }
    public bool LikelyStarted { get; init; }
    public string LikelyProgressLevel { get; init; } = "none";
}

public sealed class DerivedInventoryInsightsDto
{
    public bool HasQuickhacks { get; init; }
    public int QuickhackProgramCount { get; init; }
    public int QuickhackMaterialCount { get; init; }
    public bool HasMultipleOperatingSystems { get; init; }
    public int OperatingSystemCount { get; init; }
    public int IconicItemCount { get; init; }

    /// <summary>low | medium | high — crafting stacks + cash heuristic.</summary>
    public string MaterialWealth { get; init; } = "low";

    /// <summary>low | medium | high — level + crafting + cash heuristic.</summary>
    public string UpgradeReadiness { get; init; } = "low";
}

public sealed class DerivedEquipmentMaturityDto
{
    public string AverageTier { get; init; } = "unknown";
    public int CyberwareSlotsUsed { get; init; }

    /// <summary>Body cyberware slot budget aligned with exporter equipment areas (not save-parsed).</summary>
    public int CyberwareSlotsPossible { get; init; }

    /// <summary>slotsUsed / slotsPossible, capped at 1.0.</summary>
    public double CyberwareUtilizationRatio { get; init; }

    public string UpgradePotential { get; init; } = "low";
}

public sealed class DerivedBuildProfileDto
{
    public string Primary { get; init; } = "hybrid-generalist";
    public string Secondary { get; init; } = "hybrid-generalist";

    /// <summary>low | medium | high</summary>
    public string Confidence { get; init; } = "low";

    public IReadOnlyList<string> Signals { get; init; } = Array.Empty<string>();
}

public sealed class DerivedExplorationDto
{
    /// <summary>0–1 heuristic from unlocked fast-travel point count.</summary>
    public double EstimatedCoverage { get; init; }
}

public sealed class DerivedInventorySummaryDto
{
    public int WeaponCount { get; init; }
    public int CyberwareCount { get; init; }
    public int ConsumableCount { get; init; }
    public int ClothingCount { get; init; }
    public int QuickhackCount { get; init; }
    public int QuestItemCount { get; init; }
    public int AmmoCount { get; init; }
    /// <summary>Distinct TweakDB id values across inventory rows (one per item type id).</summary>
    public int UniqueItemTypes { get; init; }
}

public sealed class DerivedEquipmentSummaryDto
{
    public int TotalEquippedWeapons { get; init; }
    public int TotalEquippedCyberware { get; init; }
}

public sealed class QuestFactSummaryDto
{
    public int Total { get; init; }
    public int WithCatalogName { get; init; }
    public int NonZeroValue { get; init; }
    public int ZeroValue { get; init; }
    public int NamedNonZero { get; init; }
}

public sealed class QuestSignalSummaryDto
{
    public int NamedFactsMatchingQuestPrefix { get; init; }
    public int NamedFactsContainingDone { get; init; }
    public int NamedFactsContainingFailed { get; init; }
    public int NamedFactsEp1Hint { get; init; }

    /// <summary>Named facts: main-line style (prefix mq) and completion suffix.</summary>
    public int MainQuestCompleted { get; init; }

    /// <summary>Named facts: side-quest style (prefix sq) and completion suffix.</summary>
    public int SideQuestCompleted { get; init; }

    /// <summary>Named facts: open-world / gig style (ma_ or sts_) and completion suffix.</summary>
    public int GigsCompleted { get; init; }
}
