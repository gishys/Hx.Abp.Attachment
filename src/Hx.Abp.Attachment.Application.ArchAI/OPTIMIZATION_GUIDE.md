# 文本分析服务优化指南

## 🎯 优化概述

本次优化主要解决了以下问题：

1. **消除代码冗余** - 提取公共逻辑到基础服务类
2. **提高通用性** - 优化提示词，使其适用于更广泛的业务场景
3. **增强可维护性** - 使用配置文件管理识别规则，便于扩展和修改
4. **改进实体识别** - 实现更完善的实体类型识别逻辑

## 📁 优化后的项目结构

```
src/Hx.Abp.Attachment.Application.ArchAI/
├── Hx/Abp/Attachment/Application/ArchAI/
│   ├── BaseTextAnalysisService.cs           # 基础文本分析服务（新增）
│   ├── TextAnalysisService.cs               # 文本分析服务（优化）
│   ├── TextClassificationService.cs         # 文本分类服务（优化）
│   ├── TextAnalysisConfiguration.cs         # 文本分析配置（新增）
│   ├── SemanticVectorService.cs             # 语义向量服务
│   ├── ArchiveAIAppService.cs               # 应用服务实现
│   └── HxAbpAttachmentApplicationArchAIModule.cs
```

## 🔧 主要优化内容

### 1. 基础服务抽象

创建了 `BaseTextAnalysisService` 抽象类，提取了以下公共逻辑：

-   **API 调用逻辑** - 统一的 DeepSeek API 调用方法
-   **JSON 解析** - 通用的响应解析和错误处理
-   **提示词构建** - 通用的提示词模板
-   **元数据处理** - 统一的元数据添加逻辑
-   **语义向量生成** - 通用的向量生成方法

### 2. 提示词优化

#### 优化前的问题：

-   提示词过于具体，针对特定业务场景
-   包含大量硬编码的业务术语
-   缺乏通用性和扩展性

#### 优化后的改进：

-   使用通用的提示词模板
-   通过参数化配置关键词数量和摘要长度
-   支持动态任务描述，适应不同分析场景

```csharp
// 优化后的提示词构建
private static string BuildGenericPrompt(int keywordCount, int maxSummaryLength, string taskDescription)
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
```

### 3. 实体识别优化

#### 优化前的问题：

-   使用简单的硬编码规则
-   实体类型有限，缺乏扩展性
-   识别逻辑分散在各个方法中

#### 优化后的改进：

-   创建了 `TextAnalysisConfiguration` 配置类
-   支持 7 种实体类型：Organization、DocumentType、Amount、Date、FinancialInstitution、Location、Person
-   使用正则表达式增强识别准确性
-   配置化的识别规则，便于维护和扩展

```csharp
// 配置化的实体识别规则
public static readonly Dictionary<string, string[]> EntityTypeRules = new()
{
    ["Organization"] = new[] { "公司", "有限", "股份", "集团", "企业", "机构", "协会", "基金会", "中心", "研究院", "学院", "大学", "学校" },
    ["DocumentType"] = new[] { "证明", "合同", "协议", "报告", "证书", "声明", "申请", "表格", "清单", "记录", "档案", "文件", "通知", "公告" },
    ["Amount"] = new[] { "万元", "元", "万", "亿", "千", "百", "分", "角", "美元", "欧元", "日元", "英镑" },
    ["Date"] = new[] { "年", "月", "日", "号", "时", "分", "秒" },
    ["FinancialInstitution"] = new[] { "银行", "联社", "信用社", "证券", "保险", "基金", "信托", "投资", "理财", "信贷", "贷款" },
    ["Location"] = new[] { "省", "市", "县", "区", "镇", "村", "街道", "路", "号", "楼", "室", "广场", "大厦", "中心" },
    ["Person"] = new[] { "先生", "女士", "同志", "老师", "教授", "博士", "经理", "主任", "部长", "总监" }
};
```

