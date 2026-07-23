using ACST.Domain.DTOs.RolePermission;
using ACST.Shared;

namespace ACST.Domain.Features.RolePermission
{
    public interface IRolePermissionService
    {
        Task<Result<List<RoleResponse>>> GetRolesAsync();
        Task<Result<List<PermissionResponse>>> GetPermissionsAsync();
        Task<Result<RolePermissionsResponse>> GetRolePermissionsAsync(int roleId);
        Task<Result> SetRolePermissionsAsync(int roleId, SetRolePermissionsRequest request);
        Task<Result<RoleResponse>> CreateRoleAsync(CreateRoleRequest request);
    }
}
