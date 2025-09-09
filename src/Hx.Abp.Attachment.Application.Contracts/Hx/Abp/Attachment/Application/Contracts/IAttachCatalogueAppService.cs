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
        /// 混合检索分类：结合全文检索和文本向量检索
        /// </summary>
        /// <param name="searchText">搜索文本</param>
        /// <param name="reference">业务引用</param>
        /// <param name="referenceType">业务类型</param>
        /// <param name="limit">返回数量限制</param>
        /// <param name="queryTextVector">查询文本向量</param>
        /// <param name="similarityThreshold">相似度阈值</param>
        /// <returns>匹配的分类列表</returns>
        Task<List<AttachCatalogueDto>> SearchByHybridAsync(string searchText, string? reference = null, int? referenceType = null, int limit = 10, string? queryTextVector = null, float similarityThreshold = 0.7f);


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

        /// <summary>
        /// 批量设置元数据字段（创建、更新、删除）
        /// </summary>
        /// <param name="id">分类ID</param>
        /// <param name="metaFields">元数据字段列表</param>
        /// <returns></returns>
        Task SetMetaFieldsAsync(Guid id, List<CreateUpdateMetaFieldDto> metaFields);

        /// <summary>
        /// 获取元数据字段
        /// </summary>
        /// <param name="id">分类ID</param>
        /// <param name="fieldKey">字段键名</param>
        /// <returns></returns>
        Task<MetaFieldDto?> GetMetaFieldAsync(Guid id, string fieldKey);

        /// <summary>
        /// 获取所有启用的元数据字段
        /// </summary>
        /// <param name="id">分类ID</param>
        /// <returns></returns>
        Task<List<MetaFieldDto>> GetEnabledMetaFieldsAsync(Guid id);

        /// <summary>
        /// 根据模板ID和版本查找分类
        /// </summary>
        /// <param name="templateId">模板ID</param>
        /// <param name="templateVersion">模板版本号，null表示查找所有版本</param>
        /// <returns>匹配的分类列表</returns>
        Task<List<AttachCatalogueDto>> FindByTemplateAsync(Guid templateId, int? templateVersion = null);

        /// <summary>
        /// 根据模板ID查找所有版本的分类
        /// </summary>
        /// <param name="templateId">模板ID</param>
        /// <returns>匹配的分类列表</returns>
        Task<List<AttachCatalogueDto>> FindByTemplateIdAsync(Guid templateId);

        /// <summary>
        /// 获取分类树形结构（用于树状展示）
        /// 基于行业最佳实践，支持多种查询条件和性能优化
        /// 参考 AttachCatalogueTemplateRepository 的最佳实践，使用路径优化
        /// </summary>
        /// <param name="reference">业务引用，null表示查询所有业务</param>
        /// <param name="referenceType">业务类型，null表示查询所有类型</param>
        /// <param name="catalogueFacetType">分类分面类型，null表示查询所有类型</param>
        /// <param name="cataloguePurpose">分类用途，null表示查询所有用途</param>
        /// <param name="includeChildren">是否包含子节点，默认true</param>
        /// <param name="includeFiles">是否包含附件文件，默认false</param>
        /// <param name="fulltextQuery">全文搜索查询，支持中文分词</param>
        /// <param name="templateId">模板ID过滤，null表示查询所有模板</param>
        /// <param name="templateVersion">模板版本过滤，null表示查询所有版本</param>
        /// <returns>分类树形结构列表</returns>
        Task<List<AttachCatalogueTreeDto>> GetCataloguesTreeAsync(
            string? reference = null,
            int? referenceType = null,
            FacetType? catalogueFacetType = null,
            TemplatePurpose? cataloguePurpose = null,
            bool includeChildren = true,
            bool includeFiles = false,
            string? fulltextQuery = null,
            Guid? templateId = null,
            int? templateVersion = null);
    }
}