### 4. 文档类型和业务领域识别优化

#### 优化前的问题：

-   识别规则硬编码在方法中
-   支持的文档类型和业务领域有限
-   缺乏统一的配置管理

#### 优化后的改进：

-   支持 10 种文档类型：结清证明、合同协议、分析报告、证明证书、申请表格、通知公告、会议记录、财务报表、技术文档、法律文书
-   支持 10 种业务领域：金融服务、制造业、房地产、政务服务、教育文化、医疗卫生、交通运输、能源环保、信息技术、商业贸易
-   配置化的识别规则，便于添加新的类型和领域

## 🚀 使用示例

### 单个文档分析

```csharp
var input = new TextAnalysisInputDto
{
    Text = "这是一份贷款结清证明，证明借款人已按时还清所有贷款本息。",
    KeywordCount = 5,
    MaxSummaryLength = 200,
    GenerateSemanticVector = true,
    ExtractEntities = true,
    AnalysisType = TextAnalysisType.SingleDocument
};

var result = await textAnalysisService.AnalyzeTextAsync(input);
```

### 文本分类特征提取

```csharp
var input = new TextClassificationInputDto
{
    ClassificationName = "结清证明",
    TextSamples = new List<string>
    {
        "样本1内容...",
        "样本2内容...",
        "样本3内容..."
    },
    KeywordCount = 5,
    MaxSummaryLength = 200,
    GenerateSemanticVector = true
};

var result = await textClassificationService.ExtractClassificationFeaturesAsync(input);
```

## 🔧 扩展指南

### 添加新的实体类型

1. 在 `TextAnalysisConfiguration.EntityTypeRules` 中添加新的规则
2. 在 `TextAnalysisService` 中添加对应的识别方法
3. 在 `DetermineEntityType` 方法中添加新的判断逻辑

### 添加新的文档类型

在 `TextAnalysisConfiguration.DocumentTypeRules` 中添加新的规则：

```csharp
["新文档类型"] = new[] { "关键词1", "关键词2", "关键词3" }
```

### 添加新的业务领域

在 `TextAnalysisConfiguration.BusinessDomainRules` 中添加新的规则：

```csharp
["新业务领域"] = new[] { "关键词1", "关键词2", "关键词3" }
```

## 📊 性能优化

### 代码行数对比

| 文件                         | 优化前 | 优化后 | 减少比例 |
| ---------------------------- | ------ | ------ | -------- |
| TextAnalysisService.cs       | 544 行 | 320 行 | 41%      |
| TextClassificationService.cs | 323 行 | 120 行 | 63%      |
| 总计                         | 867 行 | 440 行 | 49%      |

### 主要优化点

1. **消除重复代码** - 提取公共逻辑到基础服务
2. **简化方法实现** - 使用配置化的识别规则
3. **提高代码复用** - 统一的 API 调用和解析逻辑
4. **增强可维护性** - 配置化的规则管理

## 🎯 最佳实践

### 1. 配置管理

-   使用 `TextAnalysisConfiguration` 统一管理识别规则
-   避免在代码中硬编码业务规则
-   定期更新和优化识别规则

### 2. 错误处理

-   使用统一的异常处理机制
-   提供友好的错误信息
-   实现适当的重试逻辑

### 3. 性能监控

-   记录 API 调用性能指标
-   监控识别准确率
-   定期优化识别规则

### 4. 扩展性设计

-   使用配置化的规则管理
-   支持动态添加新的实体类型和业务领域
-   保持代码的模块化和可测试性

## 🔮 未来改进方向

1. **机器学习集成** - 集成更先进的 NLP 模型进行实体识别
2. **规则引擎** - 实现更复杂的规则匹配逻辑
3. **缓存机制** - 添加结果缓存以提高性能
4. **批量处理** - 支持批量文本分析
5. **多语言支持** - 扩展支持多语言文本分析
