namespace FileService.Application.DTOs
{
    public record FileStorageOptions
    {
        public string BasePath { get; set; }=string.Empty;
    }
}