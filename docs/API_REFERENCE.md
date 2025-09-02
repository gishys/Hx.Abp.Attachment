# API 参考文档

## 概述

本文档详细描述了 Hx.Abp.Attachment 智能档案管理系统的 API 接口，包括接口定义、请求参数、响应格式、错误码等信息。

## 基础信息

### 基础 URL

```
开发环境: https://localhost:5001
生产环境: https://api.attachment.example.com
```

### 认证方式

系统使用 JWT Token 进行身份认证，需要在请求头中包含 `Authorization` 字段：

```
Authorization: Bearer <your_jwt_token>
```

### 响应格式

所有 API 响应都使用统一的 JSON 格式：

```json
{
    "success": true,
    "data": {
        // 具体数据内容
    },
    "error": null,
    "timestamp": "2024-01-01T00:00:00Z"
}
```

### 错误响应

当请求失败时，响应格式如下：

```json
{
    "success": false,
    "data": null,
    "error": {
        "code": "ERROR_CODE",
        "message": "错误描述信息",
        "details": "详细错误信息"
    },
    "timestamp": "2024-01-01T00:00:00Z"
}
```

### 分页参数

支持分页的接口使用以下参数：

| 参数名         | 类型    | 必填 | 说明                           |
| -------------- | ------- | ---- | ------------------------------ |
| skipCount      | integer | 否   | 跳过的记录数，默认 0           |
| maxResultCount | integer | 否   | 每页记录数，默认 10，最大 1000 |

### 通用状态码

| 状态码 | 说明           |
| ------ | -------------- |
| 200    | 请求成功       |
| 400    | 请求参数错误   |
| 401    | 未授权访问     |
| 403    | 禁止访问       |
| 404    | 资源不存在     |
| 500    | 服务器内部错误 |

## 档案管理 API

### 1. 创建档案目录

**接口地址**: `POST /api/app/attachment/create`

**接口描述**: 创建新的档案目录

**请求参数**:

```json
{
    "catalogueName": "项目合同",
    "catalogueType": 1,
    "cataloguePurpose": 2,
    "reference": "PROJ-2024-001",
    "referenceType": 1,
    "parentId": null,
    "isRequired": true,
    "isStatic": false,
    "isVerification": false,
    "permissions": {
        "roles": ["Manager", "ProjectLead"],
        "actions": ["read", "write"]
    },
    "extraProperties": {
        "projectCode": "PRJ001",
        "contractValue": 1000000
    }
}
```

**参数说明**:

| 参数名           | 类型    | 必填 | 说明                                     |
| ---------------- | ------- | ---- | ---------------------------------------- |
| catalogueName    | string  | 是   | 目录名称，最大长度 255                   |
| catalogueType    | integer | 是   | 目录类型：1-项目，2-合同，3-报告，4-其他 |
| cataloguePurpose | integer | 是   | 目录用途：1-归档，2-审批，3-查阅，4-其他 |
| reference        | string  | 否   | 引用标识，最大长度 255                   |
| referenceType    | integer | 否   | 引用类型：1-项目，2-合同，3-客户，4-其他 |
| parentId         | string  | 否   | 父目录 ID，UUID 格式                     |
| isRequired       | boolean | 否   | 是否必需，默认 false                     |
| isStatic         | boolean | 否   | 是否静态，默认 false                     |
| isVerification   | boolean | 否   | 是否需要验证，默认 false                 |
| permissions      | object  | 否   | 权限配置                                 |
| extraProperties  | object  | 否   | 扩展属性                                 |

**响应示例**:

