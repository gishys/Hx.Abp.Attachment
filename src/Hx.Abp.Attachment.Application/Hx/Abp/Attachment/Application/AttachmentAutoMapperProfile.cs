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
            CreateMap<AttachCatalogue, AttachCatalogueDto>(MemberList.Destination);
            CreateMap<AttachCatalogueCreateDto, AttachCatalogue>(MemberList.Source);
            CreateMap<AttachCatalogueUpdateDto, AttachCatalogue>(MemberList.Source);
            
            // AttachFile 映射
            CreateMap<AttachFile, AttachFileDto>(MemberList.Destination);
            CreateMap<AttachFileCreateDto, AttachFile>(MemberList.Source);
            CreateMap<AttachFileUpdateDto, AttachFile>(MemberList.Source);
            
            // AttachCatalogueTemplate 映射
            CreateMap<AttachCatalogueTemplate, AttachCatalogueTemplateDto>(MemberList.Destination);
            CreateMap<CreateUpdateAttachCatalogueTemplateDto, AttachCatalogueTemplate>(MemberList.Source);
        }
    }
}
