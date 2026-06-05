using AutoMapper;
using LearnPath.API.DTOs.Auth;
using LearnPath.API.Entities;

namespace LearnPath.API.Mappings;

public class AuthMappingProfile : Profile
{
    public AuthMappingProfile()
    {
        CreateMap<RegisterRequestDto, User>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email));
    }
}