```json
{
    "success": true,
    "data": {
        "id": "550e8400-e29b-41d4-a716-446655440000",
        "catalogueName": "项目合同",
        "catalogueType": 1,
        "cataloguePurpose": 2,
        "reference": "PROJ-2024-001",
        "referenceType": 1,
        "parentId": null,
        "isRequired": true,
        "isStatic": false,
        "isVerification": false,
        "permissions": {
            "roles": ["Manager", "ProjectLead"],
            "actions": ["read", "write"]
        },
        "extraProperties": {
            "projectCode": "PRJ001",
            "contractValue": 1000000
        },
        "creationTime": "2024-01-01T00:00:00Z",
        "creatorId": "user-id",
        "lastModificationTime": "2024-01-01T00:00:00Z",
        "lastModifierId": "user-id"
    },
    "error": null,
    "timestamp": "2024-01-01T00:00:00Z"
}
```

### 2. 获取档案目录详情

**接口地址**: `GET /api/app/attachment/{id}`

**接口描述**: 根据 ID 获取档案目录的详细信息

**路径参数**:

| 参数名 | 类型   | 必填 | 说明                   |
| ------ | ------ | ---- | ---------------------- |
| id     | string | 是   | 档案目录 ID，UUID 格式 |

**响应示例**:

```json
{
    "success": true,
    "data": {
        "id": "550e8400-e29b-41d4-a716-446655440000",
        "catalogueName": "项目合同",
        "catalogueType": 1,
        "cataloguePurpose": 2,
        "reference": "PROJ-2024-001",
        "referenceType": 1,
        "parentId": null,
        "isRequired": true,
        "isStatic": false,
        "isVerification": false,
        "permissions": {
            "roles": ["Manager", "ProjectLead"],
            "actions": ["read", "write"]
        },
        "extraProperties": {
            "projectCode": "PRJ001",
            "contractValue": 1000000
        },
        "creationTime": "2024-01-01T00:00:00Z",
        "creatorId": "user-id",
        "lastModificationTime": "2024-01-01T00:00:00Z",
        "lastModifierId": "user-id",
        "isDeleted": false,
        "deletionTime": null,
        "deleterId": null
    },
    "error": null,
    "timestamp": "2024-01-01T00:00:00Z"
}
```

### 3. 更新档案目录

**接口地址**: `PUT /api/app/attachment/{id}`

**接口描述**: 更新指定档案目录的信息

**路径参数**:

| 参数名 | 类型   | 必填 | 说明                   |
| ------ | ------ | ---- | ---------------------- |
| id     | string | 是   | 档案目录 ID，UUID 格式 |

**请求参数**:

```json
{
    "catalogueName": "项目合同-更新",
    "catalogueType": 1,
    "cataloguePurpose": 2,
    "reference": "PROJ-2024-001-UPDATED",
    "referenceType": 1,
    "parentId": null,
    "isRequired": true,
    "isStatic": false,
    "isVerification": false,
    "permissions": {
        "roles": ["Manager", "ProjectLead", "TeamMember"],
        "actions": ["read", "write", "delete"]
    },
    "extraProperties": {
        "projectCode": "PRJ001",
        "contractValue": 1200000,
        "updateReason": "合同金额调整"
    }
}
```

**响应示例**:

```json
{
    "success": true,
    "data": {
        "id": "550e8400-e29b-41d4-a716-446655440000",
        "catalogueName": "项目合同-更新",
        "catalogueType": 1,
        "cataloguePurpose": 2,
        "reference": "PROJ-2024-001-UPDATED",
        "referenceType": 1,
        "parentId": null,
        "isRequired": true,
        "isStatic": false,
        "isVerification": false,
        "permissions": {
            "roles": ["Manager", "ProjectLead", "TeamMember"],
            "actions": ["read", "write", "delete"]
        },
        "extraProperties": {
            "projectCode": "PRJ001",
            "contractValue": 1200000,
            "updateReason": "合同金额调整"
        },
        "creationTime": "2024-01-01T00:00:00Z",
        "creatorId": "user-id",
        "lastModificationTime": "2024-01-01T12:00:00Z",
        "lastModifierId": "user-id",
        "isDeleted": false,
        "deletionTime": null,
        "deleterId": null
    },
    "error": null,
    "timestamp": "2024-01-01T12:00:00Z"
}
```

### 4. 删除档案目录

**接口地址**: `DELETE /api/app/attachment/{id}`

