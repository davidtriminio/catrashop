using Catrashop.BuildingBlocks.Results;
using Catrashop.Modules.Shipping.Application.Abstractions;
using Catrashop.Modules.Shipping.Application.Contracts;
using Catrashop.Modules.Shipping.Domain;

namespace Catrashop.Modules.Shipping.Application.Services;

/// <summary>Admin management and public listing of shipping methods.</summary>
public sealed class ShippingMethodService
{
    private readonly IShippingMethodRepository _methods;
    private readonly IShippingUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;

    public ShippingMethodService(IShippingMethodRepository methods, IShippingUnitOfWork unitOfWork, TimeProvider clock)
    {
        _methods = methods;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<ShippingMethodResponse>> CreateAsync(CreateShippingMethodRequest request, CancellationToken ct = default)
    {
        var code = request.Code.Trim().ToLowerInvariant();
        if (await _methods.CodeExistsAsync(code, ct))
            return ShippingErrors.MethodCodeInUse(code);

        var method = ShippingMethod.Create(code, request.Name, request.Cost, request.Currency, request.EstimatedDays, _clock.GetUtcNow());
        _methods.Add(method);
        await _unitOfWork.SaveChangesAsync(ct);
        return ShippingMappers.ToResponse(method);
    }

    public async Task<Result<ShippingMethodResponse>> UpdateAsync(Guid id, UpdateShippingMethodRequest request, CancellationToken ct = default)
    {
        var method = await _methods.GetByIdAsync(id, ct);
        if (method is null)
            return ShippingErrors.MethodNotFoundById;

        method.Update(request.Name, request.Cost, request.Currency, request.EstimatedDays, _clock.GetUtcNow());
        await _unitOfWork.SaveChangesAsync(ct);
        return ShippingMappers.ToResponse(method);
    }

    public async Task<Result> SetActiveAsync(Guid id, bool active, CancellationToken ct = default)
    {
        var method = await _methods.GetByIdAsync(id, ct);
        if (method is null)
            return ShippingErrors.MethodNotFoundById;

        if (active) method.Activate(_clock.GetUtcNow());
        else method.Deactivate(_clock.GetUtcNow());
        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<IReadOnlyList<ShippingMethodResponse>> ListAsync(bool activeOnly, CancellationToken ct = default) =>
        (await _methods.ListAsync(activeOnly, ct)).Select(ShippingMappers.ToResponse).ToArray();
}
