# 附件目录模板接口文档

## 概述

本文档详细描述了附件目录模板相关的 API 接口，包括接口说明、参数详解、应用场景和调用示例。

## 接口列表

### 1. 创建分类模板接口

#### 接口信息

-   **接口路径**: `POST /api/attach-catalogue-template`
-   **接口描述**: 创建新的附件目录模板
-   **请求方式**: POST
-   **Content-Type**: application/json

#### 请求参数

**请求体**: `CreateUpdateAttachCatalogueTemplateDto`

| 参数名            | 类型                                   | 必填 | 描述                  | 示例值                                               |
| ----------------- | -------------------------------------- | ---- | --------------------- | ---------------------------------------------------- |
| name              | string                                 | 是   | 模板名称              | "合同文档模板"                                       |
| description       | string                                 | 否   | 模板描述              | "用于存储各类合同文档的模板"                         |
| tags              | string[]                               | 否   | 标签数组              | ["合同", "法律", "重要"]                             |
| attachReceiveType | AttachReceiveType                      | 是   | 附件接收类型          | 2                                                    |
| workflowConfig    | string                                 | 否   | 工作流配置(JSON 格式) | `{"workflowKey":"contract_approval","timeout":3600}` |
| isRequired        | boolean                                | 是   | 是否必填              | true                                                 |
| sequenceNumber    | int                                    | 是   | 排序号                | 100                                                  |
| isStatic          | boolean                                | 是   | 是否静态模板          | false                                                |
| parentId          | Guid?                                  | 否   | 父模板 ID             | "3fa85f64-5717-4562-b3fc-2c963f66afa6"               |
| templatePath      | string                                 | 否   | 模板路径              | "00001.00002"                                        |
| facetType         | FacetType                              | 是   | 分面类型              | 0                                                    |
| templatePurpose   | TemplatePurpose                        | 是   | 模板用途              | 1                                                    |
| textVector        | double[]                               | 否   | 文本向量              | null                                                 |
| permissions       | AttachCatalogueTemplatePermissionDto[] | 否   | 权限配置              | []                                                   |
| metaFields        | CreateUpdateMetaFieldDto[]             | 否   | 元数据字段            | []                                                   |

#### 复杂类型说明

**AttachCatalogueTemplatePermissionDto**:
| 字段名 | 类型 | 必填 | 说明 | 示例值 |
|------------|----------------|------|--------------|---------------------------|
| id | Guid? | 否 | 权限 ID | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |
| roleName | string | 是 | 角色名称 | "Admin" |
| action | PermissionAction | 是 | 权限动作 | 1 |
| effect | PermissionEffect | 是 | 权限效果 | 0 |

**CreateUpdateMetaFieldDto**:
| 字段名 | 类型 | 必填 | 说明 | 示例值 |
|--------------|--------|------|--------------|---------------------------|
| id | Guid? | 否 | 字段 ID | "3fa85f64-5717-4562-b3fc-2c963f66afa8" |
| fieldName | string | 是 | 字段名称 | "身份证号" |
| fieldType | string | 是 | 字段类型 | "string" |
| isRequired | bool | 是 | 是否必填 | true |
| defaultValue | string | 否 | 默认值 | "" |
| description | string | 否 | 字段描述 | "身份证号码" |

#### 枚举值说明

**AttachReceiveType**:

-   1: 原件
-   2: 复印件
-   3: 原件副本
-   4: 副本复印件
-   5: 手稿
-   6: 原件或复印件
-   99: 其它

**FacetType**:

-   0: 通用分面
-   1: 组织维度
-   2: 项目类型
-   3: 阶段分面
-   4: 专业领域
-   5: 文档类型
-   6: 时间切片
-   99: 业务自定义

**TemplatePurpose**:

-   1: 分类管理
-   2: 文档管理
-   3: 流程管理
-   4: 权限管理
-   99: 其他用途

**PermissionAction**:

-   0: 查看
-   1: 创建
-   2: 编辑
-   3: 删除
-   4: 下载
-   5: 上传

**PermissionEffect**:

-   0: 允许
-   1: 拒绝

#### 响应结果

**成功响应** (200 OK):

