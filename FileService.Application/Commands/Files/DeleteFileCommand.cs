using FileService.Application.Common;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FileService.Application.Commands.Files
{
    public record DeleteFileCommand(
        Guid FileId,
        Guid DeletedBy
    ):IRequest<Result<bool>>;


    public class DeleteFileCommandValidator : AbstractValidator<DeleteFileCommand>
    {
        public DeleteFileCommandValidator()
        {
            RuleFor(x => x.FileId)
                .NotEmpty().WithMessage("File ID is required");

            RuleFor(x => x.DeletedBy)
                .NotEmpty().WithMessage("DeletedBy user ID is required");
        }
    }



    public class DeleteFileCommandHandler:IRequestHandler<DeleteFileCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteFileCommandHandler> _logger;

        public DeleteFileCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<DeleteFileCommandHandler> logger)
        {
            _unitOfWork= unitOfWork;
            _logger= logger;
        }

        public async Task<Result<bool>> Handle(
            DeleteFileCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                var fileMetadata = await _unitOfWork.FileMetadata.GetByIdAsync(
                    request.FileId,
                    cancellationToken);

                if(fileMetadata == null)
                {
                    return Result<bool>.Failure("File not found");
                }

                // Only the uploader can delete the file
                if (fileMetadata.UploadedBy != request.DeletedBy)
                {
                    return Result<bool>.Failure("Only the file uploader can delete the file");
                }

                // Perform soft delete using domain logic
                fileMetadata.Delete(request.DeletedBy);

                await _unitOfWork.FileMetadata.UpdateAsync(fileMetadata, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger?.LogInformation(
                    "File deleted: {FileId}, Name: {FileName}, DeletedBy: {UserId}",
                    fileMetadata.Id,
                    fileMetadata.OriginalFileName,
                    request.DeletedBy);

                return Result<bool>.Success(true, "File deleted succesfully");
            }
            catch (InvalidOperationException ex)
            {
                _logger?.LogWarning(ex, "Failed to delete file {FileId} : {Message}", request.FileId, ex.Message);
                return Result<bool>.Failure(ex.Message);
            }
            catch(Exception ex)
            {
                _logger?.LogError(ex, "Error deleting file {FileId}", request.FileId);
                return Result<bool>.Failure("An error occured while deleting the file", ex.Message);
            }
        }
    }
}