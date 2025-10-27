namespace FileService.Application.DTOs
{
    public record ValidateFileAccessRequest(List<Guid> FileIds);
}