**接口描述**: 删除指定的档案目录（软删除）

**路径参数**:

| 参数名 | 类型   | 必填 | 说明                   |
| ------ | ------ | ---- | ---------------------- |
| id     | string | 是   | 档案目录 ID，UUID 格式 |

**响应示例**:

```json
{
    "success": true,
    "data": null,
    "error": null,
    "timestamp": "2024-01-01T12:00:00Z"
}
```

### 5. 获取档案目录列表

**接口地址**: `GET /api/app/attachment/list`

**接口描述**: 获取档案目录列表，支持分页和筛选

**查询参数**:

| 参数名            | 类型    | 必填 | 说明                           |
| ----------------- | ------- | ---- | ------------------------------ |
| skipCount         | integer | 否   | 跳过的记录数，默认 0           |
| maxResultCount    | integer | 否   | 每页记录数，默认 10，最大 1000 |
| catalogueName     | string  | 否   | 目录名称，支持模糊查询         |
| catalogueType     | integer | 否   | 目录类型筛选                   |
| cataloguePurpose  | integer | 否   | 目录用途筛选                   |
| reference         | string  | 否   | 引用标识筛选                   |
| referenceType     | integer | 否   | 引用类型筛选                   |
| parentId          | string  | 否   | 父目录 ID 筛选                 |
| isRequired        | boolean | 否   | 是否必需筛选                   |
| isStatic          | boolean | 否   | 是否静态筛选                   |
| isVerification    | boolean | 否   | 是否需要验证筛选               |
| creationTimeStart | string  | 否   | 创建时间开始，ISO 8601 格式    |
| creationTimeEnd   | string  | 否   | 创建时间结束，ISO 8601 格式    |

**响应示例**:

```json
{
    "success": true,
    "data": {
        "items": [
            {
                "id": "550e8400-e29b-41d4-a716-446655440000",
                "catalogueName": "项目合同",
                "catalogueType": 1,
                "cataloguePurpose": 2,
                "reference": "PROJ-2024-001",
                "referenceType": 1,
                "parentId": null,
                "isRequired": true,
                "isStatic": false,
                "isVerification": false,
                "creationTime": "2024-01-01T00:00:00Z",
                "creatorId": "user-id",
                "lastModificationTime": "2024-01-01T12:00:00Z",
                "lastModifierId": "user-id"
            }
        ],
        "totalCount": 1,
        "skipCount": 0,
        "maxResultCount": 10
    },
    "error": null,
    "timestamp": "2024-01-01T12:00:00Z"
}
```

### 6. 批量创建档案目录

**接口地址**: `POST /api/app/attachment/create-many`

**接口描述**: 批量创建多个档案目录

**请求参数**:

```json
{
    "inputs": [
        {
            "catalogueName": "项目合同1",
            "catalogueType": 1,
            "cataloguePurpose": 2,
            "reference": "PROJ-2024-001",
            "referenceType": 1
        },
        {
            "catalogueName": "项目合同2",
            "catalogueType": 1,
            "cataloguePurpose": 2,
            "reference": "PROJ-2024-002",
            "referenceType": 1
        }
    ],
    "createMode": 1
}
```

**参数说明**:

| 参数名     | 类型    | 必填 | 说明                                         |
| ---------- | ------- | ---- | -------------------------------------------- |
| inputs     | array   | 是   | 档案目录创建参数数组                         |
| createMode | integer | 否   | 创建模式：1-全部成功，2-部分成功，3-全部失败 |

**响应示例**:

```json
{
    "success": true,
    "data": {
        "results": [
            {
                "success": true,
                "data": {
                    "id": "550e8400-e29b-41d4-a716-446655440000",
                    "catalogueName": "项目合同1"
                },
                "error": null
            },
            {
                "success": true,
                "data": {
                    "id": "550e8400-e29b-41d4-a716-446655440001",
                    "catalogueName": "项目合同2"
                },
                "error": null
            }
        ],
        "totalCount": 2,
        "successCount": 2,
        "failureCount": 0
    },
    "error": null,
    "timestamp": "2024-01-01T12:00:00Z"
}
```

