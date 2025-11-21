using AutoMapper;
using VietStart.API.Entities.Domains;
using VietStart.API.Entities.DTO;

namespace VietStart.API.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // StartUpMedia mappings
            CreateMap<StartUpMedia, StartUpMediaDto>();
            CreateMap<CreateStartUpMediaDto, StartUpMedia>();
            CreateMap<UpdateStartUpMediaDto, StartUpMedia>();

            // Category mappings
            CreateMap<Category, CategoryDto>();
            CreateMap<CreateCategoryDto, Category>();
            CreateMap<UpdateCategoryDto, Category>();

            // StartUp mappings
            CreateMap<StartUp, StartUpDto>();
            CreateMap<CreateStartUpDto, StartUp>();
            CreateMap<UpdateStartUpDto, StartUp>();

            // StartUp Detail mapping with all related data
            CreateMap<StartUp, StartUpDetailDto>()
                .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.AppUser))
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category))
                .ForMember(dest => dest.Medias, opt => opt.MapFrom(src => src.StartUpMedias))
                .ForMember(dest => dest.Comments, opt => opt.MapFrom(src => src.Comments))
                .ForMember(dest => dest.Shares, opt => opt.MapFrom(src => src.Shares))
                .ForMember(dest => dest.Reacts, opt => opt.MapFrom(src => src.Reacts));

            // Comment mappings
            CreateMap<Comment, CommentDto>();
            CreateMap<CreateCommentDto, Comment>();
            CreateMap<UpdateCommentDto, Comment>();

            // React mappings
            CreateMap<React, ReactDto>();
            CreateMap<CreateReactDto, React>();
            CreateMap<UpdateReactDto, React>();

            // Share mappings
            CreateMap<Share, ShareDto>();
            CreateMap<CreateShareDto, Share>();
            CreateMap<UpdateShareDto, Share>();

            // AppUser mappings
            CreateMap<AppUser, AppUserDto>();

            // TeamStartUp mappings
            CreateMap<TeamStartUp, TeamStartUpDto>()
                .ForMember(dest => dest.StartUpIdea, opt => opt.MapFrom(src => src.StartUp.Idea))
                .ForMember(dest => dest.UserFullName, opt => opt.MapFrom(src => src.User.FullName))
                .ForMember(dest => dest.UserAvatar, opt => opt.MapFrom(src => src.User.Avatar))
                .ForMember(dest => dest.PositionName, opt => opt.MapFrom(src => src.Position.Name));

            CreateMap<TeamStartUp, TeamStartUpDetailDto>();
            CreateMap<CreateTeamStartUpDto, TeamStartUp>();
            CreateMap<UpdateTeamStartUpDto, TeamStartUp>();

            // Position mappings
            CreateMap<Position, PositionDto>();
            CreateMap<CreatePositionDto, Position>();
            CreateMap<UpdatePositionDto, Position>();
        }
    }
}
