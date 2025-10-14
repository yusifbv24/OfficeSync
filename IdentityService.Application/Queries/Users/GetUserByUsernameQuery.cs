using AutoMapper;
using IdentityService.Application.Common;
using IdentityService.Application.DTOs.User;
using IdentityService.Application.Interfaces;
using MediatR;

namespace IdentityService.Application.Queries.Users
{
    public record GetUserByUsernameQuery(
        string Username): IRequest<Result<UserDto>>;

    public class GetUserByUsernameQueryHandler : IRequestHandler<GetUserByUsernameQuery, Result<UserDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public GetUserByUsernameQueryHandler(IUnitOfWork unitOfWork,IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Result<UserDto>> Handle(GetUserByUsernameQuery request,CancellationToken cancellationToken)
        {
            var user = await _unitOfWork.Users.GetFirstOrDefaultAsync(
                u => u.Username == request.Username,cancellationToken);

            if (user == null)
            {
                return Result<UserDto>.Failure("User not found");
            }

            // Map entity to DTO
            var userDto = _mapper.Map<UserDto>(user);

            return Result<UserDto>.Success(userDto);
        }
    }
}