## 智能检索 API

### 1. 全文搜索

**接口地址**: `POST /api/app/attachment/search`

**接口描述**: 基于关键词进行全文搜索

**请求参数**:

```json
{
    "keyword": "项目合同",
    "skipCount": 0,
    "maxResultCount": 20,
    "catalogueType": null,
    "cataloguePurpose": null,
    "referenceType": null,
    "creationTimeStart": null,
    "creationTimeEnd": null,
    "sortBy": "relevance",
    "sortOrder": "desc"
}
```

**参数说明**:

| 参数名            | 类型    | 必填 | 说明                                             |
| ----------------- | ------- | ---- | ------------------------------------------------ |
| keyword           | string  | 是   | 搜索关键词                                       |
| skipCount         | integer | 否   | 跳过的记录数，默认 0                             |
| maxResultCount    | integer | 否   | 每页记录数，默认 20，最大 100                    |
| catalogueType     | integer | 否   | 目录类型筛选                                     |
| cataloguePurpose  | integer | 否   | 目录用途筛选                                     |
| referenceType     | integer | 否   | 引用类型筛选                                     |
| creationTimeStart | string  | 否   | 创建时间开始，ISO 8601 格式                      |
| creationTimeEnd   | string  | 否   | 创建时间结束，ISO 8601 格式                      |
| sortBy            | string  | 否   | 排序字段：relevance, creationTime, catalogueName |
| sortOrder         | string  | 否   | 排序方向：asc, desc                              |

**响应示例**:

```json
{
    "success": true,
    "data": {
        "items": [
            {
                "id": "550e8400-e29b-41d4-a716-446655440000",
                "catalogueName": "项目合同",
                "catalogueType": 1,
                "cataloguePurpose": 2,
                "reference": "PROJ-2024-001",
                "referenceType": 1,
                "relevanceScore": 0.95,
                "matchedKeywords": ["项目", "合同"],
                "summary": "这是一个关于项目合同的重要文档...",
                "creationTime": "2024-01-01T00:00:00Z"
            }
        ],
        "totalCount": 1,
        "skipCount": 0,
        "maxResultCount": 20,
        "searchTime": 150,
        "suggestions": ["项目合同模板", "合同审批流程"]
    },
    "error": null,
    "timestamp": "2024-01-01T12:00:00Z"
}
```

### 2. 语义搜索

**接口地址**: `POST /api/app/attachment/semantic-search`

**接口描述**: 基于语义理解进行智能搜索

**请求参数**:

```json
{
    "query": "去年签订的合同",
    "skipCount": 0,
    "maxResultCount": 20,
    "semanticThreshold": 0.7,
    "includeSynonyms": true,
    "includeRelated": true
}
```

**参数说明**:

| 参数名            | 类型    | 必填 | 说明                          |
| ----------------- | ------- | ---- | ----------------------------- |
| query             | string  | 是   | 自然语言查询                  |
| skipCount         | integer | 否   | 跳过的记录数，默认 0          |
| maxResultCount    | integer | 否   | 每页记录数，默认 20，最大 100 |
| semanticThreshold | number  | 否   | 语义相似度阈值，0-1，默认 0.7 |
| includeSynonyms   | boolean | 否   | 是否包含同义词，默认 true     |
| includeRelated    | boolean | 否   | 是否包含相关内容，默认 true   |

**响应示例**:

```json
{
    "success": true,
    "data": {
        "items": [
            {
                "id": "550e8400-e29b-41d4-a716-446655440000",
                "catalogueName": "2023年项目合同",
                "catalogueType": 1,
                "cataloguePurpose": 2,
                "reference": "PROJ-2023-001",
                "referenceType": 1,
                "semanticScore": 0.88,
                "semanticExplanation": "查询'去年签订的合同'匹配到'2023年项目合同'，时间语义匹配",
                "relatedConcepts": ["合同", "项目", "2023年"],
                "creationTime": "2023-06-01T00:00:00Z"
            }
        ],
        "totalCount": 1,
        "skipCount": 0,
        "maxResultCount": 20,
        "searchTime": 250,
        "semanticAnalysis": {
            "extractedEntities": ["去年", "合同"],
            "extractedTime": "2023年",
            "extractedType": "合同"
        }
    },
    "error": null,
    "timestamp": "2024-01-01T12:00:00Z"
}
```

