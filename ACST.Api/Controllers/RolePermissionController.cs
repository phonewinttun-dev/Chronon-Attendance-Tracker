using ACST.Domain.DTOs.RolePermission;
using ACST.Domain.Features.RolePermission;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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

        private bool IsAdmin()
        {
            return User.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
        }

        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles()
        {
            if (!IsAdmin()) return Forbid();

            var result = await _rolePermissionService.GetRolesAsync();
            return Ok(result);
        }

        [HttpGet("permissions")]
        public async Task<IActionResult> GetPermissions()
        {
            if (!IsAdmin()) return Forbid();

            var result = await _rolePermissionService.GetPermissionsAsync();
            return Ok(result);
        }

        [HttpGet("roles/{roleId}/permissions")]
        public async Task<IActionResult> GetRolePermissions(int roleId)
        {
            if (!IsAdmin()) return Forbid();

            var result = await _rolePermissionService.GetRolePermissionsAsync(roleId);
            if (result.IsFailure) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("roles/{roleId}/permissions")]
        public async Task<IActionResult> SetRolePermissions(int roleId, [FromBody] SetRolePermissionsRequest request)
        {
            if (!IsAdmin()) return Forbid();

            var result = await _rolePermissionService.SetRolePermissionsAsync(roleId, request);
            if (result.IsFailure) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("roles")]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
        {
            if (!IsAdmin()) return Forbid();

            var result = await _rolePermissionService.CreateRoleAsync(request);
            if (result.IsFailure) return BadRequest(result);
            return Ok(result);
        }
    }
}
}