```json
{
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "合同文档模板",
    "description": "用于存储各类合同文档的模板",
    "tags": ["合同", "法律", "重要"],
    "attachReceiveType": 2,
    "workflowConfig": "{\"workflowKey\":\"contract_approval\",\"timeout\":3600}",
    "isRequired": true,
    "sequenceNumber": 100,
    "isStatic": false,
    "parentId": null,
    "templatePath": "00001",
    "facetType": 0,
    "templatePurpose": 1,
    "textVector": null,
    "permissions": [
        {
            "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
            "roleName": "Admin",
            "action": 1,
            "effect": 0
        },
        {
            "id": null,
            "roleName": "User",
            "action": 0,
            "effect": 0
        }
    ],
    "metaFields": [
        {
            "id": "3fa85f64-5717-4562-b3fc-2c963f66afa8",
            "fieldName": "身份证号",
            "fieldType": "string",
            "isRequired": true,
            "defaultValue": "",
            "description": "身份证号码"
        },
        {
            "id": "3fa85f64-5717-4562-b3fc-2c963f66afa9",
            "fieldName": "申请日期",
            "fieldType": "datetime",
            "isRequired": false,
            "defaultValue": "",
            "description": "申请提交日期"
        }
    ],
    "creationTime": "2024-12-19T10:00:00Z",
    "lastModificationTime": null,
    "isDeleted": false,
    "isLatest": true
}
```

#### React Axios 调用示例

```javascript
import axios from 'axios';

const createTemplate = async (templateData) => {
    try {
        const response = await axios.post(
            '/api/attach-catalogue-template',
            {
                name: '合同文档模板',
                description: '用于存储各类合同文档的模板',
                tags: ['合同', '法律', '重要'],
                attachReceiveType: 2, // 复印件
                workflowConfig: JSON.stringify({
                    workflowKey: 'contract_approval',
                    timeout: 3600,
                    skipApprovers: ['admin'],
                    scripts: ['validate_contract.js'],
                    webhooks: ['https://api.company.com/contract-notify'],
                }),
                isRequired: true,
                sequenceNumber: 100,
                isStatic: false,
                facetType: 0, // 通用分面
                templatePurpose: 1, // 分类管理
                permissions: [
                    {
                        id: null,
                        roleName: 'Admin',
                        action: 1, // 创建
                        effect: 0, // 允许
                    },
                    {
                        id: null,
                        roleName: 'User',
                        action: 0, // 查看
                        effect: 0, // 允许
                    },
                ],
                metaFields: [
                    {
                        id: null,
                        fieldName: '身份证号',
                        fieldType: 'string',
                        isRequired: true,
                        defaultValue: '',
                        description: '身份证号码',
                    },
                    {
                        id: null,
                        fieldName: '申请日期',
                        fieldType: 'datetime',
                        isRequired: false,
                        defaultValue: '',
                        description: '申请提交日期',
                    },
                ],
            },
            {
                headers: {
                    'Content-Type': 'application/json',
                    Authorization: `Bearer ${token}`,
                },
            }
        );

        console.log('模板创建成功:', response.data);
        return response.data;
    } catch (error) {
        console.error('模板创建失败:', error.response?.data || error.message);
        throw error;
    }
};

// 使用示例
createTemplate();
```

---

### 2. 获取分类模板列表接口

#### 接口信息

-   **接口路径**: `GET /api/attach-catalogue-template`
-   **接口描述**: 获取分类模板列表，支持分页和过滤
-   **请求方式**: GET

#### 请求参数

**查询参数**:

| 参数名             | 类型              | 必填 | 描述                 | 示例值                                 |
| ------------------ | ----------------- | ---- | -------------------- | -------------------------------------- |
| name               | string            | 否   | 模板名称（模糊查询） | "合同"                                 |
| attachReceiveType  | AttachReceiveType | 否   | 附件类型过滤         | 2                                      |
| facetType          | FacetType         | 否   | 分面类型过滤         | 0                                      |
| templatePurpose    | TemplatePurpose   | 否   | 模板用途过滤         | 1                                      |
| isRequired         | boolean           | 否   | 是否必收过滤         | true                                   |
| isStatic           | boolean           | 否   | 是否静态过滤         | false                                  |
| isLatest           | boolean           | 否   | 是否最新版本过滤     | true                                   |
| parentId           | Guid              | 否   | 父模板 ID 过滤       | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |
| hasVector          | boolean           | 否   | 是否包含向量过滤     | true                                   |
| minVectorDimension | int               | 否   | 向量维度最小值       | 64                                     |
| maxVectorDimension | int               | 否   | 向量维度最大值       | 2048                                   |
| skipCount          | int               | 否   | 跳过的记录数         | 0                                      |
| maxResultCount     | int               | 否   | 最大返回记录数       | 10                                     |
| sorting            | string            | 否   | 排序字段             | "name"                                 |

#### React Axios 调用示例

