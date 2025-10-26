using FileService.Application.Interfaces;
using FileService.Domain.Common;
using FluentValidation;
using MediatR;

namespace FileService.Application.Commands
{
    /// <summary>
    /// Command to restore a previously soft-deleted file.
    /// This allows users to recover files they accidentally deleted.
    /// Only admins or the original uploader can restore files.
    /// </summary>
    public record RestoreFileCommand(
        Guid FileId,
        Guid RestoredBy
    ):IRequest<Result<bool>>;



    public class RestoreFileCommandValidator : AbstractValidator<RestoreFileCommand>
    {
        public RestoreFileCommandValidator()
        {
            RuleFor(x => x.FileId)
                .NotEmpty()
                .WithMessage("File ID is required");

            RuleFor(x => x.RestoredBy)
                .NotEmpty()
                .WithMessage("Restorer ID is required");
        }
    }


    public class RestoreFileCommandHandler : IRequestHandler<RestoreFileCommand, Result<bool>>
    {
        private readonly IFileRepository _fileRepository;
        private readonly IUserServiceClient _userServiceClient;
        private readonly IUnitOfWork _unitOfWork;

        public RestoreFileCommandHandler(
            IFileRepository fileRepository,
            IUserServiceClient userServiceClient,
            IUnitOfWork unitOfWork)
        {
            _fileRepository = fileRepository;
            _userServiceClient = userServiceClient;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<bool>> Handle(
            RestoreFileCommand request,
            CancellationToken cancellationToken)
        {
            // Step 1: Retrieve the file including deleted ones
            var file = await _fileRepository.GetByIdIncludingDeletedAsync(
                request.FileId,
                cancellationToken);

            if(file == null)
            {
                return Result<bool>.Failure("File not found");
            }

            // Step 2: Verify the file is actually deleted
            if (!file.IsDeleted)
            {
                return Result<bool>.Failure("File is not deleted");
            }


            // Step 3: Check permission - only admins or original uploader can restore
            var userProfile = await _userServiceClient
                .GetUserProfileAsync(request.RestoredBy, cancellationToken);

            if(userProfile == null)
            {
                return Result<bool>.Failure("User not found");
            }

            bool canRestore = userProfile.Role == "Admin" || file.UploadedBy == request.RestoredBy;

            if (!canRestore)
            {
                return Result<bool>.Failure("You do not have permission to restore this file");
            }

            // Step 4: Restore the file
            file.Restore();

            // Step 5: Save changes

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
    }
}