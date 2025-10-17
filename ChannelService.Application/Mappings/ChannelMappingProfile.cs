using AutoMapper;
using ChannelService.Application.Channels;
using ChannelService.Application.Members;
using ChannelService.Domain.Entities;

namespace ChannelService.Application.Mappings
{
    public class ChannelMappingProfile:Profile
    {
        public ChannelMappingProfile()
        {
            CreateMap<Channel, ChannelDto>()
                .ForMember(dest => dest.MemberCount,
                    opt => opt.MapFrom(src => src.Members.Count(m => !m.IsRemoved)));

            CreateMap<Channel,ChannelListDto>()
                .ForMember(dest=>dest.MemberCount,
                    opt=>opt.MapFrom(src=>src.Members.Count(m=>!m.IsRemoved)));

            CreateMap<ChannelMember, ChannelMemberDto>();
        }
    }
}