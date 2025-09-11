# WorkflowConfig OCR 配置示例

## 概述

本文档提供了 `WorkflowConfig` 中 OCR 识别配置的示例，用于在文件上传时自动进行 OCR 文本识别。

## 基本配置示例

### 1. 启用 OCR 识别的基本配置

```json
{
  "workflowKey": "document_processing",
  "enableOcr": true,
  "ocrConfig": {
    "enableOcr": true,
    "supportedFileTypes": [".pdf", ".jpg", ".jpeg", ".png", ".tiff"],
    "minFileSize": 1024,
    "maxFileSize": 10485760,
    "minConfidence": 0.8,
    "isAsync": false,
    "timeoutSeconds": 30,
    "saveTextBlocks": true,
    "enableTextPostProcessing": true
  }
}
```

### 2. 异步 OCR 处理配置

```json
{
  "workflowKey": "async_document_processing",
  "enableOcr": true,
  "ocrConfig": {
    "enableOcr": true,
    "supportedFileTypes": [".pdf", ".jpg", ".jpeg", ".png"],
    "minFileSize": 10240,
    "maxFileSize": 52428800,
    "minConfidence": 0.75,
    "isAsync": true,
    "timeoutSeconds": 60,
    "saveTextBlocks": true,
    "enableTextPostProcessing": true,
    "textProcessingRules": [
      {
        "name": "removeSpecialChars",
        "ruleType": "regex",
        "expression": "[^\\w\\s\\u4e00-\\u9fff]",
        "isEnabled": true
      },
      {
        "name": "normalizeWhitespace",
        "ruleType": "replace",
        "expression": "\\s+",
        "isEnabled": true,
        "parameters": {
          "replacement": " "
        }
      }
    ]
  }
}
```

### 3. 高精度 OCR 配置

```json
{
  "workflowKey": "high_precision_ocr",
  "enableOcr": true,
  "ocrConfig": {
    "enableOcr": true,
    "supportedFileTypes": [".pdf", ".tiff", ".tif"],
    "minFileSize": 5120,
    "maxFileSize": 104857600,
    "minConfidence": 0.9,
    "isAsync": false,
    "timeoutSeconds": 120,
    "saveTextBlocks": true,
    "enableTextPostProcessing": true,
    "textProcessingRules": [
      {
        "name": "removeNoise",
        "ruleType": "regex",
        "expression": "^[\\s\\-_=]+$",
        "isEnabled": true
      },
      {
        "name": "fixLineBreaks",
        "ruleType": "replace",
        "expression": "\\n\\s*\\n",
        "isEnabled": true,
        "parameters": {
          "replacement": "\\n"
        }
      }
    ]
  }
}
```

### 4. 仅图片文件 OCR 配置

```json
{
  "workflowKey": "image_ocr_only",
  "enableOcr": true,
  "ocrConfig": {
    "enableOcr": true,
    "supportedFileTypes": [".jpg", ".jpeg", ".png", ".bmp", ".gif"],
    "minFileSize": 1024,
    "maxFileSize": 20971520,
    "minConfidence": 0.8,
    "isAsync": false,
    "timeoutSeconds": 30,
    "saveTextBlocks": false,
    "enableTextPostProcessing": true
  }
}
```

### 5. 禁用 OCR 的配置

```json
{
  "workflowKey": "no_ocr_processing",
  "enableOcr": false,
  "steps": [
    {
      "name": "文件验证",
      "stepType": "validation",
      "isRequired": true
    },
    {
      "name": "文件存储",
      "stepType": "storage",
      "isRequired": true
    }
  ]
}
```

## 配置参数说明

### WorkflowConfig 参数

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `workflowKey` | string | 否 | 工作流标识键 |
| `enableOcr` | boolean | 否 | 是否启用 OCR 识别（默认 false） |
| `ocrConfig` | OcrWorkflowConfig | 否 | OCR 配置对象 |

### OcrWorkflowConfig 参数

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| `enableOcr` | boolean | 否 | true | 是否启用 OCR 识别 |
| `supportedFileTypes` | string[] | 否 | 所有支持类型 | 支持的文件类型列表 |
| `minFileSize` | long | 否 | 无限制 | 最小文件大小（字节） |
| `maxFileSize` | long | 否 | 无限制 | 最大文件大小（字节） |
| `minConfidence` | double | 否 | 0.8 | 最小置信度阈值 |
| `isAsync` | boolean | 否 | false | 是否异步处理 |
| `timeoutSeconds` | int | 否 | 30 | 处理超时时间（秒） |
| `saveTextBlocks` | boolean | 否 | true | 是否保存文本块 |
| `enableTextPostProcessing` | boolean | 否 | true | 是否启用文本后处理 |
| `textProcessingRules` | TextProcessingRule[] | 否 | 无 | 文本处理规则列表 |

### TextProcessingRule 参数

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `name` | string | 是 | 规则名称 |
| `ruleType` | string | 是 | 规则类型（regex, replace, filter 等） |
| `expression` | string | 是 | 规则表达式 |
| `isEnabled` | boolean | 否 | true | 是否启用规则 |
| `parameters` | object | 否 | 无 | 规则参数 |

## 使用场景

### 1. 合同文档处理
- 启用 OCR 识别
- 支持 PDF 和图片格式
- 高置信度要求
- 保存文本块用于后续分析

### 2. 发票处理
- 异步 OCR 处理
- 支持多种图片格式
- 文本后处理规则
- 快速响应

### 3. 技术文档
- 同步 OCR 处理
- 支持 PDF 和 TIFF
- 高精度要求
- 完整的文本块保存

### 4. 普通文件存储
- 禁用 OCR 识别
- 仅进行文件存储
- 提高上传速度

## 注意事项

1. **性能考虑**：异步处理适合大文件或批量处理，同步处理适合小文件或需要立即结果的场景
2. **文件大小限制**：合理设置文件大小限制，避免处理过大的文件影响性能
3. **置信度设置**：根据文档质量调整置信度阈值，平衡识别准确性和覆盖率
4. **文本后处理**：根据业务需求配置文本处理规则，提高识别结果质量
5. **超时设置**：根据文件大小和处理复杂度设置合适的超时时间

## 集成示例

在创建分类模板时，将 WorkflowConfig 作为 JSON 字符串存储：

```csharp
var template = new AttachCatalogueTemplate(
    templateId: Guid.NewGuid(),
    templateName: "合同文档模板",
    attachReceiveType: AttachReceiveType.Required,
    sequenceNumber: 1,
    workflowConfig: JsonSerializer.Serialize(new WorkflowConfig
    {
        EnableOcr = true,
        OcrConfig = new OcrWorkflowConfig
        {
            EnableOcr = true,
            SupportedFileTypes = new List<string> { ".pdf", ".jpg", ".jpeg", ".png" },
            MinConfidence = 0.8,
            IsAsync = false,
            SaveTextBlocks = true
        }
    })
);
```
