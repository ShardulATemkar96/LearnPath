using AutoMapper;
using LearnPath.API.DTOs.LearningPath;
using LearnPath.API.Entities;

namespace LearnPath.API.Mappings;

public class ModuleMappingProfile : Profile
{
    public ModuleMappingProfile()
    {
        // Create/Update DTO → Module (for saving)
        CreateMap<CreateModuleDto, Module>()
            .ForMember(dest => dest.Resources, opt => opt.Ignore())
            .ForMember(dest => dest.Objectives, opt => opt.Ignore())
            .ForMember(dest => dest.Tags, opt => opt.Ignore());

        CreateMap<UpdateModuleDto, Module>()
            .ForMember(dest => dest.Resources, opt => opt.Ignore())
            .ForMember(dest => dest.Objectives, opt => opt.Ignore())
            .ForMember(dest => dest.Tags, opt => opt.Ignore());

        // Nested DTOs → Entities
        CreateMap<CreateResourceDto, ModuleResource>();
        CreateMap<CreateObjectiveDto, ModuleObjective>();

        // Entity → Response DTO
        CreateMap<Module, ModuleResponseDto>()
            .ForMember(dest => dest.IsCompleted, opt => opt.Ignore())
            .ForMember(dest => dest.IsUnlocked, opt => opt.Ignore());

        CreateMap<ModuleResource, ResourceDto>();
        CreateMap<ModuleObjective, ObjectiveDto>();

        // Tag conversion: string ↔ ModuleTag
        CreateMap<string, ModuleTag>()
            .ForMember(dest => dest.TagName, opt => opt.MapFrom(src => src))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ModuleId, opt => opt.Ignore())
            .ForMember(dest => dest.Module, opt => opt.Ignore());

        CreateMap<ModuleTag, string>()
            .ConvertUsing(src => src.TagName);
    }
}