namespace TrackManagement.Application.Common.Security;

public static class Roles
{
    public const string CatalogManager = "catalog-manager";
}

public static class Policies
{
    public const string ManageCatalog = nameof(ManageCatalog);
}