### 3. 获取搜索建议

**接口地址**: `GET /api/app/attachment/search-suggestions`

**接口描述**: 获取搜索关键词的建议和自动补全

**查询参数**:

| 参数名   | 类型    | 必填 | 说明                  |
| -------- | ------- | ---- | --------------------- |
| keyword  | string  | 是   | 输入的关键词          |
| maxCount | integer | 否   | 最大建议数量，默认 10 |

**响应示例**:

```json
{
    "success": true,
    "data": {
        "suggestions": [
            {
                "text": "项目合同",
                "type": "exact",
                "frequency": 15
            },
            {
                "text": "项目合同模板",
                "type": "related",
                "frequency": 8
            },
            {
                "text": "合同审批流程",
                "type": "related",
                "frequency": 6
            }
        ],
        "popularSearches": ["项目合同", "技术文档", "会议纪要"],
        "trendingTopics": ["数字化转型", "项目管理", "合同管理"]
    },
    "error": null,
    "timestamp": "2024-01-01T12:00:00Z"
}
```

## AI 分类 API

### 1. 智能分类推荐

**接口地址**: `POST /api/app/attachment/classify`

**接口描述**: 基于文档内容进行智能分类推荐

**请求参数**:

```json
{
    "content": "这是一份关于项目合同的重要文档，包含了合同条款、付款方式、违约责任等关键信息。",
    "categories": ["合同协议", "项目管理", "财务文档", "法律文档"],
    "maxRecommendations": 3,
    "includeConfidence": true,
    "includeExplanation": true
}
```

**参数说明**:

| 参数名             | 类型    | 必填 | 说明                                 |
| ------------------ | ------- | ---- | ------------------------------------ |
| content            | string  | 是   | 文档内容                             |
| categories         | array   | 否   | 候选分类列表，为空时使用系统默认分类 |
| maxRecommendations | integer | 否   | 最大推荐数量，默认 3                 |
| includeConfidence  | boolean | 否   | 是否包含置信度，默认 true            |
| includeExplanation | true    | 否   | 是否包含解释说明，默认 true          |

**响应示例**:

```json
{
    "success": true,
    "data": {
        "recommendations": [
            {
                "category": "合同协议",
                "confidence": 0.92,
                "explanation": "文档内容包含'合同条款'、'付款方式'、'违约责任'等合同相关词汇，与'合同协议'分类高度匹配",
                "matchedKeywords": ["合同条款", "付款方式", "违约责任"],
                "categoryDescription": "包含各种类型的合同和协议文档"
            },
            {
                "category": "法律文档",
                "confidence": 0.78,
                "explanation": "文档涉及'违约责任'等法律概念，与'法律文档'分类有一定相关性",
                "matchedKeywords": ["违约责任"],
                "categoryDescription": "涉及法律事务和合规要求的文档"
            },
            {
                "category": "项目管理",
                "confidence": 0.65,
                "explanation": "文档提到'项目'，与'项目管理'分类有基本相关性",
                "matchedKeywords": ["项目"],
                "categoryDescription": "项目相关的管理和执行文档"
            }
        ],
        "processingTime": 1200,
        "modelVersion": "v1.2.0",
        "contentAnalysis": {
            "wordCount": 45,
            "keyPhrases": ["项目合同", "合同条款", "付款方式", "违约责任"],
            "language": "zh-CN",
            "sentiment": "neutral"
        }
    },
    "error": null,
    "timestamp": "2024-01-01T12:00:00Z"
}
```

### 2. 批量分类推荐

