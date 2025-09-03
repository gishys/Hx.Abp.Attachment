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
                .ForMember(dest => dest.Permissions, opt => opt.MapFrom(src => src.Permissions));
            CreateMap<AttachCatalogueCreateDto, AttachCatalogue>(MemberList.Source);
            CreateMap<AttachCatalogueUpdateDto, AttachCatalogue>(MemberList.Source);
            
            // AttachFile 映射
            CreateMap<AttachFile, AttachFileDto>(MemberList.Destination);
            CreateMap<AttachFileCreateDto, AttachFile>(MemberList.Source);
            CreateMap<AttachFileUpdateDto, AttachFile>(MemberList.Source);
            
            // AttachCatalogueTemplate 映射
            CreateMap<AttachCatalogueTemplate, AttachCatalogueTemplateDto>(MemberList.Destination)
                .ForMember(dest => dest.Permissions, opt => opt.MapFrom(src => src.Permissions));
            CreateMap<AttachCatalogueTemplate, AttachCatalogueTemplateTreeDto>(MemberList.Destination)
                .ForMember(dest => dest.Permissions, opt => opt.MapFrom(src => src.Permissions))
                .ForMember(dest => dest.Children, opt => opt.MapFrom(src => src.Children));
            CreateMap<CreateUpdateAttachCatalogueTemplateDto, AttachCatalogueTemplate>(MemberList.Source);
            
            // 权限相关映射
            CreateMap<AttachCatalogueTemplatePermission, AttachCatalogueTemplatePermissionDto>(MemberList.Destination);
            CreateMap<AttachCatalogueTemplatePermissionDto, AttachCatalogueTemplatePermission>(MemberList.Source);
            
            // 元数据字段映射
            CreateMap<MetaField, MetaFieldDto>(MemberList.Destination);
            CreateMap<CreateUpdateMetaFieldDto, MetaField>(MemberList.Source);
        }
    }
}
