using System.Text.RegularExpressions;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.BuildingBlocks.SharedKernel;

namespace SchoolOS.PlatformModules.Subscriptions.Domain;

/// <summary>
/// Platform-wide sellable or core functional module corresponding to <see cref="ModuleCode"/>.
/// </summary>
public sealed class PortalModule : Entity<Guid>
{
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 500;

    private PortalModule()
    {
        Name = string.Empty;
        Description = string.Empty;
    }

    private PortalModule(ModuleCode code, string name, string description, bool isCore, int sortOrder) : this()
    {
        if (code == ModuleCode.None)
        {
            throw new ArgumentException("Module code cannot be None.", nameof(code));
        }

        Id = Guid.CreateVersion7();
        Code = code;
        IsCore = isCore;
        SortOrder = sortOrder;
        IsActive = true;
        Update(name, description);
    }

    public ModuleCode Code { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public bool IsCore { get; private set; }
    public bool IsActive { get; private set; }
    public int SortOrder { get; private set; }

    public static PortalModule Create(ModuleCode code, string name, string description, bool isCore, int sortOrder) =>
        new(code, name, description, isCore, sortOrder);

    public void Update(string name, string description, bool? isActive = null, int? sortOrder = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(description);

        var trimmedName = name.Trim();
        var trimmedDesc = description.Trim();
        if (trimmedName.Length > NameMaxLength)
        {
            throw new ArgumentOutOfRangeException(nameof(name));
        }

        if (trimmedDesc.Length > DescriptionMaxLength)
        {
            throw new ArgumentOutOfRangeException(nameof(description));
        }

        Name = trimmedName;
        Description = trimmedDesc;
        if (isActive.HasValue)
        {
            IsActive = isActive.Value;
        }

        if (sortOrder.HasValue)
        {
            SortOrder = sortOrder.Value;
        }
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}

/// <summary>
/// Directed dependency edge: <see cref="ModuleId"/> requires <see cref="RequiresModuleId"/>.
/// </summary>
public sealed class ModuleDependency : Entity<Guid>
{
    private ModuleDependency()
    {
    }

    private ModuleDependency(Guid moduleId, Guid requiresModuleId)
    {
        if (moduleId == Guid.Empty || requiresModuleId == Guid.Empty)
        {
            throw new ArgumentException("Module IDs cannot be empty.");
        }

        if (moduleId == requiresModuleId)
        {
            throw new ArgumentException("A module cannot depend on itself.", nameof(requiresModuleId));
        }

        Id = Guid.CreateVersion7();
        ModuleId = moduleId;
        RequiresModuleId = requiresModuleId;
    }

    public Guid ModuleId { get; private set; }
    public Guid RequiresModuleId { get; private set; }

    public static ModuleDependency Create(Guid moduleId, Guid requiresModuleId) =>
        new(moduleId, requiresModuleId);
}

/// <summary>
/// DAG resolver computing the transitive dependency closure for selected modules.
/// </summary>
public static class ModuleDependencyGraph
{
    public static IReadOnlySet<Guid> RequiredBy(
        IReadOnlyCollection<(Guid ModuleId, Guid RequiresModuleId)> dependencies,
        IEnumerable<Guid> chosenModuleIds)
    {
        ArgumentNullException.ThrowIfNull(dependencies);
        ArgumentNullException.ThrowIfNull(chosenModuleIds);

        var map = dependencies
            .GroupBy(d => d.ModuleId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.RequiresModuleId).ToList());

        var result = new HashSet<Guid>();
        var queue = new Queue<Guid>();

        foreach (var id in chosenModuleIds)
        {
            if (result.Add(id))
            {
                queue.Enqueue(id);
            }
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!map.TryGetValue(current, out var required))
            {
                continue;
            }

            foreach (var dep in required)
            {
                if (result.Add(dep))
                {
                    queue.Enqueue(dep);
                }
            }
        }

        return result;
    }

    public static bool WouldCreateCycle(
        IReadOnlyCollection<(Guid ModuleId, Guid RequiresModuleId)> existingDependencies,
        Guid moduleId,
        Guid requiresModuleId)
    {
        if (moduleId == requiresModuleId)
        {
            return true;
        }

        var closure = RequiredBy(existingDependencies, [requiresModuleId]);
        return closure.Contains(moduleId);
    }
}

