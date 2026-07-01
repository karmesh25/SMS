namespace ABR.Application.DTOs.Users;

public class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public Guid RoleId { get; set; }
    public List<Guid> SiteIds { get; set; } = new();
}

public class UpdateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public Guid RoleId { get; set; }
    public List<Guid> SiteIds { get; set; } = new();
}
