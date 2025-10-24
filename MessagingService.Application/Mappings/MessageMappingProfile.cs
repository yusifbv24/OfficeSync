using AutoMapper;
using MessagingService.Application.Attachments;
using MessagingService.Application.Messages;
using MessagingService.Application.Reactions;
using MessagingService.Domain.Entities;

namespace MessagingService.Application.Mappings
{
    public class MessageMappingProfile: Profile
    {
        public MessageMappingProfile()
        {
            CreateMap<Message, MessageDto>()
                .ForMember(dest => dest.SenderName, opt => opt.Ignore()) // Set manually in handlers
                .ForMember(dest => dest.Reactions, opt => opt.MapFrom(src => MapReactionsGrouped(src.Reactions)))
                .ForMember(dest => dest.Attachments, opt => opt.MapFrom(src => src.Attachments));

            CreateMap<Message, MessageListDto>()
                .ForMember(dest => dest.SenderName, opt => opt.Ignore())
                .ForMember(dest => dest.ReactionCount,
                    opt => opt.MapFrom(src => src.Reactions.Count(r => !r.IsRemoved)))
                .ForMember(dest => dest.AttachmentCount,
                    opt => opt.MapFrom(src => src.Attachments.Count));

            CreateMap<MessageAttachment, MessageAttachmentDto>();
        }

        /// <summary>
        /// Groups reactions by emoji and creates aggregated DTOs.
        /// For example, if 3 users reacted with "👍", creates one DTO with count=3.
        /// </summary>
        private List<MessageReactionDto> MapReactionsGrouped(IReadOnlyCollection<MessageReaction> reactions)
        {
            return reactions
                .Where(r => !r.IsRemoved)
                .GroupBy(r => r.Emoji)
                .Select(g => new MessageReactionDto
                {
                    Emoji = g.Key,
                    Count = g.Count(),
                    Users = g.Select(r => new ReactionUserDto
                    {
                        UserId = r.UserId,
                        CreatedAt = r.CreatedAt
                    })
                    .ToList()
                })
                .ToList();
        }
    }
}