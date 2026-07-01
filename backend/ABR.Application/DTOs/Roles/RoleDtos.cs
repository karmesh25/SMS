namespace ABR.Application.DTOs.Roles;

public class RolePermissionDto
{
    public string ModuleKey { get; set; } = string.Empty;
    public bool CanView { get; set; }
    public bool CanManage { get; set; }
}

public class RoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public int UserCount { get; set; }
    public List<RolePermissionDto> Permissions { get; set; } = new();
}

public class CreateRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<RolePermissionDto> Permissions { get; set; } = new();
}

public class UpdateRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<RolePermissionDto> Permissions { get; set; } = new();
}
