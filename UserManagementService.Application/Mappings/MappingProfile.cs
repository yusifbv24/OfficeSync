using AutoMapper;
using UserManagementService.Application.DTOs.Users;
using UserManagementService.Domain.Entities;

namespace UserManagementService.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<UserProfile, UserProfileDto>();
        }
    }
}