```javascript
const getTemplateList = async (params = {}) => {
    try {
        const response = await axios.get('/api/attach-catalogue-template', {
            params: {
                name: '合同',
                facetType: 0,
                templatePurpose: 1,
                isLatest: true,
                skipCount: 0,
                maxResultCount: 20,
                sorting: 'name',
            },
            headers: {
                Authorization: `Bearer ${token}`,
            },
        });

        console.log('获取模板列表成功:', response.data);
        return response.data;
    } catch (error) {
        console.error(
            '获取模板列表失败:',
            error.response?.data || error.message
        );
        throw error;
    }
};
```

---

### 3. 根据 ID 获取分类模板接口

#### 接口信息

-   **接口路径**: `GET /api/attach-catalogue-template/{id}`
-   **接口描述**: 根据 ID 获取单个分类模板详情
-   **请求方式**: GET

#### 请求参数

**路径参数**:

| 参数名 | 类型 | 必填 | 描述    | 示例值                                 |
| ------ | ---- | ---- | ------- | -------------------------------------- |
| id     | Guid | 是   | 模板 ID | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |

#### React Axios 调用示例

```javascript
const getTemplateById = async (templateId) => {
    try {
        const response = await axios.get(
            `/api/attach-catalogue-template/${templateId}`,
            {
                headers: {
                    Authorization: `Bearer ${token}`,
                },
            }
        );

        console.log('获取模板详情成功:', response.data);
        return response.data;
    } catch (error) {
        console.error(
            '获取模板详情失败:',
            error.response?.data || error.message
        );
        throw error;
    }
};
```

---

### 4. 更新分类模板接口

#### 接口信息

-   **接口路径**: `PUT /api/attach-catalogue-template/{id}`
-   **接口描述**: 更新指定的分类模板
-   **请求方式**: PUT
-   **Content-Type**: application/json

#### 请求参数

**路径参数**:

| 参数名 | 类型 | 必填 | 描述    | 示例值                                 |
| ------ | ---- | ---- | ------- | -------------------------------------- |
| id     | Guid | 是   | 模板 ID | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |

**请求体**: `CreateUpdateAttachCatalogueTemplateDto` (同创建接口)

#### React Axios 调用示例

```javascript
const updateTemplate = async (templateId, updateData) => {
    try {
        const response = await axios.put(
            `/api/attach-catalogue-template/${templateId}`,
            {
                name: '更新后的合同文档模板',
                description: '更新后的描述',
                tags: ['合同', '法律', '重要', '更新'],
                attachReceiveType: 2,
                isRequired: true,
                sequenceNumber: 100,
                isStatic: false,
                facetType: 0,
                templatePurpose: 1,
            },
            {
                headers: {
                    'Content-Type': 'application/json',
                    Authorization: `Bearer ${token}`,
                },
            }
        );

        console.log('模板更新成功:', response.data);
        return response.data;
    } catch (error) {
        console.error('模板更新失败:', error.response?.data || error.message);
        throw error;
    }
};
```

---

### 5. 删除分类模板接口

#### 接口信息

-   **接口路径**: `DELETE /api/attach-catalogue-template/{id}`
-   **接口描述**: 删除指定的分类模板
-   **请求方式**: DELETE

#### 请求参数

**路径参数**:

| 参数名 | 类型 | 必填 | 描述    | 示例值                                 |
| ------ | ---- | ---- | ------- | -------------------------------------- |
| id     | Guid | 是   | 模板 ID | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |

#### React Axios 调用示例

```javascript
const deleteTemplate = async (templateId) => {
    try {
        await axios.delete(`/api/attach-catalogue-template/${templateId}`, {
            headers: {
                Authorization: `Bearer ${token}`,
            },
        });

        console.log('模板删除成功');
    } catch (error) {
        console.error('模板删除失败:', error.response?.data || error.message);
        throw error;
    }
};
```

---

### 6. 混合检索模板接口

#### 接口信息

-   **接口路径**: `POST /api/attach-catalogue-template/search/hybrid`
-   **接口描述**: 混合检索模板（字面 + 语义）
-   **请求方式**: POST
-   **Content-Type**: application/json

#### 请求参数

**请求体**: `TemplateSearchInputDto`