**接口地址**: `POST /api/app/attachment/batch-classify`

**接口描述**: 批量处理多个文档的分类推荐

**请求参数**:

```json
{
    "documents": [
        {
            "id": "doc-001",
            "content": "项目合同文档内容...",
            "title": "项目合同"
        },
        {
            "id": "doc-002",
            "content": "技术报告文档内容...",
            "title": "技术报告"
        }
    ],
    "categories": ["合同协议", "技术文档", "项目管理"],
    "maxRecommendations": 2,
    "includeConfidence": true
}
```

**参数说明**:

| 参数名             | 类型    | 必填 | 说明                                      |
| ------------------ | ------- | ---- | ----------------------------------------- |
| documents          | array   | 是   | 文档数组，每个文档包含 id、content、title |
| categories         | array   | 否   | 候选分类列表                              |
| maxRecommendations | integer | 否   | 每个文档的最大推荐数量，默认 2            |
| includeConfidence  | boolean | 否   | 是否包含置信度，默认 true                 |

**响应示例**:

```json
{
    "success": true,
    "data": {
        "results": [
            {
                "documentId": "doc-001",
                "title": "项目合同",
                "recommendations": [
                    {
                        "category": "合同协议",
                        "confidence": 0.92
                    },
                    {
                        "category": "项目管理",
                        "confidence": 0.78
                    }
                ],
                "processingTime": 800
            },
            {
                "documentId": "doc-002",
                "title": "技术报告",
                "recommendations": [
                    {
                        "category": "技术文档",
                        "confidence": 0.95
                    },
                    {
                        "category": "项目管理",
                        "confidence": 0.82
                    }
                ],
                "processingTime": 750
            }
        ],
        "totalDocuments": 2,
        "totalProcessingTime": 1550,
        "averageProcessingTime": 775,
        "successCount": 2,
        "failureCount": 0
    },
    "error": null,
    "timestamp": "2024-01-01T12:00:00Z"
}
```

### 3. 分类置信度评估

**接口地址**: `POST /api/app/attachment/classification-confidence`

**接口描述**: 评估文档与指定分类的匹配置信度

**请求参数**:

```json
{
    "content": "这是一份关于项目合同的重要文档...",
    "category": "合同协议",
    "includeDetailedAnalysis": true
}
```

**参数说明**:

| 参数名                  | 类型    | 必填 | 说明                         |
| ----------------------- | ------- | ---- | ---------------------------- |
| content                 | string  | 是   | 文档内容                     |
| category                | string  | 是   | 要评估的分类                 |
| includeDetailedAnalysis | boolean | 否   | 是否包含详细分析，默认 false |

**响应示例**:

```json
{
    "success": true,
    "data": {
        "category": "合同协议",
        "confidence": 0.92,
        "confidenceLevel": "high",
        "detailedAnalysis": {
            "keywordMatch": {
                "score": 0.85,
                "matchedKeywords": ["合同", "条款", "付款", "违约"],
                "totalKeywords": 5
            },
            "semanticSimilarity": {
                "score": 0.92,
                "explanation": "文档内容与合同协议分类在语义上高度相似"
            },
            "contextRelevance": {
                "score": 0.88,
                "explanation": "文档上下文与合同协议场景高度相关"
            }
        },
        "recommendations": [
            "可以添加更多合同相关的专业术语",
            "建议包含合同编号和签署日期信息"
        ],
        "processingTime": 650
    },
    "error": null,
    "timestamp": "2024-01-01T12:00:00Z"
}
```

## 文档分析 API

### 1. 文档内容分析

**接口地址**: `POST /api/app/attachment/analyze-document`

**接口描述**: 对文档内容进行深度分析，提取关键信息

**请求参数**:

```json
{
    "content": "这是一份关于项目合同的重要文档...",
    "analysisTypes": ["keywords", "entities", "summary", "sentiment"],
    "language": "zh-CN",
    "includeMetadata": true
}
```

**参数说明**:

