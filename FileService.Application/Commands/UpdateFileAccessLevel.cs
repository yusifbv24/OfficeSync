using FileService.Application.Interfaces;
using FileService.Domain.Common;
using FileService.Domain.Enums;
using FluentValidation;
using MediatR;

namespace FileService.Application.Commands
{
    /// <summary>
    /// Command to change a file's access level.
    /// This allows the file owner or admins to change who can access the file
    /// by switching between Private, Public, ChannelMembers, and Restricted access levels.
    /// </summary>
    public record UpdateFileAccessLevelCommand(
        Guid FileId,
        FileAccessLevel NewAccessLevel,
        Guid UpdatedBy
    ):IRequest<Result<bool>>;


    public class UpdateFileAccessLevelCommandValidator : AbstractValidator<UpdateFileAccessLevelCommand>
    {
        public UpdateFileAccessLevelCommandValidator()
        {
            RuleFor(x => x.FileId)
                .NotEmpty()
                .WithMessage("File ID is required");

            RuleFor(x => x.UpdatedBy)
                .NotEmpty()
                .WithMessage("Updater ID is required");

            RuleFor(x => x.NewAccessLevel)
                .NotEmpty()
                .WithMessage("Invalid access level");
        }
    }


    public class UpdateFileAccessLevelCommandHandler : IRequestHandler<UpdateFileAccessLevelCommand, Result<bool>>
    {
        private readonly IFileRepository _fileRepository;
        private readonly IUserServiceClient _userServiceClient;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateFileAccessLevelCommandHandler(
            IFileRepository fileRepository,
            IUserServiceClient userServiceClient,
            IUnitOfWork unitOfWork)
        {
            _fileRepository = fileRepository;
            _userServiceClient = userServiceClient;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<bool>> Handle(
            UpdateFileAccessLevelCommand request,
            CancellationToken cancellationToken)
        {
            // Step 1. Retrieve file
            var file = await _fileRepository.GetByIdAsync(
                request.FileId,
                cancellationToken);

            if(file == null || file.IsDeleted)
            {
                return Result<bool>.Failure("File not found");
            }

            // Step 2. Verify permissions
            var userProfile = await _userServiceClient.GetUserProfileAsync(
                request.UpdatedBy, cancellationToken);

            if (userProfile == null)
            {
                return Result<bool>.Failure("User not found");
            }

            bool canUpdate=userProfile.Role=="Admin" || file.UploadedBy==request.UpdatedBy;
            if (!canUpdate)
            {
                return Result<bool>.Failure("You do not have permission to update the file's access level");
            }

            // Step 3. If changin to ChannelMembers, verify file has a channelId
            if(request.NewAccessLevel==FileAccessLevel.ChannelMembers && !file.ChannelId.HasValue)
            {
                return Result<bool>.Failure("Cannot set access level to ChannelMembers for files not associated with a channel");
            }

            // Step 4. Update access level
            file.UpdateAccessLevel(request.NewAccessLevel);

            // Step 5. Save changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<bool>.Success(true);
        }
    }
}