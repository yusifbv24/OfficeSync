using FileService.Domain.Enums;

namespace FileService.Application.DTOs
{
    public record UpdateAccessLevelRequest(FileAccessLevel AccessLevel);
}