/// <summary>
/// Commercial bundle of paid modules sold under a plan code such as <c>STARTER</c> or <c>FULL</c>.
/// </summary>
public sealed partial class Plan : Entity<Guid>
{
    public const int CodeMaxLength = 50;
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 500;

    private readonly List<PlanModule> _modules = [];

    private Plan()
    {
        Code = string.Empty;
        Name = string.Empty;
        Description = string.Empty;
    }

    private Plan(string code, string name, string description) : this()
    {
        var normalized = (code ?? string.Empty).Trim().ToUpperInvariant();
        if (!IsValidCode(normalized))
        {
            throw new ArgumentException("Plan code must use uppercase letters, digits, or underscores starting with a letter.", nameof(code));
        }

        Id = Guid.CreateVersion7();
        Code = normalized;
        IsActive = true;
        Update(name, description);
    }

    public string Code { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public bool IsActive { get; private set; }
    public IReadOnlyCollection<PlanModule> Modules => _modules.AsReadOnly();

    public static Plan Create(string code, string name, string description) => new(code, name, description);

    public void Update(string name, string description, bool? isActive = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(description);

        var trimmedName = name.Trim();
        var trimmedDesc = description.Trim();
        if (trimmedName.Length > NameMaxLength)
        {
            throw new ArgumentOutOfRangeException(nameof(name));
        }

        if (trimmedDesc.Length > DescriptionMaxLength)
        {
            throw new ArgumentOutOfRangeException(nameof(description));
        }

        Name = trimmedName;
        Description = trimmedDesc;
        if (isActive.HasValue)
        {
            IsActive = isActive.Value;
        }
    }

    public void AddModule(PortalModule module)
    {
        ArgumentNullException.ThrowIfNull(module);
        if (module.IsCore)
        {
            throw new ArgumentException("Core modules are included in every subscription and are not added explicitly to plans.", nameof(module));
        }

        if (_modules.Any(m => m.ModuleId == module.Id))
        {
            return;
        }

        _modules.Add(PlanModule.Create(this, module));
    }

    public static bool IsValidCode(string? code) =>
        code is not null && code.Length <= CodeMaxLength && CodePattern().IsMatch(code);

    [GeneratedRegex("^[A-Z][A-Z0-9_]*$")]
    private static partial Regex CodePattern();
}

/// <summary>
/// Join entity linking a commercial <see cref="Plan"/> to an included paid <see cref="PortalModule"/>.
/// </summary>
public sealed class PlanModule : Entity<Guid>
{
    private PlanModule()
    {
    }

    private PlanModule(Guid planId, Guid moduleId)
    {
        Id = Guid.CreateVersion7();
        PlanId = planId;
        ModuleId = moduleId;
    }

    public Guid PlanId { get; private set; }
    public Guid ModuleId { get; private set; }

    public static PlanModule Create(Plan plan, PortalModule module)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(module);
        if (module.IsCore)
        {
            throw new ArgumentException("Core modules cannot be added explicitly to plans.", nameof(module));
        }

        return new PlanModule(plan.Id, module.Id);
    }
}

/// <summary>
/// Country-specific currency and sales tax schedule for pricing plans and add-on modules.
/// </summary>
public sealed partial class PriceBook : Entity<Guid>
{
    private readonly List<PriceBookItem> _items = [];

    private PriceBook()
    {
        CountryCode = string.Empty;
        CurrencyCode = string.Empty;
    }

    private PriceBook(string countryCode, string currencyCode, decimal taxRate) : this()
    {
        var normCountry = (countryCode ?? string.Empty).Trim().ToUpperInvariant();
        var normCurrency = (currencyCode ?? string.Empty).Trim().ToUpperInvariant();
        if (!Iso2Regex().IsMatch(normCountry))
        {
            throw new ArgumentException("CountryCode must be ISO 3166-1 alpha-2.", nameof(countryCode));
        }

        if (!Iso3Regex().IsMatch(normCurrency))
        {
            throw new ArgumentException("CurrencyCode must be ISO 4217.", nameof(currencyCode));
        }

        if (taxRate < 0m || taxRate > 100m)
        {
            throw new ArgumentOutOfRangeException(nameof(taxRate), "Tax rate percentage must be between 0 and 100.");
        }

        Id = Guid.CreateVersion7();
        CountryCode = normCountry;
        CurrencyCode = normCurrency;
        TaxRate = decimal.Round(taxRate, 2, MidpointRounding.AwayFromZero);
        IsActive = true;
    }