| 参数名          | 类型    | 必填 | 说明                                             |
| --------------- | ------- | ---- | ------------------------------------------------ |
| content         | string  | 是   | 文档内容                                         |
| analysisTypes   | array   | 否   | 分析类型：keywords, entities, summary, sentiment |
| language        | string  | 否   | 文档语言，默认自动检测                           |
| includeMetadata | boolean | 否   | 是否包含元数据，默认 false                       |

**响应示例**:

```json
{
    "success": true,
    "data": {
        "documentInfo": {
            "wordCount": 150,
            "characterCount": 300,
            "language": "zh-CN",
            "readingTime": "1分钟"
        },
        "keywords": [
            {
                "text": "项目合同",
                "weight": 0.95,
                "frequency": 3
            },
            {
                "text": "合同条款",
                "weight": 0.88,
                "frequency": 2
            }
        ],
        "entities": [
            {
                "text": "张三",
                "type": "PERSON",
                "confidence": 0.92
            },
            {
                "text": "ABC公司",
                "type": "ORGANIZATION",
                "confidence": 0.95
            }
        ],
        "summary": "这是一份关于项目合同的重要文档，主要涉及合同条款、付款方式、违约责任等关键内容。文档明确了甲乙双方的权利义务，为项目顺利执行提供了法律保障。",
        "sentiment": {
            "overall": "neutral",
            "score": 0.05,
            "details": {
                "positive": 0.3,
                "negative": 0.25,
                "neutral": 0.45
            }
        },
        "processingTime": 1800,
        "modelVersion": "v1.2.0"
    },
    "error": null,
    "timestamp": "2024-01-01T12:00:00Z"
}
```

### 2. 关键词提取

**接口地址**: `POST /api/app/attachment/extract-keywords`

**接口描述**: 从文档中提取关键词

**请求参数**:

```json
{
    "content": "这是一份关于项目合同的重要文档...",
    "maxCount": 10,
    "minLength": 2,
    "algorithm": "tfidf",
    "includeWeight": true
}
```

**参数说明**:

| 参数名        | 类型    | 必填 | 说明                                    |
| ------------- | ------- | ---- | --------------------------------------- |
| content       | string  | 是   | 文档内容                                |
| maxCount      | integer | 否   | 最大关键词数量，默认 10                 |
| minLength     | integer | 否   | 最小关键词长度，默认 2                  |
| algorithm     | string  | 否   | 算法：tfidf, textrank, bert，默认 tfidf |
| includeWeight | boolean | 否   | 是否包含权重，默认 true                 |

**响应示例**:

```json
{
    "success": true,
    "data": {
        "keywords": [
            {
                "text": "项目合同",
                "weight": 0.95,
                "frequency": 3,
                "position": [0, 15, 45]
            },
            {
                "text": "合同条款",
                "weight": 0.88,
                "frequency": 2,
                "position": [25, 67]
            }
        ],
        "algorithm": "tfidf",
        "processingTime": 450,
        "statistics": {
            "totalWords": 150,
            "uniqueWords": 89,
            "averageWeight": 0.67
        }
    },
    "error": null,
    "timestamp": "2024-01-01T12:00:00Z"
}
```

### 3. 文档摘要生成

**接口地址**: `POST /api/app/attachment/generate-summary`

**接口描述**: 为文档生成摘要

**请求参数**:

```json
{
    "content": "这是一份关于项目合同的重要文档...",
    "maxLength": 200,
    "summaryType": "extractive",
    "includeKeywords": true
}
```

**参数说明**:

| 参数名                 | 类型    | 必填 | 说明                                               |
| ---------------------- | ------- | ---- | -------------------------------------------------- |
| content                | string  | 是   | 文档内容                                           |
| maxLength              | integer | 否   | 最大摘要长度，默认 200                             |
| summaryType            | string  | 否   | 摘要类型：extractive, abstractive，默认 extractive |
| includeKeywords": true | boolean | 否   | 是否包含关键词，默认 true                          |

**响应示例**:

