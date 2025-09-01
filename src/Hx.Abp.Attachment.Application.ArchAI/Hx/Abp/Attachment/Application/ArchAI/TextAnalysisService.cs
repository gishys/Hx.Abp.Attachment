using Hx.Abp.Attachment.Application.ArchAI.Contracts;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Volo.Abp;

namespace Hx.Abp.Attachment.Application.ArchAI
{
    /// <summary>
    /// 文本分析服务
    /// </summary>
    public class TextAnalysisService(
        ILogger<TextAnalysisService> logger,
        HttpClient httpClient,
        SemanticVectorService semanticVectorService,
        AIServiceFactory aiServiceFactory) : BaseTextAnalysisService(logger, httpClient, semanticVectorService)
    {

        /// <summary>
        /// 分析文本并生成摘要和关键词
        /// </summary>
        /// <param name="input">文本分析输入参数</param>
        /// <returns>文本分析结果</returns>
        public async Task<TextAnalysisDto> AnalyzeTextAsync(TextAnalysisInputDto input)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                _logger.LogInformation("开始分析文本，长度: {TextLength}", input.Text.Length);

                // 使用新的文档智能分析服务
                var documentAnalysisService = aiServiceFactory.GetDocumentAnalysisService();
                var result = await documentAnalysisService.AnalyzeDocumentAsync(input);

                // 添加元数据
                stopwatch.Stop();
                AddBasicMetadata(result, input.Text.Length, stopwatch.ElapsedMilliseconds);

                // 提取实体信息
                if (input.ExtractEntities)
                {
                    result.Entities = ExtractEntities(input.Text, result.Keywords);
                }

                // 识别文档类型和业务领域
                result.DocumentType = IdentifyDocumentType(result.Summary, result.Keywords);
                result.BusinessDomain = IdentifyBusinessDomain(result.Summary, result.Keywords);

                // 生成语义向量
                if (input.GenerateSemanticVector)
                {
                    result.SemanticVector = await GenerateSemanticVectorAsync(result.Summary, result.Keywords);
                }

                _logger.LogInformation("文本分析完成，提取关键词数量: {KeywordCount}, 置信度: {Confidence}, 处理时间: {ProcessingTime}ms", 
                    result.Keywords.Count, result.Confidence, stopwatch.ElapsedMilliseconds);
                return result;
            }
            catch (UserFriendlyException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "文本分析过程中发生错误");
                throw new UserFriendlyException("文本分析服务暂时不可用，请稍后再试");
            }
        }

        /// <summary>
        /// 构建分析提示词
        /// </summary>
        private static string BuildAnalysisPrompt(TextAnalysisInputDto input)
        {
            return input.AnalysisType == TextAnalysisType.TextClassification 
                ? BuildClassificationPrompt(input) 
                : BuildSingleDocumentPrompt(input);
        }

        /// <summary>
        /// 构建通用分析提示词
        /// </summary>
        private static new string BuildGenericPrompt(int keywordCount, int maxSummaryLength, string taskDescription)
        {
            return $@"
# 通用文本分析专家指令

## 任务要求
{taskDescription}

## 输出格式要求
请严格按照以下JSON格式返回结果，不要包含任何其他内容：

{{
  ""summary"": ""文本摘要内容，控制在{maxSummaryLength}字符以内，突出核心信息和主要观点"",
  ""keywords"": [""关键词1"", ""关键词2"", ""关键词3"", ""关键词4"", ""关键词5""],
  ""confidence"": 0.95
}}

## 分析指导原则
1. **摘要生成**：
   - 提取文本的核心信息和主要观点
   - 保持逻辑清晰，语言简洁
   - 确保摘要完整表达原文主旨
   - 重点关注实体名称、时间、金额、地点等关键信息

2. **关键词提取**：
   - 提取{keywordCount}个最重要的关键词
   - 关键词应具有代表性，能体现文本主题
   - 包含实体名词、专业术语、核心概念等
   - 按重要性排序，优先提取：
     * 实体名称（公司、机构、人名、地名等）
     * 文档类型标识
     * 关键业务术语
     * 重要时间节点
     * 数值信息（金额、数量等）

3. **置信度评估**：
   - 基于文本清晰度、信息完整性评估
   - 范围0.0-1.0，0.9以上表示高置信度
   - 考虑文本结构、信息密度、专业术语使用等因素

## 注意事项
- 只返回JSON格式结果，不要包含解释文字
- 确保JSON格式正确，可以被直接解析
- 关键词应该是单个词或短语，不要包含标点符号
- 摘要应该客观准确，避免主观判断
- 重点关注对后续语义匹配有用的信息";
        }

        /// <summary>
        /// 构建单个文档分析提示词
        /// </summary>
        private static string BuildSingleDocumentPrompt(TextAnalysisInputDto input)
        {
            var taskDescription = "请对输入的文本进行深度分析，生成结构化的摘要和关键词提取结果，用于后续的语义匹配和模板分类。";
            return BuildGenericPrompt(input.KeywordCount, input.MaxSummaryLength, taskDescription);
        }

        /// <summary>
        /// 构建文本分类分析提示词
        /// </summary>
        private static string BuildClassificationPrompt(TextAnalysisInputDto input)
        {
            var classificationName = !string.IsNullOrEmpty(input.ClassificationName) 
                ? $"（{input.ClassificationName}）" 
                : "";

            var taskDescription = $"请对输入的多个同类文本样本进行深度分析，提取该类文本{classificationName}的通用特征，生成结构化的分类描述和特征关键词，用于文本分类和模板匹配。";
            return BuildGenericPrompt(input.KeywordCount, input.MaxSummaryLength, taskDescription);
        }

        /// <summary>
        /// 解析分析结果
        /// </summary>
        private static new TextAnalysisDto ParseAnalysisResult(string content)
        {
            try
            {
                // 尝试直接解析JSON
                var result = JsonSerializer.Deserialize<TextAnalysisDto>(content, JsonOptions);
                if (result != null && !string.IsNullOrEmpty(result.Summary))
                {
                    return result;
                }
            }
            catch (JsonException ex)
            {
                // 记录JSON解析错误，继续尝试其他方法
                System.Diagnostics.Debug.WriteLine($"直接JSON解析失败: {ex.Message}");
            }

            // 尝试从文本中提取JSON
            var jsonContent = ExtractJsonFromText(content);
            if (!string.IsNullOrEmpty(jsonContent))
            {
                try
                {
                    var result = JsonSerializer.Deserialize<TextAnalysisDto>(jsonContent, JsonOptions);
                    if (result != null && !string.IsNullOrEmpty(result.Summary))
                    {
                        return result;
                    }
                }
                catch (JsonException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"提取JSON解析失败: {ex.Message}");
                }
            }

            // 备用方案：手动解析
            return ParseAnalysisResultManually(content);
        }

        /// <summary>
        /// 从文本中提取JSON内容
        /// </summary>
        private static string ExtractJsonFromText(string content)
        {
            var jsonStart = content.IndexOf('{');
            var jsonEnd = content.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                return content.Substring(jsonStart, jsonEnd - jsonStart + 1);
            }

            return string.Empty;
        }

        /// <summary>
        /// 手动解析分析结果（备用方案）
        /// </summary>
        private static TextAnalysisDto ParseAnalysisResultManually(string content)
        {
            var result = new TextAnalysisDto
            {
                Summary = "文本分析完成",
                Keywords = [],
                Confidence = 0.8
            };

            // 简单的关键词提取逻辑
            var words = content.Split([' ', ',', '.', ';', ':', '!', '?', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2)
                .Select(w => w.Trim('"', '\'', '(', ')', '[', ']', '{', '}'))
                .Where(w => w.Length > 2)
                .GroupBy(w => w.ToLower())
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => g.Key);

            result.Keywords.AddRange(words);

            return result;
        }

        /// <summary>
        /// 提取实体信息
        /// </summary>
        private static List<EntityInfo> ExtractEntities(string text, List<string> keywords)
        {
            var entities = new List<EntityInfo>();
            
            // 简单的实体提取逻辑
            foreach (var keyword in keywords)
            {
                var entityType = DetermineEntityType(keyword, text);
                if (!string.IsNullOrEmpty(entityType))
                {
                    entities.Add(new EntityInfo
                    {
                        Name = keyword,
                        Type = entityType,
                        Value = keyword,
                        Confidence = 0.8
                    });
                }
            }

            return entities;
        }

        /// <summary>
        /// 确定实体类型
        /// </summary>
        private static string DetermineEntityType(string keyword, string text)
        {
            // 组织实体识别
            if (IsOrganizationEntity(keyword, text))
                return "Organization";
            
            // 文档类型识别
            if (IsDocumentTypeEntity(keyword, text))
                return "DocumentType";
            
            // 金额实体识别
            if (IsAmountEntity(keyword, text))
                return "Amount";
            
            // 日期实体识别
            if (IsDateEntity(keyword, text))
                return "Date";
            
            // 金融机构识别
            if (IsFinancialInstitutionEntity(keyword, text))
                return "FinancialInstitution";
            
            // 地点实体识别
            if (IsLocationEntity(keyword, text))
                return "Location";
            
            // 人名实体识别
            if (IsPersonEntity(keyword, text))
                return "Person";

            return string.Empty;
        }

        /// <summary>
        /// 判断是否为组织实体
        /// </summary>
        private static bool IsOrganizationEntity(string keyword, string text)
        {
            // 基础关键词匹配
            var isOrgKeyword = TextAnalysisConfiguration.EntityTypeRules["Organization"].Any(k => keyword.Contains(k));
            if (!isOrgKeyword) return false;

            // 上下文验证：检查周围是否有组织相关的上下文
            var contextKeywords = TextAnalysisConfiguration.EntityContextKeywords["Organization"];
            var hasContext = contextKeywords.Any(k => text.Contains(k));
            
            return hasContext;
        }

        /// <summary>
        /// 判断是否为文档类型实体
        /// </summary>
        private static bool IsDocumentTypeEntity(string keyword, string text)
        {
            // 基础关键词匹配
            var isDocKeyword = TextAnalysisConfiguration.EntityTypeRules["DocumentType"].Any(k => keyword.Contains(k));
            if (!isDocKeyword) return false;

            // 上下文验证：检查周围是否有文档相关的上下文
            var contextKeywords = TextAnalysisConfiguration.EntityContextKeywords["DocumentType"];
            var hasContext = contextKeywords.Any(k => text.Contains(k));
            
            return hasContext;
        }

        /// <summary>
        /// 判断是否为金额实体
        /// </summary>
        private static bool IsAmountEntity(string keyword, string text)
        {
            // 基础关键词匹配
            var isAmountKeyword = TextAnalysisConfiguration.EntityTypeRules["Amount"].Any(k => keyword.Contains(k));
            var isAmountPattern = System.Text.RegularExpressions.Regex.IsMatch(keyword, TextAnalysisConfiguration.AmountRegexPattern);
            
            if (!isAmountKeyword && !isAmountPattern) return false;

            // 上下文验证：检查周围是否有金额相关的上下文
            var contextKeywords = TextAnalysisConfiguration.EntityContextKeywords["Amount"];
            var hasContext = contextKeywords.Any(k => text.Contains(k));
            
            return hasContext;
        }

        /// <summary>
        /// 判断是否为日期实体
        /// </summary>
        private static bool IsDateEntity(string keyword, string text)
        {
            var dateKeywords = TextAnalysisConfiguration.EntityTypeRules["Date"];
            var hasDateKeywords = dateKeywords.Count(k => keyword.Contains(k)) >= 2;
            var isDatePattern = System.Text.RegularExpressions.Regex.IsMatch(keyword, TextAnalysisConfiguration.DateRegexPattern);
            
            if (!hasDateKeywords && !isDatePattern) return false;

            // 上下文验证：检查周围是否有日期相关的上下文
            var contextKeywords = TextAnalysisConfiguration.EntityContextKeywords["Date"];
            var hasContext = contextKeywords.Any(k => text.Contains(k));
            
            return hasContext;
        }

        /// <summary>
        /// 判断是否为金融机构实体
        /// </summary>
        private static bool IsFinancialInstitutionEntity(string keyword, string text)
        {
            // 基础关键词匹配
            var isFinKeyword = TextAnalysisConfiguration.EntityTypeRules["FinancialInstitution"].Any(k => keyword.Contains(k));
            if (!isFinKeyword) return false;

            // 上下文验证：检查周围是否有金融相关的上下文
            var contextKeywords = TextAnalysisConfiguration.EntityContextKeywords["FinancialInstitution"];
            var hasContext = contextKeywords.Any(k => text.Contains(k));
            
            return hasContext;
        }

        /// <summary>
        /// 判断是否为地点实体
        /// </summary>
        private static bool IsLocationEntity(string keyword, string text)
        {
            // 基础关键词匹配
            var isLocationKeyword = TextAnalysisConfiguration.EntityTypeRules["Location"].Any(k => keyword.Contains(k));
            if (!isLocationKeyword) return false;

            // 上下文验证：检查周围是否有地点相关的上下文
            var contextKeywords = TextAnalysisConfiguration.EntityContextKeywords["Location"];
            var hasContext = contextKeywords.Any(k => text.Contains(k));
            
            return hasContext;
        }

        /// <summary>
        /// 判断是否为人名实体
        /// </summary>
        private static bool IsPersonEntity(string keyword, string text)
        {
            var personKeywords = TextAnalysisConfiguration.EntityTypeRules["Person"];
            var hasPersonKeyword = personKeywords.Any(k => keyword.Contains(k));
            var isNameLength = keyword.Length >= TextAnalysisConfiguration.PersonNameLength.Min && 
                              keyword.Length <= TextAnalysisConfiguration.PersonNameLength.Max;
            
            if (!hasPersonKeyword && !isNameLength) return false;

            // 上下文验证：检查周围是否有人名相关的上下文
            var contextKeywords = TextAnalysisConfiguration.EntityContextKeywords["Person"];
            var hasContext = contextKeywords.Any(k => text.Contains(k));
            
            // 如果有关键词匹配，直接返回true
            if (hasPersonKeyword) return true;
            
            // 如果是姓名长度且没有组织或文档类型特征，且有上下文支持，则认为是人名
            return isNameLength && 
                   !IsOrganizationEntity(keyword, text) && 
                   !IsDocumentTypeEntity(keyword, text) && 
                   hasContext;
        }

        /// <summary>
        /// 识别文档类型
        /// </summary>
        private static string IdentifyDocumentType(string summary, List<string> keywords)
        {
            var text = (summary + " " + string.Join(" ", keywords)).ToLower();
            
            foreach (var rule in TextAnalysisConfiguration.DocumentTypeRules)
            {
                if (rule.Value.Any(keyword => text.Contains(keyword)))
                {
                    return rule.Key;
                }
            }
            
            return "其他文档";
        }

        /// <summary>
        /// 识别业务领域
        /// </summary>
        private static new string IdentifyBusinessDomain(string summary, List<string> keywords)
        {
            var text = (summary + " " + string.Join(" ", keywords)).ToLower();
            
            // 定义业务领域识别规则
            var businessDomainRules = new Dictionary<string, string[]>
            {
                ["金融服务"] = ["银行", "贷款", "金融", "保险", "证券", "基金", "理财", "信贷", "投资", "融资", "担保", "抵押"],
                ["制造业"] = ["工程", "机械", "制造", "生产", "加工", "装配", "设备", "工艺", "技术", "质量", "检测"],
                ["房地产"] = ["房地产", "房产", "不动产", "房屋", "建筑", "楼盘", "物业", "地产", "土地", "开发", "建设"],
                ["政务服务"] = ["政府", "政务", "行政", "机关", "部门", "机构", "审批", "许可", "登记", "备案", "监管"],
                ["教育文化"] = ["教育", "学校", "培训", "文化", "艺术", "媒体", "出版", "新闻", "广播", "电视", "网络"],
                ["医疗卫生"] = ["医疗", "卫生", "医院", "诊所", "药品", "治疗", "诊断", "护理", "保健", "康复", "防疫"],
                ["交通运输"] = ["交通", "运输", "物流", "快递", "货运", "客运", "车辆", "道路", "桥梁", "港口", "机场"],
                ["能源环保"] = ["能源", "环保", "电力", "石油", "天然气", "煤炭", "新能源", "节能", "减排", "污染", "治理"],
                ["信息技术"] = ["信息", "技术", "软件", "硬件", "网络", "数据", "系统", "平台", "应用", "开发", "维护"],
                ["商业贸易"] = ["商业", "贸易", "销售", "采购", "供应", "零售", "批发", "进出口", "市场", "营销", "品牌"]
            };

            foreach (var rule in businessDomainRules)
            {
                if (rule.Value.Any(keyword => text.Contains(keyword)))
                {
                    return rule.Key;
                }
            }
            
            return "其他领域";
        }
    }
}