| 参数名              | 类型                | 必填 | 描述                   | 示例值           |
| ------------------- | ------------------- | ---- | ---------------------- | ---------------- |
| keyword             | string              | 否   | 搜索关键词（字面检索） | "合同"           |
| semanticQuery       | string              | 否   | 语义查询（向量检索）   | "合同文档"       |
| facetType           | FacetType           | 否   | 分面类型过滤           | 0                |
| templatePurpose     | TemplatePurpose     | 否   | 模板用途过滤           | 1                |
| tags                | string[]            | 否   | 标签过滤               | ["合同", "法律"] |
| onlyLatest          | boolean             | 否   | 是否只搜索最新版本     | true             |
| maxResults          | int                 | 否   | 最大返回结果数         | 20               |
| similarityThreshold | double              | 否   | 向量相似度阈值         | 0.7              |
| weights             | HybridSearchWeights | 否   | 混合检索权重配置       | 见下方           |

**HybridSearchWeights**:

| 参数名         | 类型   | 必填 | 描述         | 示例值 |
| -------------- | ------ | ---- | ------------ | ------ |
| textWeight     | double | 否   | 字面检索权重 | 0.4    |
| semanticWeight | double | 否   | 语义检索权重 | 0.6    |
| tagWeight      | double | 否   | 标签匹配权重 | 0.3    |
| nameWeight     | double | 否   | 名称匹配权重 | 0.5    |

#### 响应结果

**成功响应** (200 OK):

```json
{
    "items": [
        {
            "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
            "name": "合同文档模板",
            "description": "用于存储各类合同文档的模板",
            "tags": ["合同", "法律", "重要"],
            "facetType": 0,
            "templatePurpose": 1,
            "totalScore": 0.95,
            "textScore": 0.8,
            "semanticScore": 0.9,
            "tagScore": 0.7,
            "matchReasons": ["关键词匹配", "语义相似"],
            "isLatest": true,
            "version": 1
        }
    ]
}
```

#### React Axios 调用示例

```javascript
const searchTemplatesHybrid = async (searchParams) => {
    try {
        const response = await axios.post(
            '/api/attach-catalogue-template/search/hybrid',
            {
                keyword: '合同',
                semanticQuery: '合同文档',
                facetType: 0,
                templatePurpose: 1,
                tags: ['合同', '法律'],
                onlyLatest: true,
                maxResults: 20,
                similarityThreshold: 0.7,
                weights: {
                    textWeight: 0.4,
                    semanticWeight: 0.6,
                    tagWeight: 0.3,
                    nameWeight: 0.5,
                },
            },
            {
                headers: {
                    'Content-Type': 'application/json',
                    Authorization: `Bearer ${token}`,
                },
            }
        );

        console.log('混合检索成功:', response.data);
        return response.data;
    } catch (error) {
        console.error('混合检索失败:', error.response?.data || error.message);
        throw error;
    }
};
```

---

### 7. 文本检索模板接口

#### 接口信息

-   **接口路径**: `GET /api/attach-catalogue-template/search/text`
-   **接口描述**: 文本检索模板
-   **请求方式**: GET

#### 请求参数

**查询参数**:

| 参数名          | 类型            | 必填 | 描述           | 示例值   |
| --------------- | --------------- | ---- | -------------- | -------- |
| keyword         | string          | 是   | 搜索关键词     | "合同"   |
| facetType       | FacetType       | 否   | 分面类型过滤   | 0        |
| templatePurpose | TemplatePurpose | 否   | 模板用途过滤   | 1        |
| tags            | string[]        | 否   | 标签过滤       | ["合同"] |
| maxResults      | int             | 否   | 最大返回结果数 | 20       |

#### React Axios 调用示例

```javascript
const searchTemplatesByText = async (keyword, filters = {}) => {
    try {
        const response = await axios.get(
            '/api/attach-catalogue-template/search/text',
            {
                params: {
                    keyword,
                    facetType: filters.facetType,
                    templatePurpose: filters.templatePurpose,
                    tags: filters.tags,
                    maxResults: filters.maxResults || 20,
                },
                headers: {
                    Authorization: `Bearer ${token}`,
                },
            }
        );

        console.log('文本检索成功:', response.data);
        return response.data;
    } catch (error) {
        console.error('文本检索失败:', error.response?.data || error.message);
        throw error;
    }
};
```

---

### 8. 语义检索模板接口

#### 接口信息

-   **接口路径**: `GET /api/attach-catalogue-template/search/semantic`
-   **接口描述**: 语义检索模板
-   **请求方式**: GET

#### 请求参数

**查询参数**:

| 参数名              | 类型            | 必填 | 描述           | 示例值     |
| ------------------- | --------------- | ---- | -------------- | ---------- |
| semanticQuery       | string          | 是   | 语义查询       | "合同文档" |
| facetType           | FacetType       | 否   | 分面类型过滤   | 0          |
| templatePurpose     | TemplatePurpose | 否   | 模板用途过滤   | 1          |
| similarityThreshold | double          | 否   | 相似度阈值     | 0.7        |
| maxResults          | int             | 否   | 最大返回结果数 | 20         |

