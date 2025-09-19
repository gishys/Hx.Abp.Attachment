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
        /// <param name="searchText">搜索文本（可选）</param>
        /// <param name="reference">业务引用</param>
        /// <param name="referenceType">业务类型</param>
        /// <param name="limit">返回数量限制</param>
        /// <param name="queryTextVector">查询文本向量</param>
        /// <param name="similarityThreshold">相似度阈值</param>
        /// <returns>匹配的分类列表</returns>
        Task<List<AttachCatalogueDto>> SearchByHybridAsync(string? searchText = null, string? reference = null, int? referenceType = null, int limit = 10, string? queryTextVector = null, float similarityThreshold = 0.7f);


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

        /// <summary>
        /// 智能分类文件上传和推荐
        /// 基于OCR内容进行智能分类推荐，适用于文件自动归类场景
        /// </summary>
        /// <param name="catalogueId">分类ID</param>
        /// <param name="inputs">文件列表</param>
        /// <param name="prefix">文件前缀</param>
        /// <returns>智能分类推荐结果列表</returns>
        Task<List<SmartClassificationResultDto>> CreateFilesWithSmartClassificationAsync(
            Guid catalogueId, 
            List<AttachFileCreateDto> inputs, 
            string? prefix = null);

        /// <summary>
        /// 确定文件分类
        /// 将文件归类到指定分类，并更新相关属性
        /// </summary>
        /// <param name="fileId">文件ID</param>
        /// <param name="catalogueId">分类ID</param>
        /// <param name="ocrContent">OCR全文内容</param>
        /// <returns>更新后的文件信息</returns>
        Task<AttachFileDto> ConfirmFileClassificationAsync(Guid fileId, Guid catalogueId, string? ocrContent = null);

        /// <summary>
        /// 批量确定文件分类
        /// 将多个文件归类到指定分类，并更新相关属性
        /// </summary>
        /// <param name="requests">文件分类请求列表</param>
        /// <returns>更新后的文件信息列表</returns>
        Task<List<AttachFileDto>> ConfirmFileClassificationsAsync(List<ConfirmFileClassificationRequest> requests);

        /// <summary>
        /// 根据业务引用和模板用途获取文件列表
        /// 查询未归档的文件列表
        /// </summary>
        /// <param name="reference">业务引用</param>
        /// <param name="templatePurpose">模板用途</param>
        /// <returns>文件列表</returns>
        Task<List<AttachFileDto>> GetFilesByReferenceAndTemplatePurposeAsync(string reference, TemplatePurpose templatePurpose);

        /// <summary>
        /// 根据业务引用和模板用途获取文件列表并进行智能分类推荐
        /// 查询未归档的文件列表，并为每个文件提供分类推荐
        /// </summary>
        /// <param name="reference">业务引用</param>
        /// <param name="templatePurpose">模板用途</param>
        /// <returns>智能分类推荐结果列表</returns>
        Task<List<SmartClassificationResultDto>> GetFilesWithSmartClassificationByReferenceAndTemplatePurposeAsync(string reference, TemplatePurpose templatePurpose);

        /// <summary>
        /// 根据归档状态查询分类
        /// </summary>
        /// <param name="isArchived">归档状态</param>
        /// <param name="reference">业务引用过滤</param>
        /// <param name="referenceType">业务类型过滤</param>
        /// <returns>匹配的分类列表</returns>
        Task<List<AttachCatalogueDto>> GetByArchivedStatusAsync(bool isArchived, string? reference = null, int? referenceType = null);

        /// <summary>
        /// 批量设置归档状态
        /// </summary>
        /// <param name="catalogueIds">分类ID列表</param>
        /// <param name="isArchived">归档状态</param>
        /// <returns>更新的记录数</returns>
        Task<int> SetArchivedStatusAsync(List<Guid> catalogueIds, bool isArchived);

        /// <summary>
        /// 设置分类归档状态
        /// </summary>
        /// <param name="id">分类ID</param>
        /// <param name="isArchived">归档状态</param>
        /// <returns>更新后的分类信息</returns>
        Task<AttachCatalogueDto?> SetCatalogueArchivedStatusAsync(Guid id, bool isArchived);

        /// <summary>
        /// 设置分类概要信息
        /// </summary>
        /// <param name="id">分类ID</param>
        /// <param name="summary">概要信息</param>
        /// <returns>更新后的分类信息</returns>
        Task<AttachCatalogueDto?> SetCatalogueSummaryAsync(Guid id, string? summary);

        /// <summary>
        /// 智能分析分类信息
        /// 基于分类下的文件内容，自动生成概要信息、分类标签、全文内容和元数据
        /// </summary>
        /// <param name="id">分类ID</param>
        /// <param name="forceUpdate">是否强制更新（默认false，只更新空值）</param>
        /// <returns>智能分析结果</returns>
        Task<IntelligentAnalysisResultDto> AnalyzeCatalogueIntelligentlyAsync(Guid id, bool forceUpdate = false);
    }
}
