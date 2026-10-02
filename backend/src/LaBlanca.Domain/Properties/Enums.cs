namespace LaBlanca.Domain.Properties;

public enum PropertyOperation
{
    Sale,
    Rent,
}

public enum PropertyType
{
    House,
    Apartment,
    Land,
    Commercial,
    Other,
}

public enum PropertyStatus
{
    Draft,
    Available,
    Reserved,
    Sold,
    Rented,
    Archived,
}

public enum Currency
{
    USD,
    PYG,
    BRL,
}

public static class PropertyStatuses
{
    /// <summary>Status exibidos no site público (além de publicado).</summary>
    public static readonly PropertyStatus[] Listed = [PropertyStatus.Available, PropertyStatus.Reserved];

    public static bool IsListed(PropertyStatus status) => Listed.Contains(status);
}
