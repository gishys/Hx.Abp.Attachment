using AutoMapper;
using Hx.Abp.Attachment.Application.Contracts;
using Hx.Abp.Attachment.Domain;

namespace Hx.Abp.Attachment.Application
{
    public class AttachmentAutoMapperProfile : Profile
    {
        public AttachmentAutoMapperProfile()
        {
            CreateMap<AttachCatalogue, AttachCatalogueDto>(MemberList.Destination);
        }
    }
}
