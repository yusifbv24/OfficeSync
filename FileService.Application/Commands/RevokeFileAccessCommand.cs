using FileService.Application.Interfaces;
using FileService.Domain.Common;
using FluentValidation;
using MediatR;

namespace FileService.Application.Commands
{
    /// <summary>
    /// Command to revoke previously granted access to a restricted file.
    /// This removes a user's ability to view and download a file they previously had access to.
    /// The access record is kept for audit purposes but marked as revoked.
    /// </summary>
    public record RevokeFileAccessCommand(
        Guid FileId,
        Guid UserId,
        Guid RevokedBy
    ):IRequest<Result<bool>>;



    public class RevokeFileAccessCommandValidator : AbstractValidator<RevokeFileAccessCommand>
    {
        public RevokeFileAccessCommandValidator()
        {
            RuleFor(x => x.FileId)
                .NotEmpty()
                .WithMessage("File ID is required");

            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("User ID is required");

            RuleFor(x => x.RevokedBy)
                .NotEmpty()
                .WithMessage("Revoker ID is required");
        }
    }


    public class RevokeFileAccessCommandHandler : IRequestHandler<RevokeFileAccessCommand, Result<bool>>
    {
        private readonly IFileRepository _fileRepository;
        private readonly IUserServiceClient _userServiceClient;
        private readonly IUnitOfWork _unitOfWork;

        public RevokeFileAccessCommandHandler(
            IFileRepository fileRepository,
            IUserServiceClient userServiceClient,
            IUnitOfWork unitOfWork)
        {
            _fileRepository = fileRepository;
            _userServiceClient = userServiceClient;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<bool>> Handle(
            RevokeFileAccessCommand request,
            CancellationToken cancellationToken)
        {
            // Step 1. Retrieve the file with access records
            var file = await _fileRepository.GetByIdWithAccessesAsync(
                request.FileId, cancellationToken);

            if(file == null || file.IsDeleted)
            {
                return Result<bool>.Failure("File not found");
            }

            // Step 2. Verify the revoker has permission
            var revokerProfile=await _userServiceClient
                .GetUserProfileAsync(request.RevokedBy,cancellationToken);

            if (revokerProfile == null)
            {
                return Result<bool>.Failure("Revoker not found");
            }

            // Only file owner or admins can revoke access
            bool canRevoke = revokerProfile.Role == "Admin" || file.UploadedBy == request.RevokedBy;
            if (!canRevoke)
            {
                return Result<bool>.Failure("You do not have permission to revoke access to this file");
            }

            // Step 3. You cannot revoke access from the file owner
            if (request.RevokedBy == file.UploadedBy)
            {
                return Result<bool>.Failure("Cannot revoke access from the file owner");
            }

            // Step 4. Revoke access
            try
            {
                file.RevokeAccess(request.UserId, request.RevokedBy);
            }
            catch (InvalidOperationException ex)
            {
                // User might not have access to revoke
                return Result<bool>.Failure(ex.Message);
            }

            // Step 5. Save changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
    }
}