using AutoMapper;
using IdentityService.Application.Common;
using IdentityService.Application.DTOs.User;
using IdentityService.Application.Interfaces;
using MediatR;

namespace IdentityService.Application.Queries.Users
{
    public record GetUserByIdQuery(
        Guid userId): IRequest<Result<UserDto>>;

    public class GetUserByIdQueryHandler:IRequestHandler<GetUserByIdQuery, Result<UserDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public GetUserByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork=unitOfWork;
            _mapper=mapper;
        }

        public async Task<Result<UserDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(request.userId, cancellationToken);

            if (user == null)
            {
                return Result<UserDto>.Failure("User not found");
            }

            // Map entity to DTO
            var userDto= _mapper.Map<UserDto>(user);
            return Result<UserDto>.Success(userDto);
        }
    }
}