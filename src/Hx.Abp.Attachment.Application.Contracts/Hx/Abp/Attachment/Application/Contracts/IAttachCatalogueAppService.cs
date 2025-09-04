using Hx.Abp.Attachment.Domain.Shared;
using Volo.Abp.Application.Services;

namespace Hx.Abp.Attachment.Application.Contracts
{
    public interface IAttachCatalogueAppService : IApplicationService
    {
        Task<AttachCatalogueDto?> CreateAsync(AttachCatalogueCreateDto create, CatalogueCreateMode? createMode);
        Task<List<AttachCatalogueDto>> FindByReferenceAsync(List<GetAttachListInput> inputs);
        Task<AttachCatalogueDto?> UpdateAsync(Guid id, AttachCatalogueCreateDto input);
        Task DeleteAsync(Guid id);
        Task DeleteSingleFileAsync(Guid attachFileId);
        Task<AttachFileDto> UpdateSingleFileAsync(Guid catalogueId, Guid attachFileId, AttachFileCreateDto input);
        Task<List<AttachFileDto>> QueryFilesAsync(Guid catalogueId);
        Task<AttachFileDto> QueryFileAsync(Guid attachFileId);
        Task<List<AttachFileDto>> CreateFilesAsync(Guid? id, List<AttachFileCreateDto> inputs, string? prefix = null);
        Task<FileVerifyResultDto> VerifyUploadAsync(List<GetAttachListInput> inputs, bool details = false);
        Task<List<AttachCatalogueDto>> CreateManyAsync(List<AttachCatalogueCreateDto> inputs, CatalogueCreateMode createMode);
        Task DeleteByReferenceAsync(List<AttachCatalogueCreateDto> inputs);
        Task<AttachCatalogueDto?> GetAttachCatalogueByFileIdAsync(Guid fileId);

        /// <summary>
        /// 全文检索分类
        /// </summary>
        /// <param name="searchText">搜索文本</param>
        /// <param name="reference">业务引用</param>
        /// <param name="referenceType">业务类型</param>
        /// <param name="limit">返回数量限制</param>
        /// <returns>匹配的分类列表</returns>
        Task<List<AttachCatalogueDto>> SearchByFullTextAsync(string searchText, string? reference = null, int? referenceType = null, int limit = 10);


        /// <summary>
        /// 设置分类权限
        /// </summary>
        /// <param name="id">分类ID</param>
        /// <param name="permissions">权限列表</param>
        /// <returns></returns>
        Task SetPermissionsAsync(Guid id, List<AttachCatalogueTemplatePermissionDto> permissions);

        /// <summary>
        /// 获取分类权限
        /// </summary>
        /// <param name="id">分类ID</param>
        /// <returns></returns>
        Task<List<AttachCatalogueTemplatePermissionDto>> GetPermissionsAsync(Guid id);

        /// <summary>
        /// 检查用户权限
        /// </summary>
        /// <param name="id">分类ID</param>
        /// <param name="userId">用户ID</param>
        /// <param name="action">权限操作</param>
        /// <returns></returns>
        Task<bool> HasPermissionAsync(Guid id, Guid userId, PermissionAction action);

        /// <summary>
        /// 获取分类标识描述
        /// </summary>
        /// <param name="id">分类ID</param>
        /// <returns></returns>
        Task<string> GetCatalogueIdentifierDescriptionAsync(Guid id);

        /// <summary>
        /// 根据分类标识查询
        /// </summary>
        /// <param name="catalogueType">分类类型</param>
        /// <param name="cataloguePurpose">分类用途</param>
        /// <returns></returns>
        Task<List<AttachCatalogueDto>> GetByCatalogueIdentifierAsync(FacetType? catalogueFacetType = null, TemplatePurpose? cataloguePurpose = null);

        /// <summary>
        /// 根据向量维度查询
        /// </summary>
        /// <param name="minDimension">最小维度</param>
        /// <param name="maxDimension">最大维度</param>
        /// <returns></returns>
        Task<List<AttachCatalogueDto>> GetByVectorDimensionAsync(int? minDimension = null, int? maxDimension = null);
    }
}
