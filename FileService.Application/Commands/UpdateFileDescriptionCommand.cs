using FileService.Application.Interfaces;
using FileService.Domain.Common;
using FluentValidation;
using MediatR;

namespace FileService.Application.Commands
{
    /// <summary>
    /// Command to update a file's description or metadata.
    /// This allows users to add context or notes about their uploaded files.
    /// </summary>
    public record UpdateFileDescriptionCommand(
        Guid FileId,
        string? Description,
        Guid UpdatedBy
    ):IRequest<Result<bool>>;


    public class UpdateFileDescriptionValidator : AbstractValidator<UpdateFileDescriptionCommand>
    {
        public UpdateFileDescriptionValidator()
        {
            RuleFor(x => x.FileId)
                .NotEmpty().WithMessage("File ID is required");

            RuleFor(x => x.UpdatedBy)
                .NotEmpty().WithMessage("Updater ID is required");

            RuleFor(x => x.Description)
                .MaximumLength(500)
                .When(x => !string.IsNullOrEmpty(x.Description))
                .WithMessage("Description cannot exceed 500 characters");
        }
    }



    public class UpdateFileDescriptionCommandHandler : IRequestHandler<UpdateFileDescriptionCommand, Result<bool>>
    {
        private readonly IFileRepository _fileRepository;
        private readonly IUserServiceClient _userServiceClient;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateFileDescriptionCommandHandler(
            IFileRepository fileRepository,
            IUserServiceClient userServiceClient,
            IUnitOfWork unitOfWork)
        {
            _fileRepository = fileRepository;
            _userServiceClient = userServiceClient;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<bool>> Handle(
            UpdateFileDescriptionCommand request,
            CancellationToken cancellationToken)
        {
            // Retrieve file 
            var file = await _fileRepository.GetByIdAsync(request.FileId, cancellationToken);

            if(file == null)
            {
                return Result<bool>.Failure("File not found");
            }

            // Check permission
            var userProfile = await _userServiceClient.GetUserProfileAsync(request.UpdatedBy, cancellationToken);

            if (userProfile == null)
            {
                return Result<bool>.Failure("User not found");
            }

            bool canUpdate=userProfile.Role=="Admin"||file.UploadedBy==request.UpdatedBy;

            if (!canUpdate)
            {
                return Result<bool>.Failure("You do not have permission to update this file");
            }

            file.UpdateDescription(request.Description);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<bool>.Success(true);
        }
    }
}