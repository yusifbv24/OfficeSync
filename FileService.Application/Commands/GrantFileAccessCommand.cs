using FileService.Application.Interfaces;
using FileService.Domain.Common;
using FileService.Domain.Enums;
using FluentValidation;
using MediatR;

namespace FileService.Application.Commands
{
    /// <summary>
    /// Command to grant explicit access to a restricted file for a specific user.
    /// This is only applicable when a file's access level is set to Restricted.
    /// When a file is restricted, only explicitly granted users (plus the owner and admins)
    /// can view and download it. This provides fine-grained control over sensitive files.
    /// </summary>
    public record GrantFileAccessCommand(
        Guid FileId,
        Guid UserId,
        Guid GrantedBy
    ):IRequest<Result<bool>>;


    public class GrantFileAccessCommandValidator : AbstractValidator<GrantFileAccessCommand>
    {
        public GrantFileAccessCommandValidator()
        {
            RuleFor(x => x.FileId)
                .NotEmpty()
                .WithMessage("File ID is required");

            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("User ID is required");

            RuleFor(x => x.GrantedBy)
                .NotEmpty()
                .WithMessage("Granter ID is required");

            // You cannot grant access to yourself - that doesn't make sense
            RuleFor(x => x)
                .Must(x => x.UserId != x.GrantedBy)
                .WithMessage("You cannot grant access to yourself");
        }
    }


    public class GrantFileAccessCommandHandler : IRequestHandler<GrantFileAccessCommand, Result<bool>>
    {
        private readonly IFileRepository _fileRepository;
        private readonly IUserServiceClient _userServiceClient;
        private readonly IUnitOfWork _unitOfWork;

        public GrantFileAccessCommandHandler(
            IFileRepository fileRepository,
            IUserServiceClient userServiceClient,
            IUnitOfWork unitOfWork)
        {
            _fileRepository = fileRepository;
            _userServiceClient = userServiceClient;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<bool>> Handle(
            GrantFileAccessCommand request,
            CancellationToken cancellationToken)
        {
            // Step 1. Retrieve the file
            var file = await _fileRepository.GetByIdAsync(request.FileId, cancellationToken);

            if(file == null)
            {
                return Result<bool>.Failure("File not found");
            }

            // Step 2. Verify file is actually restricted
            // You can only grant explicit access to restricted files
            if (file.AccessLevel != FileAccessLevel.Restricted)
            {
                return Result<bool>.Failure(
                    "Can only grant explicit access to restricted files." +
                    "Change the file's access level to Restricted first.");
            }

            // Step 3. Verify the granter has permission to grant access
            // Get granter's profile to check their role
            var granterProfile = await _userServiceClient
                .GetUserProfileAsync(request.GrantedBy, cancellationToken);

            if(granterProfile == null)
            {
                return Result<bool>.Failure("Granter not found");
            }

            // Only file owner or admins can grant access
            bool canGrant=granterProfile.Role=="Admin" || file.UploadedBy==request.GrantedBy;

            if (!canGrant)
            {
                return Result<bool>.Failure("You do not have permission to grant access this file");
            }

            // Step 4. Verify the target user exists
            // We dont want to grant access to non-existent users
            var targetUserProfile = await _userServiceClient
                .GetUserProfileAsync(request.UserId, cancellationToken);

            if(targetUserProfile == null)
            {
                return Result<bool>.Failure("Target user not found");
            }

            // Step 5. Grant access to the file
            try
            {
                file.GrantAccess(request.UserId, request.GrantedBy);
            }
            catch (InvalidOperationException ex)
            {
                // User might already have access
                return Result<bool>.Failure(ex.Message);
            }

            // Step 6. Save changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
    }
}