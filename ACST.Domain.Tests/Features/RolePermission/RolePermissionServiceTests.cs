using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ACST.Database.ApplicationDbContextModels.Models;
using ACST.Domain.DTOs.RolePermission;
using ACST.Domain.Features.RolePermission;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ACST.Domain.Tests.Features.RolePermission;

public class RolePermissionServiceTests
{
    private readonly AppDbContext _context;
    private readonly RolePermissionService _service;

    public RolePermissionServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _service = new RolePermissionService(_context);
    }

    [Fact]
    [Trait("Category", "positive")]
    public async Task GetRolesAsync_ReturnsOnlyNonDeletedRoles()
    {
        // Arrange
        _context.TblRoles.AddRange(
            new TblRole { RoleId = 1, RoleName = "Admin", DeleteFlag = false },
            new TblRole { RoleId = 2, RoleName = "Lecturer", DeleteFlag = false },
            new TblRole { RoleId = 3, RoleName = "ArchivedRole", DeleteFlag = true }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetRolesAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Count);
        Assert.Contains(result.Data, r => r.RoleName == "Admin");
        Assert.Contains(result.Data, r => r.RoleName == "Lecturer");
        Assert.DoesNotContain(result.Data, r => r.RoleName == "ArchivedRole");
    }

    [Fact]
    [Trait("Category", "positive")]
    public async Task GetPermissionsAsync_ReturnsActivePermissions()
    {
        // Arrange
        _context.TblPermissions.AddRange(
            new TblPermission { PermissionId = 1, PermissionName = "CanViewAttendance", DeleteFlag = false },
            new TblPermission { PermissionId = 2, PermissionName = "CanEditAttendance", DeleteFlag = false },
            new TblPermission { PermissionId = 3, PermissionName = "DeletedPerm", DeleteFlag = true }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPermissionsAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Count);
        Assert.Contains(result.Data, p => p.PermissionName == "CanViewAttendance");
        Assert.DoesNotContain(result.Data, p => p.PermissionName == "DeletedPerm");
    }

    [Fact]
    [Trait("Category", "positive")]
    [Trait("Category", "critical-path")]
    public async Task GetRolePermissionsAsync_ValidRole_ReturnsPermissions()
    {
        // Arrange
        var role = new TblRole { RoleId = 1, RoleName = "Lecturer", DeleteFlag = false };
        var perm1 = new TblPermission { PermissionId = 10, PermissionName = "MarkSession", DeleteFlag = false };
        var perm2 = new TblPermission { PermissionId = 11, PermissionName = "ViewSchedule", DeleteFlag = false };
        _context.TblRoles.Add(role);
        _context.TblPermissions.AddRange(perm1, perm2);

        _context.TblRolepermissions.AddRange(
            new TblRolepermission { RoleId = 1, PermissionId = 10, Permission = perm1, DeleteFlag = false },
            new TblRolepermission { RoleId = 1, PermissionId = 11, Permission = perm2, DeleteFlag = false }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetRolePermissionsAsync(1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.RoleId);
        Assert.Equal("Lecturer", result.Data.RoleName);
        Assert.Equal(2, result.Data.Permissions.Count);
    }

    [Fact]
    [Trait("Category", "negative")]
    public async Task GetRolePermissionsAsync_NonExistentOrDeletedRole_ReturnsFailure()
    {
        // Arrange
        var deletedRole = new TblRole { RoleId = 99, RoleName = "GhostRole", DeleteFlag = true };
        _context.TblRoles.Add(deletedRole);
        await _context.SaveChangesAsync();

        // Act
        var notFoundResult = await _service.GetRolePermissionsAsync(555);
        var deletedResult = await _service.GetRolePermissionsAsync(99);

        // Assert
        Assert.False(notFoundResult.IsSuccess);
        Assert.Equal("Role not found.", notFoundResult.Message);

        Assert.False(deletedResult.IsSuccess);
        Assert.Equal("Role not found.", deletedResult.Message);
    }

    [Fact]
    [Trait("Category", "positive")]
    [Trait("Category", "critical-path")]
    public async Task SetRolePermissionsAsync_ValidRole_ReplacesPermissionsSuccessfully()
    {
        // Arrange
        var role = new TblRole { RoleId = 1, RoleName = "Lecturer", DeleteFlag = false };
        var oldPerm = new TblPermission { PermissionId = 1, PermissionName = "OldPerm", DeleteFlag = false };
        var newPerm = new TblPermission { PermissionId = 2, PermissionName = "NewPerm", DeleteFlag = false };
        _context.TblRoles.Add(role);
        _context.TblPermissions.AddRange(oldPerm, newPerm);
        _context.TblRolepermissions.Add(new TblRolepermission { RoleId = 1, PermissionId = 1, DeleteFlag = false });
        await _context.SaveChangesAsync();

        var request = new SetRolePermissionsRequest
        {
            PermissionIds = new List<int> { 2 }
        };

        // Act
        var result = await _service.SetRolePermissionsAsync(1, request);

        // Assert
        Assert.True(result.IsSuccess);
        var updated = await _context.TblRolepermissions.Where(rp => rp.RoleId == 1).ToListAsync();
        Assert.Single(updated);
        Assert.Equal(2, updated[0].PermissionId);
    }

    [Fact]
    [Trait("Category", "negative")]
    public async Task SetRolePermissionsAsync_NonExistentRole_ReturnsFailure()
    {
        // Arrange
        var request = new SetRolePermissionsRequest
        {
            PermissionIds = new List<int> { 1, 2 }
        };

        // Act
        var result = await _service.SetRolePermissionsAsync(404, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Role not found.", result.Message);
    }

    [Fact]
    [Trait("Category", "positive")]
    [Trait("Category", "critical-path")]
    public async Task CreateRoleAsync_ValidName_CreatesRoleSuccessfully()
    {
        // Arrange
        var request = new CreateRoleRequest { RoleName = "Coordinator" };

        // Act
        var result = await _service.CreateRoleAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("Coordinator", result.Data.RoleName);

        var savedRole = await _context.TblRoles.FirstOrDefaultAsync(r => r.RoleName == "Coordinator");
        Assert.NotNull(savedRole);
        Assert.False(savedRole.DeleteFlag);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [Trait("Category", "negative")]
    [Trait("Category", "boundary")]
    public async Task CreateRoleAsync_EmptyOrWhitespaceName_ReturnsFailure(string roleName)
    {
        // Arrange
        var request = new CreateRoleRequest { RoleName = roleName };

        // Act
        var result = await _service.CreateRoleAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Role name cannot be empty.", result.Message);
    }

    [Fact]
    [Trait("Category", "negative")]
    [Trait("Category", "boundary")]
    public async Task CreateRoleAsync_DuplicateName_ReturnsFailure()
    {
        // Arrange
        _context.TblRoles.Add(new TblRole { RoleName = "Admin", DeleteFlag = false });
        await _context.SaveChangesAsync();

        var request = new CreateRoleRequest { RoleName = "Admin" };

        // Act
        var result = await _service.CreateRoleAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Role name already exists.", result.Message);
    }
}
