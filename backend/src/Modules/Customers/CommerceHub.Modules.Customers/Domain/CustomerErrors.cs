using CommerceHub.BuildingBlocks.Results;

namespace CommerceHub.Modules.Customers.Domain;

public static class CustomerErrors
{
    public static readonly Error NotFound =
        Error.NotFound("customers.not_found", "Customer profile not found.");

    public static readonly Error AddressNotFound =
        Error.NotFound("customers.address.not_found", "Address not found.");
}
