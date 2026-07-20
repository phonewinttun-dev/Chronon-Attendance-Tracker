using ACST.Database.ApplicationDbContextModels.Models;
using ACST.Domain.DTOs.RolePermission;
using ACST.Shared;
using Microsoft.EntityFrameworkCore;

namespace ACST.Domain.Features.RolePermission
{
    public class RolePermissionService : IRolePermissionService
    {
        private readonly AppDbContext _context;

        public RolePermissionService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Result<List<RoleResponse>>> GetRolesAsync()
        {
            var roles = await _context.TblRoles
                .Where(r => r.DeleteFlag != true)
                .Select(r => new RoleResponse
                {
                    RoleId = r.RoleId,
                    RoleName = r.RoleName
                })
                .ToListAsync();

            return Result<List<RoleResponse>>.Success(roles);
        }

        public async Task<Result<List<PermissionResponse>>> GetPermissionsAsync()
        {
            var permissions = await _context.TblPermissions
                .Where(p => p.DeleteFlag != true)
                .Select(p => new PermissionResponse
                {
                    PermissionId = p.PermissionId,
                    PermissionName = p.PermissionName
                })
                .ToListAsync();

            return Result<List<PermissionResponse>>.Success(permissions);
        }

        public async Task<Result<RolePermissionsResponse>> GetRolePermissionsAsync(int roleId)
        {
            var role = await _context.TblRoles.FirstOrDefaultAsync(r => r.RoleId == roleId && r.DeleteFlag != true);
            if (role == null)
            {
                return Result<RolePermissionsResponse>.Failure("Role not found.");
            }

            var permissions = await _context.TblRolepermissions
                .Where(rp => rp.RoleId == roleId && rp.DeleteFlag != true && rp.Permission != null)
                .Include(rp => rp.Permission)
                .Select(rp => new PermissionResponse
                {
                    PermissionId = rp.Permission!.PermissionId,
                    PermissionName = rp.Permission!.PermissionName
                })
                .ToListAsync();

            var response = new RolePermissionsResponse
            {
                RoleId = role.RoleId,
                RoleName = role.RoleName,
                Permissions = permissions
            };

            return Result<RolePermissionsResponse>.Success(response);
        }

        public async Task<Result> SetRolePermissionsAsync(int roleId, SetRolePermissionsRequest request)
        {
            var role = await _context.TblRoles.FirstOrDefaultAsync(r => r.RoleId == roleId && r.DeleteFlag != true);
            if (role == null)
            {
                return Result.Failure("Role not found.");
            }

            // Remove existing permissions
            var existing = await _context.TblRolepermissions
                .Where(rp => rp.RoleId == roleId)
                .ToListAsync();

            _context.TblRolepermissions.RemoveRange(existing);

            // Add new permissions
            foreach (var permissionId in request.PermissionIds)
            {
                if (await _context.TblPermissions.AnyAsync(p => p.PermissionId == permissionId && p.DeleteFlag != true))
                {
                    var rp = new TblRolepermission
                    {
                        RoleId = roleId,
                        PermissionId = permissionId,
                        DeleteFlag = false
                    };
                    _context.TblRolepermissions.Add(rp);
                }
            }

            await _context.SaveChangesAsync();
            return Result.Success($"Permissions updated for role {role.RoleName}.");
        }

        public async Task<Result<RoleResponse>> CreateRoleAsync(CreateRoleRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.RoleName))
                {
                    return Result<RoleResponse>.Failure("Role name cannot be empty.");
                }

                var exists = await _context.TblRoles
                    .AnyAsync(r => r.RoleName == request.RoleName && r.DeleteFlag != true);

                if (exists)
                {
                    return Result<RoleResponse>.Failure("Role name already exists.");
                }

                var role = new TblRole
                {
                    RoleName = request.RoleName,
                    DeleteFlag = false
                };

                _context.TblRoles.Add(role);
                await _context.SaveChangesAsync();

                var response = new RoleResponse
                {
                    RoleId = role.RoleId,
                    RoleName = role.RoleName ?? string.Empty
                };

                return Result<RoleResponse>.Success(response, "Role created successfully.");
            }
            catch (Exception ex)
            {
                return Result<RoleResponse>.Failure($"System Error: {ex.Message}");
            }
        }
    }
}
