namespace ACST.Domain.DTOs.RolePermission
{
    public class RoleResponse
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
    }

    public class PermissionResponse
    {
        public int PermissionId { get; set; }
        public string PermissionName { get; set; } = string.Empty;
    }

    public class RolePermissionsResponse
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public List<PermissionResponse> Permissions { get; set; } = new();
    }

    public class SetRolePermissionsRequest
    {
        public List<int> PermissionIds { get; set; } = new();
    }

    public class CreateRoleRequest
    {
        public string RoleName { get; set; } = null!;
    }
}
