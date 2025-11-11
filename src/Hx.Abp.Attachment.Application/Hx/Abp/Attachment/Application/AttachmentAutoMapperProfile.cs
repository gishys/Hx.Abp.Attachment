using AutoMapper;
using Hx.Abp.Attachment.Application.Contracts;
using Hx.Abp.Attachment.Domain;

namespace Hx.Abp.Attachment.Application
{
    public class AttachmentAutoMapperProfile : Profile
    {
        public AttachmentAutoMapperProfile()
        {
            // AttachCatalogue 映射
            CreateMap<AttachCatalogue, AttachCatalogueDto>(MemberList.Destination)
                .ForMember(dest => dest.Reference, opt => opt.MapFrom(src => src.Reference))
                .ForMember(dest => dest.CatalogueName, opt => opt.MapFrom(src => src.CatalogueName))
                .ForMember(dest => dest.IsRequired, opt => opt.MapFrom(src => src.IsRequired))
                .ForMember(dest => dest.Permissions, opt => opt.MapFrom(src => src.Permissions))
                .ForMember(dest => dest.MetaFields, opt => opt.MapFrom(src => src.MetaFields));
            CreateMap<AttachCatalogueCreateDto, AttachCatalogue>(MemberList.Source);
            CreateMap<AttachCatalogueUpdateDto, AttachCatalogue>(MemberList.Source);
            CreateMap<AttachCatalogue, AttachCatalogueTreeDto>(MemberList.Destination)
                .ForMember(dest => dest.Reference, opt => opt.MapFrom(src => src.Reference ?? string.Empty))
                .ForMember(dest => dest.CatalogueName, opt => opt.MapFrom(src => src.CatalogueName ?? string.Empty))
                .ForMember(dest => dest.IsRequired, opt => opt.MapFrom(src => src.IsRequired))
                .ForMember(dest => dest.Permissions, opt => opt.MapFrom(src => src.Permissions ?? new List<AttachCatalogueTemplatePermission>()))
                .ForMember(dest => dest.MetaFields, opt => opt.MapFrom(src => src.MetaFields ?? new List<MetaField>()))
                .ForMember(dest => dest.Children, opt => opt.Ignore()) // 忽略Children，在ConvertToTreeDtoAsync中手动处理
                .ForMember(dest => dest.AttachFiles, opt => opt.Ignore()); // 忽略AttachFiles，在ConvertToTreeDtoAsync中手动处理

            // AttachFile 映射
            CreateMap<AttachFile, AttachFileDto>(MemberList.Destination)
                .ForMember(dest => dest.FileAlias, opt => opt.MapFrom(src => src.FileAlias))
                .ForMember(dest => dest.FilePath, opt => opt.MapFrom(src => src.FilePath))
                .ForMember(dest => dest.FileName, opt => opt.MapFrom(src => src.FileName))
                .ForMember(dest => dest.FileType, opt => opt.MapFrom(src => src.FileType));
            CreateMap<AttachFileCreateDto, AttachFile>(MemberList.Source)
                .ForSourceMember(src => src.DynamicFacetCatalogueName, opt => opt.DoNotValidate()); // DynamicFacetCatalogueName是临时字段，不映射到实体
            CreateMap<AttachFileUpdateDto, AttachFile>(MemberList.Source);
            
            // AttachCatalogueTemplate 映射
            CreateMap<AttachCatalogueTemplate, AttachCatalogueTemplateDto>(MemberList.Destination)
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.TemplateName))
                .ForMember(dest => dest.Permissions, opt => opt.MapFrom(src => src.Permissions))
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id));
            CreateMap<AttachCatalogueTemplate, AttachCatalogueTemplateTreeDto>(MemberList.Destination)
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.TemplateName))
                .ForMember(dest => dest.Permissions, opt => opt.MapFrom(src => src.Permissions))
                .ForMember(dest => dest.Children, opt => opt.MapFrom(src => src.Children));
            CreateMap<CreateUpdateAttachCatalogueTemplateDto, AttachCatalogueTemplate>(MemberList.Source)
                .ForMember(dest => dest.TemplateName, opt => opt.MapFrom(src => src.Name));
            
            // 权限相关映射
            CreateMap<AttachCatalogueTemplatePermission, AttachCatalogueTemplatePermissionDto>(MemberList.Destination);
            CreateMap<AttachCatalogueTemplatePermissionDto, AttachCatalogueTemplatePermission>(MemberList.Source);
            
            // 元数据字段映射
            CreateMap<MetaField, MetaFieldDto>(MemberList.Destination);
            CreateMap<CreateUpdateMetaFieldDto, MetaField>(MemberList.Source);
        }
    }
}
