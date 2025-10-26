using FileService.Application.Interfaces;
using FileService.Domain.Common;
using FluentValidation;
using MediatR;

namespace FileService.Application.Commands
{
    /// <summary>
    /// Command to soft-delete a file from the system.
    /// This implements a soft delete pattern where the file is marked as deleted
    /// but the actual physical file and database record remain for potential recovery.
    /// This is important for audit trails and allowing users to recover accidentally deleted files.
    /// </summary>
    public record DeleteFileCommand(
        Guid FileId,
        Guid DeletedBy
    ):IRequest<Result<bool>>;



    public class DeleteFileCommandValidator : AbstractValidator<DeleteFileCommand>
    {
        public DeleteFileCommandValidator()
        {
            RuleFor(x => x.FileId)
                .NotEmpty()
                .WithMessage("File ID is required");

            RuleFor(x => x.DeletedBy)
                .NotEmpty()
                .WithMessage("Deleter ID is required");
        }
    }



    public class DeleteFileCommandHandler : IRequestHandler<DeleteFileCommand, Result<bool>>
    {
        private readonly IFileRepository _fileRepository;
        private readonly IUserServiceClient _userServiceClient;
        private readonly IChannelServiceClient _channelServiceClient;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteFileCommandHandler(
            IFileRepository fileRepository,
            IUserServiceClient userServiceClient,
            IChannelServiceClient channelServiceClient,
            IUnitOfWork unitOfWork)
        {
            _fileRepository = fileRepository;
            _userServiceClient = userServiceClient;
            _channelServiceClient = channelServiceClient;
            _unitOfWork = unitOfWork;
        }


        public async Task<Result<bool>> Handle(
            DeleteFileCommand request,
            CancellationToken cancellationToken)
        {
            // Step 1: Retrieve the file from the database
            var file = await _fileRepository.GetByIdAsync(request.FileId, cancellationToken);
            if (file == null)
            {
                return Result<bool>.Failure("File not found");
            }

            // Step 2. Check if file is already deleted
            if (file.IsDeleted)
            {
                return Result<bool>.Failure("File is already deleted");
            }

            // Step 3. Verify user has permission to delete this file
            // Get user information to check their role
            var userProfile = await _userServiceClient
                .GetUserProfileAsync(request.DeletedBy, cancellationToken);

            if (userProfile == null)
            {
                return Result<bool>.Failure("User not found");
            }

            bool canDelete = false;

            // Admins can delete any file
            if (userProfile.Role == "Admin")
            {
                canDelete = true;
            }

            // File owner can delete their own files
            else if (file.UploadedBy == request.DeletedBy)
            {
                canDelete = true;
            }
            // For channel files, check if user is a channel operator
            else if (file.ChannelId.HasValue)
            {
                var hasChannelPermission = await _channelServiceClient
                    .UserCanManageChannelAsync(file.ChannelId.Value, request.DeletedBy, cancellationToken);
                if (hasChannelPermission)
                {
                    canDelete = true;
                }
            }
            if (!canDelete)
            {
                return Result<bool>.Failure("You do not have permission to delete this file");
            }

            // Step 4. Soft delete the file
            file.Delete(request.DeletedBy);

            // Step 5. Save changes to database
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // The FileDeletedEvent will be automatically published after SaveChanges
            // Other services can subscribe to clean up references
            return Result<bool>.Success(true);
        }
    }
}