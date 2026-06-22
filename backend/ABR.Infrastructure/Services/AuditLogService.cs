using System.Text.Json;
using ABR.Application.Interfaces;
using ABR.Domain.Entities;
using ABR.Infrastructure.Persistence;

namespace ABR.Infrastructure.Services;

public sealed class AuditLogService : IAuditLogService
{
    private readonly AbrDbContext _context;

    public AuditLogService(AbrDbContext context) => _context = context;

    public async Task LogAsync(Guid? userId, string action, string tableName, Guid recordId, string? oldValues = null, string? newValues = null, CancellationToken cancellationToken = default)
    {
        _context.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            TableName = tableName,
            RecordId = recordId,
            OldValues = ToJsonb(oldValues),
            NewValues = ToJsonb(newValues)
        });
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string? ToJsonb(string? value)
    {
        if (string.IsNullOrEmpty(value)) return value;

        var trimmed = value.TrimStart();
        if (trimmed.StartsWith('{') || trimmed.StartsWith('[')) return value;

        return JsonSerializer.Serialize(value);
    }
}
