using AutoMapper;
using FileService.Application.DTOs;
using File = FileService.Domain.Entities.File;
using FileAccess = FileService.Domain.Entities.FileAccess;

namespace FileService.Application.Mappings
{
    public class MappingProfile: Profile
    {
        public MappingProfile()
        {
            CreateMap<File, FileDto>()
                .ForMember(dest => dest.AccessLevel,
                    opt => opt.MapFrom(src => src.AccessLevel.ToString()))
                .ForMember(dest => dest.HasThumbnail,
                    opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.ThumbnailPath)))
                .ForMember(dest => dest.UploaderDisplayName,
                    opt => opt.Ignore());

            CreateMap<FileAccess, FileAccessDto>()
                .ForMember(dest => dest.UserDisplayName,
                    opt => opt.Ignore())
                .ForMember(dest => dest.GrantedByDisplayName,
                    opt => opt.Ignore());
        }
    }
}