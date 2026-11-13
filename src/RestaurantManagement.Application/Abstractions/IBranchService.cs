using RestaurantManagement.Application.Common;
using RestaurantManagement.Application.Models.Branches;

namespace RestaurantManagement.Application.Abstractions;

public interface IBranchService
{
    Task<IReadOnlyList<BranchDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BranchOptionDto>> GetOptionsAsync(CancellationToken cancellationToken = default);

    Task<BranchInput?> GetInputAsync(int id, CancellationToken cancellationToken = default);

    Task<OperationResult<int>> CreateAsync(BranchInput input, CancellationToken cancellationToken = default);

    Task<OperationResult<bool>> UpdateAsync(
        int id,
        BranchInput input,
        CancellationToken cancellationToken = default);

    Task<OperationResult<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
