using ABR.Domain.Common;

namespace ABR.Domain.Entities;

public class AppRole : SoftDeleteEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }

    public ICollection<RolePermission> Permissions { get; set; } = new List<RolePermission>();
    public ICollection<User> Users { get; set; } = new List<User>();
}

public class RolePermission : BaseEntity
{
    public Guid RoleId { get; set; }
    public string ModuleKey { get; set; } = string.Empty;
    public bool CanView { get; set; }
    public bool CanManage { get; set; }

    public AppRole Role { get; set; } = null!;
}
