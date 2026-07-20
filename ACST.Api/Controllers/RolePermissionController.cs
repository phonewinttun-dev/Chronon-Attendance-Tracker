using ACST.Api.Middleware;
using ACST.Domain.DTOs.RolePermission;
using ACST.Domain.Features.RolePermission;
using ACST.Shared;
using Microsoft.AspNetCore.Mvc;

namespace ACST.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolePermissionController : ControllerBase
    {
        private readonly IRolePermissionService _rolePermissionService;

        public RolePermissionController(IRolePermissionService rolePermissionService)
        {
            _rolePermissionService = rolePermissionService;
        }

        [HttpGet("roles")]
        [HasPermission(Permissions.Roles.View)]
        public async Task<IActionResult> GetRoles()
        {
            var result = await _rolePermissionService.GetRolesAsync();
            return Ok(result);
        }

        [HttpGet("permissions")]
        [HasPermission(Permissions.Roles.View)]
        public async Task<IActionResult> GetPermissions()
        {
            var result = await _rolePermissionService.GetPermissionsAsync();
            return Ok(result);
        }

        [HttpGet("roles/{roleId}/permissions")]
        [HasPermission(Permissions.Roles.View)]
        public async Task<IActionResult> GetRolePermissions(int roleId)
        {
            var result = await _rolePermissionService.GetRolePermissionsAsync(roleId);
            if (result.IsFailure) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("roles/{roleId}/permissions")]
        [HasPermission(Permissions.Roles.Manage)]
        public async Task<IActionResult> SetRolePermissions(int roleId, [FromBody] SetRolePermissionsRequest request)
        {
            var result = await _rolePermissionService.SetRolePermissionsAsync(roleId, request);
            if (result.IsFailure) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("roles")]
        [HasPermission(Permissions.Roles.Manage)]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
        {
            var result = await _rolePermissionService.CreateRoleAsync(request);
            if (result.IsFailure) return BadRequest(result);
            return Ok(result);
        }
    }
}