    public string CountryCode { get; private set; }
    public string CurrencyCode { get; private set; }
    public decimal TaxRate { get; private set; }
    public bool IsActive { get; private set; }
    public IReadOnlyCollection<PriceBookItem> Items => _items.AsReadOnly();

    public static PriceBook Create(string countryCode, string currencyCode, decimal taxRate) =>
        new(countryCode, currencyCode, taxRate);

    public PriceBookItem SetPlanPrice(Guid planId, BillingCycle billingCycle, decimal price)
    {
        var existing = _items.FirstOrDefault(i => i.PlanId == planId && i.BillingCycle == billingCycle);
        if (existing is not null)
        {
            existing.UpdatePrice(price);
            return existing;
        }

        var item = PriceBookItem.ForPlan(Id, planId, billingCycle, price);
        _items.Add(item);
        return item;
    }

    public PriceBookItem SetModulePrice(Guid moduleId, BillingCycle billingCycle, decimal price)
    {
        var existing = _items.FirstOrDefault(i => i.ModuleId == moduleId && i.BillingCycle == billingCycle);
        if (existing is not null)
        {
            existing.UpdatePrice(price);
            return existing;
        }

        var item = PriceBookItem.ForModule(Id, moduleId, billingCycle, price);
        _items.Add(item);
        return item;
    }

    public void Deactivate() => IsActive = false;

    [GeneratedRegex("^[A-Z]{2}$")]
    private static partial Regex Iso2Regex();

    [GeneratedRegex("^[A-Z]{3}$")]
    private static partial Regex Iso3Regex();
}

/// <summary>
/// Line price in a <see cref="PriceBook"/> for either a <see cref="Plan"/> or an add-on <see cref="PortalModule"/>.
/// </summary>
public sealed class PriceBookItem : Entity<Guid>
{
    private PriceBookItem()
    {
    }

    private PriceBookItem(Guid priceBookId, Guid? planId, Guid? moduleId, BillingCycle billingCycle, decimal price)
    {
        if (priceBookId == Guid.Empty)
        {
            throw new ArgumentException("PriceBookId cannot be empty.", nameof(priceBookId));
        }

        if ((planId.HasValue && moduleId.HasValue) || (!planId.HasValue && !moduleId.HasValue))
        {
            throw new ArgumentException("Exactly one of PlanId or ModuleId must be set.");
        }

        if (!IsValidPrice(price))
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Price must be >= 0 with at most 2 decimal places.");
        }

        Id = Guid.CreateVersion7();
        PriceBookId = priceBookId;
        PlanId = planId;
        ModuleId = moduleId;
        BillingCycle = billingCycle;
        Price = decimal.Round(price, 2, MidpointRounding.AwayFromZero);
    }

    public Guid PriceBookId { get; private set; }
    public Guid? PlanId { get; private set; }
    public Guid? ModuleId { get; private set; }
    public BillingCycle BillingCycle { get; private set; }
    public decimal Price { get; private set; }

    public static PriceBookItem ForPlan(Guid priceBookId, Guid planId, BillingCycle billingCycle, decimal price) =>
        new(priceBookId, planId, null, billingCycle, price);

    public static PriceBookItem ForModule(Guid priceBookId, Guid moduleId, BillingCycle billingCycle, decimal price) =>
        new(priceBookId, null, moduleId, billingCycle, price);

    public void UpdatePrice(decimal price)
    {
        if (!IsValidPrice(price))
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Price must be >= 0.");
        }

        Price = decimal.Round(price, 2, MidpointRounding.AwayFromZero);
    }

    public static bool IsValidPrice(decimal price) => price >= 0m && decimal.Round(price, 2) == price;
}
