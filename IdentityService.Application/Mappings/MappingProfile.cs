using AutoMapper;
using IdentityService.Application.DTOs.User;
using IdentityService.Domain.Entities;

namespace IdentityService.Application.Mappings
{
    public class MappingProfile:Profile
    {
        public MappingProfile()
        {
            CreateMap<User, UserDto>();
        }
    }
}