```json
{
    "success": true,
    "data": {
        "summary": "这是一份关于项目合同的重要文档，主要涉及合同条款、付款方式、违约责任等关键内容。文档明确了甲乙双方的权利义务，为项目顺利执行提供了法律保障。",
        "summaryType": "extractive",
        "length": 89,
        "compressionRatio": 0.59,
        "keywords": ["项目合同", "合同条款", "付款方式", "违约责任"],
        "processingTime": 1200,
        "modelVersion": "v1.2.0"
    },
    "error": null,
    "timestamp": "2024-01-01T12:00:00Z"
}
```

## 错误码参考

### 通用错误码

| 错误码           | 说明           | HTTP 状态码 |
| ---------------- | -------------- | ----------- |
| VALIDATION_ERROR | 参数验证失败   | 400         |
| UNAUTHORIZED     | 未授权访问     | 401         |
| FORBIDDEN        | 禁止访问       | 403         |
| NOT_FOUND        | 资源不存在     | 404         |
| INTERNAL_ERROR   | 服务器内部错误 | 500         |

### 业务错误码

| 错误码                                 | 说明                 | HTTP 状态码 |
| -------------------------------------- | -------------------- | ----------- |
| ATTACHMENT_CATALOGUE_NOT_FOUND         | 档案目录不存在       | 404         |
| ATTACHMENT_CATALOGUE_NAME_EXISTS       | 档案目录名称已存在   | 400         |
| ATTACHMENT_CATALOGUE_INVALID_TYPE      | 档案目录类型无效     | 400         |
| ATTACHMENT_CATALOGUE_PERMISSION_DENIED | 权限不足             | 403         |
| ATTACHMENT_CATALOGUE_DELETE_FAILED     | 删除失败，存在子目录 | 400         |
| SEARCH_KEYWORD_TOO_SHORT               | 搜索关键词太短       | 400         |
| SEARCH_RESULT_TOO_LARGE                | 搜索结果过大         | 400         |
| AI_SERVICE_UNAVAILABLE                 | AI 服务不可用        | 503         |
| AI_SERVICE_TIMEOUT                     | AI 服务超时          | 504         |
| AI_SERVICE_QUOTA_EXCEEDED              | AI 服务配额超限      | 429         |

## 限流策略

### API 限流规则

| API 类型 | 限流规则     | 说明                   |
| -------- | ------------ | ---------------------- |
| 普通查询 | 1000 次/分钟 | 基础的查询操作         |
| 全文搜索 | 500 次/分钟  | 计算密集的搜索操作     |
| AI 分析  | 100 次/分钟  | 资源消耗较大的 AI 操作 |
| 批量操作 | 50 次/分钟   | 批量处理操作           |

### 限流响应

当请求超过限流阈值时，系统会返回以下响应：

```json
{
    "success": false,
    "data": null,
    "error": {
        "code": "RATE_LIMIT_EXCEEDED",
        "message": "请求频率超限，请稍后重试",
        "details": "当前限流：100次/分钟，已使用：100次"
    },
    "timestamp": "2024-01-01T12:00:00Z"
}
```

## 版本管理

### API 版本策略

系统使用 URL 路径进行版本管理：

```
/api/v1/app/attachment/...  # 当前版本
/api/v2/app/attachment/...  # 未来版本
```

### 版本兼容性

-   新版本保持向后兼容
-   废弃的 API 会提前通知
-   重大变更会通过新版本提供

## 总结

本文档详细描述了 Hx.Abp.Attachment 系统的 API 接口，包括档案管理、智能检索、AI 分类、文档分析等核心功能。通过统一的接口规范和错误处理机制，为开发者提供了清晰的使用指南。

建议开发者在集成 API 时：

1. **仔细阅读接口文档**，了解参数要求和响应格式
2. **实现适当的错误处理**，处理各种异常情况
3. **遵循限流策略**，避免请求频率过高
4. **使用 HTTPS 协议**，确保数据传输安全
5. **实现重试机制**，提高系统可靠性

如有疑问或需要技术支持，请联系开发团队。