#### React Axios 调用示例

```javascript
const searchTemplatesBySemantic = async (semanticQuery, filters = {}) => {
    try {
        const response = await axios.get(
            '/api/attach-catalogue-template/search/semantic',
            {
                params: {
                    semanticQuery,
                    facetType: filters.facetType,
                    templatePurpose: filters.templatePurpose,
                    similarityThreshold: filters.similarityThreshold || 0.7,
                    maxResults: filters.maxResults || 20,
                },
                headers: {
                    Authorization: `Bearer ${token}`,
                },
            }
        );

        console.log('语义检索成功:', response.data);
        return response.data;
    } catch (error) {
        console.error('语义检索失败:', error.response?.data || error.message);
        throw error;
    }
};
```

---

### 9. 获取根节点模板接口

#### 接口信息

-   **接口路径**: `GET /api/attach-catalogue-template/tree/roots`
-   **接口描述**: 获取根节点模板列表，用于树状展示
-   **请求方式**: GET

#### 请求参数

**查询参数**:

| 参数名          | 类型            | 必填 | 描述               | 示例值 |
| --------------- | --------------- | ---- | ------------------ | ------ |
| facetType       | FacetType       | 否   | 分面类型过滤       | 0      |
| templatePurpose | TemplatePurpose | 否   | 模板用途过滤       | 1      |
| includeChildren | boolean         | 否   | 是否包含子节点     | true   |
| onlyLatest      | boolean         | 否   | 是否只返回最新版本 | true   |

#### React Axios 调用示例

```javascript
const getRootTemplates = async (params = {}) => {
    try {
        const response = await axios.get(
            '/api/attach-catalogue-template/tree/roots',
            {
                params: {
                    facetType: params.facetType,
                    templatePurpose: params.templatePurpose,
                    includeChildren: params.includeChildren || true,
                    onlyLatest: params.onlyLatest !== false,
                },
                headers: {
                    Authorization: `Bearer ${token}`,
                },
            }
        );

        console.log('获取根级模板成功:', response.data);
        return response.data;
    } catch (error) {
        console.error(
            '获取根级模板失败:',
            error.response?.data || error.message
        );
        throw error;
    }
};
```

---

### 10. 获取模板统计信息接口

#### 接口信息

-   **接口路径**: `GET /api/attach-catalogue-template/statistics`
-   **接口描述**: 获取模板统计信息
-   **请求方式**: GET

#### React Axios 调用示例

```javascript
const getTemplateStatistics = async () => {
    try {
        const response = await axios.get(
            '/api/attach-catalogue-template/statistics',
            {
                headers: {
                    Authorization: `Bearer ${token}`,
                },
            }
        );

        console.log('获取统计信息成功:', response.data);
        return response.data;
    } catch (error) {
        console.error(
            '获取统计信息失败:',
            error.response?.data || error.message
        );
        throw error;
    }
};
```

---

## 错误处理

### 常见错误码

| 错误码 | 描述           | 解决方案                   |
| ------ | -------------- | -------------------------- |
| 400    | 请求参数错误   | 检查请求参数格式和必填字段 |
| 401    | 未授权         | 检查认证 token 是否有效    |
| 403    | 权限不足       | 检查用户权限               |
| 404    | 资源不存在     | 检查请求的资源 ID          |
| 500    | 服务器内部错误 | 联系系统管理员             |

### 错误响应格式

```json
{
    "error": {
        "code": "ErrorCode",
        "message": "错误描述",
        "details": "详细错误信息",
        "data": {
            "field": "具体字段错误信息"
        }
    }
}
```

## 最佳实践

### 1. 创建模板时

-   确保 `name` 唯一且有意义
-   合理设置 `workflowConfig` 的工作流参数
-   根据业务需求设置 `metaFields` 元数据字段
-   合理设置 `permissions` 权限配置

### 2. 搜索模板时

-   根据实际需求选择合适的搜索方式（混合检索、文本检索、语义检索）
-   合理设置相似度阈值以获得最佳结果
-   使用过滤条件缩小搜索范围

### 3. 性能优化

-   对于大型模板树，考虑分页加载
-   使用缓存减少重复请求
-   合理设置 `includeChildren` 参数

## 版本信息

-   **文档版本**: 1.0
-   **API 版本**: v1
-   **最后更新**: 2024-12-19
-   **维护人员**: 开发团队
