using AutoMapper;
using Template.Web.Application.Dtos.Base;

namespace Template.Web.Application.Utilities.MapperProfiles;

/// <summary>
///     Registers simple entity DTO mappings from their generic DTO base types.
/// </summary>
public sealed class DtoConventionProfile : Profile
{
    public DtoConventionProfile()
    {
        var dtoAssembly = typeof(ReadDto).Assembly;

        foreach (
            var dtoType in dtoAssembly
                .GetTypes()
                .Where(type => type is { IsAbstract: false, IsClass: true })
                .Where(type => !typeof(IManualDtoMapping).IsAssignableFrom(type))
        )
        {
            var dtoBaseType = GetEntityDtoBaseType(dtoType);
            if (dtoBaseType is null)
            {
                continue;
            }

            var genericDefinition = dtoBaseType.GetGenericTypeDefinition();
            var entityType = dtoBaseType.GetGenericArguments()[0];

            if (
                genericDefinition == typeof(ReadDto<>)
                || genericDefinition == typeof(ReadDto<,>)
            )
            {
                CreateMap(entityType, dtoType);
            }
            else if (
                genericDefinition == typeof(CreateDto<>)
                || genericDefinition == typeof(UpdateDto<>)
            )
            {
                CreateMap(dtoType, entityType);
            }
        }
    }

    private static Type? GetEntityDtoBaseType(Type dtoType)
    {
        for (var type = dtoType.BaseType; type is not null; type = type.BaseType)
        {
            if (!type.IsGenericType)
            {
                continue;
            }

            var genericDefinition = type.GetGenericTypeDefinition();
            if (
                genericDefinition == typeof(ReadDto<>)
                || genericDefinition == typeof(ReadDto<,>)
                || genericDefinition == typeof(CreateDto<>)
                || genericDefinition == typeof(UpdateDto<>)
            )
            {
                return type;
            }
        }

        return null;
    }
}
