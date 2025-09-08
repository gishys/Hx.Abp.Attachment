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
| id                | Guid?                                  | 否   | 模板 ID（业务标识）   | "3fa85f64-5717-4562-b3fc-2c963f66afa6"               |
| name              | string                                 | 是   | 模板名称              | "合同文档模板"                                       |
| description       | string                                 | 否   | 模板描述              | "用于存储各类合同文档的模板"                         |
| tags              | string[]                               | 否   | 标签数组              | ["合同", "法律", "重要"]                             |
| attachReceiveType | AttachReceiveType                      | 是   | 附件接收类型          | 2                                                    |
| workflowConfig    | string                                 | 否   | 工作流配置(JSON 格式) | `{"workflowKey":"contract_approval","timeout":3600}` |
| isRequired        | boolean                                | 是   | 是否必填              | true                                                 |
| sequenceNumber    | int                                    | 是   | 排序号                | 100                                                  |
| isStatic          | boolean                                | 是   | 是否静态模板          | false                                                |
| parentId          | Guid?                                  | 否   | 父模板 ID             | "3fa85f64-5717-4562-b3fc-2c963f66afa6"               |
| parentVersion     | int?                                   | 否   | 父模板版本号          | 1                                                    |
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
| permissionType | string | 是 | 权限类型 | "Role" |
| permissionTarget | string | 是 | 权限目标 | "Admin" |
| action | PermissionAction | 是 | 权限动作 | 1 |
| effect | PermissionEffect | 是 | 权限效果 | 1 |
| attributeConditions | string? | 否 | 属性条件(JSON 格式) | "{\"department\":\"IT\"}" |
| isEnabled | boolean | 是 | 是否启用 | true |
| effectiveTime | DateTime? | 否 | 生效时间 | "2024-01-01T00:00:00Z" |
| expirationTime | DateTime? | 否 | 失效时间 | "2024-12-31T23:59:59Z" |
| description | string? | 否 | 权限描述 | "管理员权限" |

**CreateUpdateMetaFieldDto**:
| 字段名 | 类型 | 必填 | 说明 | 示例值 |
|--------------|--------|------|--------------|---------------------------|
| entityType | string | 是 | 实体类型 | "Project" |
| fieldKey | string | 是 | 字段键名 | "project_name" |
| fieldName | string | 是 | 字段显示名称 | "项目名称" |
| dataType | string | 是 | 数据类型 | "string" |
| unit | string? | 否 | 单位 | "万元" |
| isRequired | bool | 是 | 是否必填 | true |
| regexPattern | string? | 否 | 正则表达式模式 | "^[A-Za-z0-9]+$" |
| options | string? | 否 | 枚举选项(JSON 格式) | "[\"选项 1\",\"选项 2\"]" |
| description | string? | 否 | 字段描述 | "项目名称字段" |
| defaultValue | string? | 否 | 默认值 | "" |
| order | int | 是 | 字段顺序 | 1 |
| isEnabled | bool | 是 | 是否启用 | true |
| group | string? | 否 | 字段分组 | "基本信息" |
| validationRules | string? | 否 | 验证规则(JSON 格式) | "{\"minLength\":1,\"maxLength\":100}" |
| tags | string[] | 否 | 元数据标签 | ["重要", "必填"] |

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

-   1: 查看
-   2: 创建
-   3: 编辑
-   4: 删除
-   5: 审批
-   6: 发布
-   7: 归档
-   8: 导出
-   9: 导入
-   10: 管理权限
-   11: 管理配置
-   12: 查看审计日志
-   99: 所有权限

**PermissionEffect**:

-   1: 允许
-   2: 拒绝
-   3: 继承

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
    "parentVersion": null,
    "templatePath": "00001",
    "facetType": 0,
    "templatePurpose": 1,
    "textVector": null,
    "permissions": [
        {
            "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
            "permissionType": "Role",
            "permissionTarget": "Admin",
            "action": 2,
            "effect": 1,
            "attributeConditions": null,
            "isEnabled": true,
            "effectiveTime": "2024-01-01T00:00:00Z",
            "expirationTime": "2024-12-31T23:59:59Z",
            "description": "管理员权限"
        },
        {
            "id": null,
            "permissionType": "Role",
            "permissionTarget": "User",
            "action": 1,
            "effect": 1,
            "attributeConditions": null,
            "isEnabled": true,
            "effectiveTime": null,
            "expirationTime": null,
            "description": "用户查看权限"
        }
    ],
    "metaFields": [
        {
            "entityType": "Project",
            "fieldKey": "project_name",
            "fieldName": "项目名称",
            "dataType": "string",
            "unit": null,
            "isRequired": true,
            "regexPattern": "^[A-Za-z0-9]+$",
            "options": null,
            "description": "项目名称字段",
            "defaultValue": "",
            "order": 1,
            "isEnabled": true,
            "group": "基本信息",
            "validationRules": "{\"minLength\":1,\"maxLength\":100}",
            "tags": ["重要", "必填"],
            "creationTime": "2024-01-01T00:00:00Z",
            "lastModificationTime": "2024-01-01T00:00:00Z"
        },
        {
            "entityType": "Project",
            "fieldKey": "apply_date",
            "fieldName": "申请日期",
            "dataType": "datetime",
            "unit": null,
            "isRequired": false,
            "regexPattern": null,
            "options": null,
            "description": "申请提交日期",
            "defaultValue": "",
            "order": 2,
            "isEnabled": true,
            "group": "基本信息",
            "validationRules": null,
            "tags": ["日期"],
            "creationTime": "2024-01-01T00:00:00Z",
            "lastModificationTime": "2024-01-01T00:00:00Z"
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
                        permissionType: 'Role',
                        permissionTarget: 'Admin',
                        action: 2, // 创建
                        effect: 1, // 允许
                        attributeConditions: null,
                        isEnabled: true,
                        effectiveTime: '2024-01-01T00:00:00Z',
                        expirationTime: '2024-12-31T23:59:59Z',
                        description: '管理员权限',
                    },
                    {
                        id: null,
                        permissionType: 'Role',
                        permissionTarget: 'User',
                        action: 1, // 查看
                        effect: 1, // 允许
                        attributeConditions: null,
                        isEnabled: true,
                        effectiveTime: null,
                        expirationTime: null,
                        description: '用户查看权限',
                    },
                ],
                metaFields: [
                    {
                        entityType: 'Project',
                        fieldKey: 'project_name',
                        fieldName: '项目名称',
                        dataType: 'string',
                        unit: null,
                        isRequired: true,
                        regexPattern: '^[A-Za-z0-9]+$',
                        options: null,
                        description: '项目名称字段',
                        defaultValue: '',
                        order: 1,
                        isEnabled: true,
                        group: '基本信息',
                        validationRules: '{"minLength":1,"maxLength":100}',
                        tags: ['重要', '必填'],
                    },
                    {
                        entityType: 'Project',
                        fieldKey: 'apply_date',
                        fieldName: '申请日期',
                        dataType: 'datetime',
                        unit: null,
                        isRequired: false,
                        regexPattern: null,
                        options: null,
                        description: '申请提交日期',
                        defaultValue: '',
                        order: 2,
                        isEnabled: true,
                        group: '基本信息',
                        validationRules: null,
                        tags: ['日期'],
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

### 3. 获取模板（最新版本，支持树形结构）接口

#### 接口信息

-   **接口路径**: `GET /api/attach-catalogue-template/{id}`
-   **接口描述**: 获取指定模板的最新版本，支持返回树形结构
-   **请求方式**: GET

#### 请求参数

**路径参数**:

| 参数名 | 类型 | 必填 | 描述    | 示例值                                 |
| ------ | ---- | ---- | ------- | -------------------------------------- |
| id     | Guid | 是   | 模板 ID | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |

**查询参数**:

| 参数名               | 类型 | 必填 | 描述             | 默认值 | 示例值 |
| -------------------- | ---- | ---- | ---------------- | ------ | ------ |
| includeTreeStructure | bool | 否   | 是否包含树形结构 | false  | true   |

#### 响应说明

-   **includeTreeStructure=false**: 返回单个模板信息
-   **includeTreeStructure=true**: 返回包含完整树形结构的模板信息（包含所有父节点和子节点）

#### React Axios 调用示例

```javascript
// 获取单个模板
const getLatestTemplate = async (id) => {
    try {
        const response = await axios.get(
            `/api/attach-catalogue-template/${id}`,
            {
                headers: {
                    Authorization: `Bearer ${token}`,
                },
            }
        );

        console.log('获取最新版本模板成功:', response.data);
        return response.data;
    } catch (error) {
        console.error(
            '获取最新版本模板失败:',
            error.response?.data || error.message
        );
        throw error;
    }
};

// 获取模板及其树形结构
const getLatestTemplateWithTree = async (id) => {
    try {
        const response = await axios.get(
            `/api/attach-catalogue-template/${id}?includeTreeStructure=true`,
            {
                headers: {
                    Authorization: `Bearer ${token}`,
                },
            }
        );

        console.log('获取模板树形结构成功:', response.data);
        return response.data;
    } catch (error) {
        console.error(
            '获取模板树形结构失败:',
            error.response?.data || error.message
        );
        throw error;
    }
};
```

---

### 4. 根据 ID 获取分类模板接口

#### 接口信息

-   **接口路径**: `GET /api/attach-catalogue-template/{id}/{version}`
-   **接口描述**: 根据模板 ID 和版本号获取单个分类模板详情
-   **请求方式**: GET

#### 请求参数

**路径参数**:

| 参数名  | 类型 | 必填 | 描述    | 示例值                                 |
| ------- | ---- | ---- | ------- | -------------------------------------- |
| id      | Guid | 是   | 模板 ID | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |
| version | int  | 是   | 版本号  | 1                                      |

#### React Axios 调用示例

```javascript
const getTemplateById = async (id, version = 1) => {
    try {
        const response = await axios.get(
            `/api/attach-catalogue-template/${id}/${version}`,
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

### 5. 更新模板（最新版本）接口

#### 接口信息

-   **接口路径**: `PUT /api/attach-catalogue-template/{id}`
-   **接口描述**: 更新指定模板的最新版本
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
const updateLatestTemplate = async (id, updateData) => {
    try {
        const response = await axios.put(
            `/api/attach-catalogue-template/${id}`,
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

        console.log('最新版本模板更新成功:', response.data);
        return response.data;
    } catch (error) {
        console.error(
            '最新版本模板更新失败:',
            error.response?.data || error.message
        );
        throw error;
    }
};
```

---

### 6. 更新分类模板接口

#### 接口信息

-   **接口路径**: `PUT /api/attach-catalogue-template/{id}/{version}`
-   **接口描述**: 更新指定的分类模板
-   **请求方式**: PUT
-   **Content-Type**: application/json

#### 请求参数

**路径参数**:

| 参数名  | 类型 | 必填 | 描述    | 示例值                                 |
| ------- | ---- | ---- | ------- | -------------------------------------- |
| id      | Guid | 是   | 模板 ID | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |
| version | int  | 是   | 版本号  | 1                                      |

**请求体**: `CreateUpdateAttachCatalogueTemplateDto` (同创建接口)

#### React Axios 调用示例

```javascript
const updateTemplate = async (id, version, updateData) => {
    try {
        const response = await axios.put(
            `/api/attach-catalogue-template/${id}/${version}`,
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

### 7. 删除模板（所有版本）接口

#### 接口信息

-   **接口路径**: `DELETE /api/attach-catalogue-template/{id}`
-   **接口描述**: 删除指定模板的所有版本
-   **请求方式**: DELETE

#### 请求参数

**路径参数**:

| 参数名 | 类型 | 必填 | 描述    | 示例值                                 |
| ------ | ---- | ---- | ------- | -------------------------------------- |
| id     | Guid | 是   | 模板 ID | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |

#### React Axios 调用示例

```javascript
const deleteAllTemplateVersions = async (id) => {
    try {
        await axios.delete(`/api/attach-catalogue-template/${id}`, {
            headers: {
                Authorization: `Bearer ${token}`,
            },
        });

        console.log('模板所有版本删除成功');
    } catch (error) {
        console.error('模板删除失败:', error.response?.data || error.message);
        throw error;
    }
};
```

---

### 8. 删除分类模板接口

#### 接口信息

-   **接口路径**: `DELETE /api/attach-catalogue-template/{id}/{version}`
-   **接口描述**: 删除指定的分类模板版本
-   **请求方式**: DELETE

#### 请求参数

**路径参数**:

| 参数名  | 类型 | 必填 | 描述    | 示例值                                 |
| ------- | ---- | ---- | ------- | -------------------------------------- |
| id      | Guid | 是   | 模板 ID | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |
| version | int  | 是   | 版本号  | 1                                      |

#### React Axios 调用示例

```javascript
const deleteTemplate = async (id, version) => {
    try {
        await axios.delete(`/api/attach-catalogue-template/${id}/${version}`, {
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

### 9. 混合检索模板接口

#### 接口信息

-   **接口路径**: `POST /api/attach-catalogue-template/search/hybrid`
-   **接口描述**: 混合检索模板（字面 + 语义），基于行业最佳实践实现向量召回 + 全文检索加权过滤 + 分数融合
-   **请求方式**: POST
-   **Content-Type**: application/json

#### 请求参数

**请求体**: `TemplateSearchInputDto`

| 参数名              | 类型                | 必填 | 描述                      | 示例值           |
| ------------------- | ------------------- | ---- | ------------------------- | ---------------- |
| keyword             | string              | 否   | 搜索关键词（字面检索）    | "合同"           |
| semanticQuery       | string              | 否   | 语义查询（向量检索）      | "合同文档"       |
| facetType           | FacetType           | 否   | 分面类型过滤              | 0                |
| templatePurpose     | TemplatePurpose     | 否   | 模板用途过滤              | 1                |
| tags                | string[]            | 否   | 标签过滤（精确匹配）      | ["合同", "法律"] |
| onlyLatest          | boolean             | 否   | 是否只搜索最新版本        | true             |
| maxResults          | int                 | 否   | 最大返回结果数（1-100）   | 20               |
| similarityThreshold | double              | 否   | 向量相似度阈值（0.0-1.0） | 0.7              |
| weights             | HybridSearchWeights | 否   | 混合检索权重配置          | 见下方           |

**HybridSearchWeights**:

| 参数名         | 类型   | 必填 | 描述                    | 示例值 |
| -------------- | ------ | ---- | ----------------------- | ------ |
| textWeight     | double | 否   | 字面检索权重（0.0-1.0） | 0.4    |
| semanticWeight | double | 否   | 语义检索权重（0.0-1.0） | 0.6    |
| tagWeight      | double | 否   | 标签匹配权重（0.0-1.0） | 0.3    |
| nameWeight     | double | 否   | 名称匹配权重（0.0-1.0） | 0.5    |

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

**响应参数说明**:

| 参数名          | 类型            | 描述                    |
| --------------- | --------------- | ----------------------- |
| id              | Guid            | 模板唯一标识            |
| name            | string          | 模板名称                |
| description     | string          | 模板描述                |
| tags            | string[]        | 模板标签列表            |
| facetType       | FacetType       | 分面类型（见枚举说明）  |
| templatePurpose | TemplatePurpose | 模板用途（见枚举说明）  |
| totalScore      | double          | 综合评分（0.0-1.0）     |
| textScore       | double          | 字面检索评分（0.0-1.0） |
| semanticScore   | double          | 语义检索评分（0.0-1.0） |
| tagScore        | double          | 标签匹配评分（0.0-1.0） |
| matchReasons    | string[]        | 匹配原因列表            |
| isLatest        | boolean         | 是否为最新版本          |
| version         | int             | 版本号                  |

#### 使用说明

1. **混合检索策略**: 系统采用向量召回 + 全文检索加权过滤 + 分数融合的混合检索策略
2. **参数要求**: `keyword` 和 `semanticQuery` 至少提供一个，不能同时为空
3. **权重配置**: 权重值应在 0.0-1.0 之间，系统会自动进行归一化处理
4. **结果排序**: 结果按综合评分降序排列，评分相同时按序列号升序排列
5. **性能优化**: 系统使用 CTE（公用表表达式）和索引优化查询性能

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

### 10. 获取模板历史接口

#### 接口信息

-   **接口路径**: `GET /api/attach-catalogue-template/{id}/history`
-   **接口描述**: 获取指定模板的所有历史版本
-   **请求方式**: GET
-   **Content-Type**: application/json

#### 请求参数

**路径参数**:

| 参数名 | 类型 | 必填 | 描述    | 示例值                                 |
| ------ | ---- | ---- | ------- | -------------------------------------- |
| id     | Guid | 是   | 模板 ID | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |

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
            "version": 3,
            "isLatest": true,
            "attachReceiveType": 1,
            "workflowConfig": "{\"workflowKey\":\"contract_approval\",\"timeout\":3600}",
            "isRequired": true,
            "sequenceNumber": 1,
            "isStatic": false,
            "parentId": null,
            "parentVersion": null,
            "templatePath": "00001",
            "children": [],
            "facetType": 0,
            "templatePurpose": 1,
            "textVector": [0.1, 0.2, 0.3],
            "vectorDimension": 3,
            "permissions": [
                {
                    "id": "4fa85f64-5717-4562-b3fc-2c963f66afa7",
                    "userId": "user123",
                    "userName": "张三",
                    "action": 1,
                    "effect": 1,
                    "resource": "template"
                }
            ],
            "metaFields": [
                {
                    "id": "5fa85f64-5717-4562-b3fc-2c963f66afa8",
                    "fieldName": "合同编号",
                    "fieldType": "string",
                    "isRequired": true,
                    "defaultValue": "",
                    "description": "合同唯一编号"
                }
            ],
            "templateIdentifierDescription": "General - Classification",
            "isRoot": true,
            "isLeaf": true,
            "depth": 0,
            "path": "00001",
            "creationTime": "2024-01-15T10:30:00Z",
            "lastModificationTime": "2024-01-20T14:45:00Z",
            "creatorId": "user123",
            "lastModifierId": "user456"
        },
        {
            "id": "6fa85f64-5717-4562-b3fc-2c963f66afa9",
            "name": "合同文档模板",
            "description": "用于存储各类合同文档的模板（历史版本）",
            "tags": ["合同", "法律"],
            "version": 2,
            "isLatest": false,
            "attachReceiveType": 1,
            "workflowConfig": null,
            "isRequired": true,
            "sequenceNumber": 1,
            "isStatic": false,
            "parentId": null,
            "parentVersion": null,
            "templatePath": "00001",
            "children": [],
            "facetType": 0,
            "templatePurpose": 1,
            "textVector": null,
            "vectorDimension": 0,
            "permissions": [],
            "metaFields": [],
            "templateIdentifierDescription": "General - Classification",
            "isRoot": true,
            "isLeaf": true,
            "depth": 0,
            "path": "00001",
            "creationTime": "2024-01-10T09:15:00Z",
            "lastModificationTime": "2024-01-12T16:20:00Z",
            "creatorId": "user123",
            "lastModifierId": "user123"
        }
    ]
}
```

**响应参数说明**:

| 参数名                        | 类型                                   | 描述                    |
| ----------------------------- | -------------------------------------- | ----------------------- |
| items                         | AttachCatalogueTemplateDto[]           | 模板历史版本列表        |
| id                            | Guid                                   | 模板唯一标识            |
| name                          | string                                 | 模板名称                |
| description                   | string                                 | 模板描述                |
| tags                          | string[]                               | 模板标签列表            |
| version                       | int                                    | 版本号                  |
| isLatest                      | boolean                                | 是否为最新版本          |
| attachReceiveType             | AttachReceiveType                      | 附件类型（见枚举说明）  |
| workflowConfig                | string                                 | 工作流配置（JSON 格式） |
| isRequired                    | boolean                                | 是否必收                |
| sequenceNumber                | int                                    | 顺序号                  |
| isStatic                      | boolean                                | 是否静态                |
| parentId                      | Guid?                                  | 父模板 ID               |
| parentVersion                 | int?                                   | 父模板版本号            |
| templatePath                  | string                                 | 模板路径                |
| children                      | AttachCatalogueTemplateDto[]           | 子模板集合              |
| facetType                     | FacetType                              | 分面类型（见枚举说明）  |
| templatePurpose               | TemplatePurpose                        | 模板用途（见枚举说明）  |
| textVector                    | double[]                               | 文本向量                |
| vectorDimension               | int                                    | 向量维度                |
| permissions                   | AttachCatalogueTemplatePermissionDto[] | 权限集合                |
| metaFields                    | MetaFieldDto[]                         | 元数据字段集合          |
| templateIdentifierDescription | string                                 | 模板标识描述            |
| isRoot                        | boolean                                | 是否为根模板            |
| isLeaf                        | boolean                                | 是否为叶子模板          |
| depth                         | int                                    | 模板层级深度            |
| path                          | string                                 | 模板路径                |
| creationTime                  | DateTime                               | 创建时间                |
| lastModificationTime          | DateTime                               | 最后修改时间            |
| creatorId                     | Guid                                   | 创建者 ID               |
| lastModifierId                | Guid                                   | 最后修改者 ID           |

#### 使用说明

1. **版本管理**: 返回指定模板的所有历史版本，按版本号降序排列
2. **版本标识**: 通过 `isLatest` 字段可以识别哪个是当前最新版本
3. **历史追踪**: 可以查看模板的演进历史和变更记录
4. **权限继承**: 历史版本会保留创建时的权限配置
5. **元数据保留**: 历史版本的元数据字段配置会被完整保留

#### React Axios 调用示例

```javascript
const getTemplateHistory = async (id) => {
    try {
        const response = await axios.get(
            `/api/attach-catalogue-template/${id}/history`,
            {
                headers: {
                    'Content-Type': 'application/json',
                    Authorization: `Bearer ${token}`,
                },
            }
        );

        console.log('获取模板历史成功:', response.data);
        return response.data;
    } catch (error) {
        console.error(
            '获取模板历史失败:',
            error.response?.data || error.message
        );
        throw error;
    }
};

// 使用示例
const id = '3fa85f64-5717-4562-b3fc-2c963f66afa6';
getTemplateHistory(id).then((history) => {
    console.log(`模板 ${id} 共有 ${history.items.length} 个版本`);
    history.items.forEach((template) => {
        console.log(
            `版本 ${template.version}: ${template.name} (${
                template.isLatest ? '最新' : '历史'
            })`
        );
    });
});
```

---

### 11. 文本检索模板接口

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

### 12. 语义检索模板接口

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

### 13. 获取根节点模板接口

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

#### 响应参数

**成功响应** (200 OK):

| 字段名     | 类型                         | 必填 | 描述           | 示例值         |
| ---------- | ---------------------------- | ---- | -------------- | -------------- |
| items      | AttachCatalogueTemplateDto[] | 是   | 根节点模板列表 | 见下方详细说明 |
| totalCount | number                       | 是   | 总数量         | 10             |

**AttachCatalogueTemplateDto 详细字段说明**:

| 字段名                        | 类型                                   | 必填 | 描述                  | 示例值                                   |
| ----------------------------- | -------------------------------------- | ---- | --------------------- | ---------------------------------------- |
| id                            | string                                 | 是   | 模板 ID               | "3fa85f64-5717-4562-b3fc-2c963f66afa6"   |
| name                          | string                                 | 是   | 模板名称              | "项目文档模板"                           |
| description                   | string                                 | 否   | 模板描述              | "用于管理项目相关文档的模板"             |
| tags                          | string[]                               | 否   | 模板标签              | ["项目", "文档", "管理"]                 |
| version                       | number                                 | 是   | 模板版本号            | 1                                        |
| isLatest                      | boolean                                | 是   | 是否为最新版本        | true                                     |
| attachReceiveType             | AttachReceiveType                      | 是   | 附件类型              | 1                                        |
| workflowConfig                | string                                 | 否   | 工作流配置(JSON 格式) | "{\"workflowKey\":\"project_approval\"}" |
| isRequired                    | boolean                                | 是   | 是否必收              | true                                     |
| sequenceNumber                | number                                 | 是   | 顺序号                | 1                                        |
| isStatic                      | boolean                                | 是   | 是否静态              | false                                    |
| parentId                      | string                                 | 否   | 父模板 ID             | "3fa85f64-5717-4562-b3fc-2c963f66afa7"   |
| parentVersion                 | number                                 | 否   | 父模板版本号          | 1                                        |
| templatePath                  | string                                 | 否   | 模板路径              | "00001.00002"                            |
| children                      | AttachCatalogueTemplateDto[]           | 否   | 子模板集合            | 递归结构，同父级结构                     |
| facetType                     | FacetType                              | 是   | 分面类型              | 0                                        |
| templatePurpose               | TemplatePurpose                        | 是   | 模板用途              | 1                                        |
| textVector                    | number[]                               | 否   | 文本向量              | [0.1, 0.2, 0.3, ...]                     |
| vectorDimension               | number                                 | 是   | 向量维度              | 768                                      |
| permissions                   | AttachCatalogueTemplatePermissionDto[] | 否   | 权限集合              | 见下方权限字段说明                       |
| metaFields                    | MetaFieldDto[]                         | 否   | 元数据字段集合        | 见下方元数据字段说明                     |
| templateIdentifierDescription | string                                 | 是   | 模板标识描述          | "通用分面 - 分类管理"                    |
| isRoot                        | boolean                                | 是   | 是否为根模板          | true                                     |
| isLeaf                        | boolean                                | 是   | 是否为叶子模板        | false                                    |
| depth                         | number                                 | 是   | 模板层级深度          | 0                                        |
| path                          | string                                 | 否   | 模板路径              | "00001.00002"                            |
| creationTime                  | string                                 | 是   | 创建时间              | "2024-01-01T00:00:00Z"                   |
| lastModificationTime          | string                                 | 否   | 最后修改时间          | "2024-01-01T00:00:00Z"                   |
| creatorId                     | string                                 | 否   | 创建者 ID             | "3fa85f64-5717-4562-b3fc-2c963f66afa8"   |
| lastModifierId                | string                                 | 否   | 最后修改者 ID         | "3fa85f64-5717-4562-b3fc-2c963f66afa9"   |

**AttachCatalogueTemplatePermissionDto 权限字段说明**:

| 字段名              | 类型             | 必填 | 描述                | 示例值                                 |
| ------------------- | ---------------- | ---- | ------------------- | -------------------------------------- |
| id                  | string           | 是   | 权限 ID             | "3fa85f64-5717-4562-b3fc-2c963f66afaa" |
| permissionType      | string           | 是   | 权限类型            | "Role"                                 |
| permissionTarget    | string           | 是   | 权限目标            | "Admin"                                |
| action              | PermissionAction | 是   | 权限操作            | 1                                      |
| effect              | PermissionEffect | 是   | 权限效果            | 1                                      |
| attributeConditions | string           | 否   | 属性条件(JSON 格式) | "{\"department\":\"IT\"}"              |
| isEnabled           | boolean          | 是   | 是否启用            | true                                   |
| effectiveTime       | string           | 否   | 生效时间            | "2024-01-01T00:00:00Z"                 |
| expirationTime      | string           | 否   | 失效时间            | "2024-12-31T23:59:59Z"                 |
| description         | string           | 否   | 权限描述            | "管理员权限"                           |

**MetaFieldDto 元数据字段说明**:

| 字段名               | 类型     | 必填 | 描述                | 示例值                                |
| -------------------- | -------- | ---- | ------------------- | ------------------------------------- |
| entityType           | string   | 是   | 实体类型            | "Project"                             |
| fieldKey             | string   | 是   | 字段键名            | "project_name"                        |
| fieldName            | string   | 是   | 字段显示名称        | "项目名称"                            |
| dataType             | string   | 是   | 数据类型            | "string"                              |
| unit                 | string   | 否   | 单位                | "万元"                                |
| isRequired           | boolean  | 是   | 是否必填            | true                                  |
| regexPattern         | string   | 否   | 正则表达式模式      | "^[A-Za-z0-9]+$"                      |
| options              | string   | 否   | 枚举选项(JSON 格式) | "[\"选项 1\",\"选项 2\"]"             |
| description          | string   | 否   | 字段描述            | "项目名称字段"                        |
| defaultValue         | string   | 否   | 默认值              | ""                                    |
| order                | number   | 是   | 字段顺序            | 1                                     |
| isEnabled            | boolean  | 是   | 是否启用            | true                                  |
| group                | string   | 否   | 字段分组            | "基本信息"                            |
| validationRules      | string   | 否   | 验证规则(JSON 格式) | "{\"minLength\":1,\"maxLength\":100}" |
| tags                 | string[] | 否   | 元数据标签          | ["重要", "必填"]                      |
| creationTime         | string   | 是   | 创建时间            | "2024-01-01T00:00:00Z"                |
| lastModificationTime | string   | 否   | 最后修改时间        | "2024-01-01T00:00:00Z"                |

#### 响应示例

````json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "项目文档模板",
      "description": "用于管理项目相关文档的模板",
      "tags": ["项目", "文档", "管理"],
      "version": 1,
      "isLatest": true,
      "attachReceiveType": 1,
      "workflowConfig": "{\"workflowKey\":\"project_approval\",\"steps\":[{\"name\":\"初审\",\"approver\":\"project_manager\"}]}",
      "isRequired": true,
      "sequenceNumber": 1,
      "isStatic": false,
      "parentId": null,
      "parentVersion": null,
      "templatePath": "00001",
      "children": [
        {
          "id": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
          "name": "设计文档",
          "description": "项目设计相关文档",
          "tags": ["设计", "技术"],
          "version": 1,
          "isLatest": true,
          "attachReceiveType": 1,
          "workflowConfig": null,
          "isRequired": true,
          "sequenceNumber": 1,
          "isStatic": false,
          "parentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
          "parentVersion": 1,
          "templatePath": "00001.00001",
          "children": [],
          "facetType": 0,
          "templatePurpose": 1,
          "textVector": [0.1, 0.2, 0.3],
          "vectorDimension": 768,
          "permissions": [],
          "metaFields": [
            {
              "entityType": "Project",
              "fieldKey": "project_name",
              "fieldName": "项目名称",
              "dataType": "string",
              "unit": null,
              "isRequired": true,
              "regexPattern": "^[A-Za-z0-9]+$",
              "options": null,
              "description": "项目名称字段",
              "defaultValue": "",
              "order": 1,
              "isEnabled": true,
              "group": "基本信息",
              "validationRules": "{\"minLength\":1,\"maxLength\":100}",
              "tags": ["重要", "必填"],
              "creationTime": "2024-01-01T00:00:00Z",
              "lastModificationTime": "2024-01-01T00:00:00Z"
            }
          ],
          "templateIdentifierDescription": "通用分面 - 分类管理",
          "isRoot": false,
          "isLeaf": true,
          "depth": 1,
          "path": "00001.00001",
          "creationTime": "2024-01-01T00:00:00Z",
          "lastModificationTime": "2024-01-01T00:00:00Z",
          "creatorId": "3fa85f64-5717-4562-b3fc-2c963f66afa8",
          "lastModifierId": "3fa85f64-5717-4562-b3fc-2c963f66afa9"
        }
      ],
      "facetType": 0,
      "templatePurpose": 1,
      "textVector": [0.1, 0.2, 0.3],
      "vectorDimension": 768,
      "permissions": [
        {
          "id": "3fa85f64-5717-4562-b3fc-2c963f66afaa",
          "permissionType": "Role",
          "permissionTarget": "Admin",
          "action": 1,
          "effect": 1,
          "attributeConditions": "{\"department\":\"IT\"}",
          "isEnabled": true,
          "effectiveTime": "2024-01-01T00:00:00Z",
          "expirationTime": "2024-12-31T23:59:59Z",
          "description": "管理员权限"
        }
      ],
      "metaFields": [
        {
          "entityType": "Project",
          "fieldKey": "project_name",
          "fieldName": "项目名称",
          "dataType": "string",
          "unit": null,
          "isRequired": true,
          "regexPattern": "^[A-Za-z0-9]+$",
          "options": null,
          "description": "项目名称字段",
          "defaultValue": "",
          "order": 1,
          "isEnabled": true,
          "group": "基本信息",
          "validationRules": "{\"minLength\":1,\"maxLength\":100}",
          "tags": ["重要", "必填"],
          "creationTime": "2024-01-01T00:00:00Z",
          "lastModificationTime": "2024-01-01T00:00:00Z"
        }
      ],
      "templateIdentifierDescription": "通用分面 - 分类管理",
      "isRoot": true,
      "isLeaf": false,
      "depth": 0,
      "path": "00001",
      "creationTime": "2024-01-01T00:00:00Z",
      "lastModificationTime": "2024-01-01T00:00:00Z",
      "creatorId": "3fa85f64-5717-4562-b3fc-2c963f66afa8",
      "lastModifierId": "3fa85f64-5717-4562-b3fc-2c963f66afa9"
    }
  ],
  "totalCount": 1
}

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
````

---

### 14. 获取模板统计信息接口

#### 接口信息

-   **接口路径**: `GET /api/attach-catalogue-template/statistics`
-   **接口描述**: 获取模板统计信息，包含基础统计、分面类型统计、模板用途统计、树形结构统计等
-   **请求方式**: GET

#### 请求参数

**无请求参数**

#### 响应参数

**成功响应** (200 OK):

| 字段名                     | 类型   | 必填 | 描述                 | 示例值                 |
| -------------------------- | ------ | ---- | -------------------- | ---------------------- |
| totalCount                 | number | 是   | 总模板数量           | 150                    |
| rootTemplateCount          | number | 是   | 根节点模板数量       | 25                     |
| childTemplateCount         | number | 是   | 子节点模板数量       | 125                    |
| latestVersionCount         | number | 是   | 最新版本模板数量     | 150                    |
| historyVersionCount        | number | 是   | 历史版本模板数量     | 45                     |
| generalFacetCount          | number | 是   | 通用分面模板数量     | 80                     |
| disciplineFacetCount       | number | 是   | 专业领域分面模板数量 | 70                     |
| classificationPurposeCount | number | 是   | 分类管理用途模板数量 | 60                     |
| documentPurposeCount       | number | 是   | 文档管理用途模板数量 | 50                     |
| workflowPurposeCount       | number | 是   | 工作流用途模板数量   | 40                     |
| templatesWithVector        | number | 是   | 有向量的模板数量     | 120                    |
| averageVectorDimension     | number | 是   | 平均向量维度         | 768.5                  |
| maxTreeDepth               | number | 是   | 最大树深度           | 5                      |
| averageChildrenCount       | number | 是   | 平均子节点数量       | 3.2                    |
| latestCreationTime         | string | 否   | 最近创建时间         | "2024-01-15T10:30:00Z" |
| latestModificationTime     | string | 否   | 最近修改时间         | "2024-01-15T14:20:00Z" |

#### 响应示例

```json
{
    "totalCount": 150,
    "rootTemplateCount": 25,
    "childTemplateCount": 125,
    "latestVersionCount": 150,
    "historyVersionCount": 45,
    "generalFacetCount": 80,
    "disciplineFacetCount": 70,
    "classificationPurposeCount": 60,
    "documentPurposeCount": 50,
    "workflowPurposeCount": 40,
    "templatesWithVector": 120,
    "averageVectorDimension": 768.5,
    "maxTreeDepth": 5,
    "averageChildrenCount": 3.2,
    "latestCreationTime": "2024-01-15T10:30:00Z",
    "latestModificationTime": "2024-01-15T14:20:00Z"
}
```

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

        // 使用统计数据
        const {
            totalCount,
            rootTemplateCount,
            childTemplateCount,
            generalFacetCount,
            disciplineFacetCount,
            maxTreeDepth,
            averageChildrenCount,
        } = response.data;

        console.log(`总模板数量: ${totalCount}`);
        console.log(`根节点数量: ${rootTemplateCount}`);
        console.log(`子节点数量: ${childTemplateCount}`);
        console.log(`最大树深度: ${maxTreeDepth}`);
        console.log(`平均子节点数: ${averageChildrenCount}`);

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

#### 使用说明

-   该接口提供模板系统的全面统计信息
-   统计数据基于动态分类树的业务需求设计
-   所有统计字段使用简单类型（数字、字符串），便于前端展示
-   统计数据实时计算，反映当前系统状态
-   可用于系统监控、数据分析和管理决策

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

### 15. 获取模板结构接口

#### 接口信息

-   **接口路径**: `GET /api/attach-catalogue-template/structure/{id}`
-   **接口描述**: 获取模板的完整结构，包含当前版本、历史版本和子模板树形结构
-   **请求方式**: GET

#### 请求参数

**路径参数**:

| 参数名 | 类型   | 必填 | 描述    | 示例值                                 |
| ------ | ------ | ---- | ------- | -------------------------------------- |
| id     | string | 是   | 模板 ID | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |

**查询参数**:

| 参数名         | 类型    | 必填 | 描述             | 示例值 |
| -------------- | ------- | ---- | ---------------- | ------ |
| includeHistory | boolean | 否   | 是否包含历史版本 | true   |

#### 响应参数

**成功响应** (200 OK):

| 字段名          | 类型                         | 必填 | 描述                         | 示例值             |
| --------------- | ---------------------------- | ---- | ---------------------------- | ------------------ |
| versions        | AttachCatalogueTemplateDto[] | 是   | 模板版本列表（按版本号降序） | 见下方详细说明     |
| currentVersion  | AttachCatalogueTemplateDto   | 否   | 当前版本（最新版本）         | 从 versions 中提取 |
| historyVersions | AttachCatalogueTemplateDto[] | 否   | 历史版本列表                 | 从 versions 中提取 |
| basicInfo       | TemplateBasicInfoDto         | 否   | 模板基本信息                 | 见下方基本信息说明 |
| versionStats    | TemplateVersionStatsDto      | 是   | 版本统计信息                 | 见下方统计信息说明 |

**TemplateBasicInfoDto 基本信息说明**:

| 字段名               | 类型            | 必填 | 描述           | 示例值                                 |
| -------------------- | --------------- | ---- | -------------- | -------------------------------------- |
| id                   | string          | 是   | 模板 ID        | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |
| name                 | string          | 是   | 模板名称       | "项目文档模板"                         |
| description          | string          | 否   | 模板描述       | "用于管理项目相关文档的模板"           |
| version              | number          | 是   | 版本号         | 2                                      |
| isLatest             | boolean         | 是   | 是否为最新版本 | true                                   |
| facetType            | FacetType       | 是   | 分面类型       | 0                                      |
| templatePurpose      | TemplatePurpose | 是   | 模板用途       | 1                                      |
| creationTime         | string          | 是   | 创建时间       | "2024-01-01T00:00:00Z"                 |
| lastModificationTime | string          | 否   | 最后修改时间   | "2024-01-01T00:00:00Z"                 |

**TemplateVersionStatsDto 统计信息说明**:

| 字段名               | 类型    | 必填 | 描述           | 示例值                                 |
| -------------------- | ------- | ---- | -------------- | -------------------------------------- |
| totalVersions        | number  | 是   | 总版本数       | 3                                      |
| currentVersionNumber | number  | 是   | 当前版本号     | 2                                      |
| hasHistory           | boolean | 是   | 是否有历史版本 | true                                   |
| latestVersionId      | string  | 否   | 最新版本 ID    | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |
| firstCreatedTime     | string  | 是   | 首次创建时间   | "2024-01-01T00:00:00Z"                 |
| lastModifiedTime     | string  | 是   | 最后修改时间   | "2024-01-15T10:30:00Z"                 |

#### 响应示例

```json
{
    "versions": [
        {
            "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
            "name": "项目文档模板",
            "description": "用于管理项目相关文档的模板",
            "tags": ["项目", "文档", "管理"],
            "version": 2,
            "isLatest": true,
            "attachReceiveType": 1,
            "workflowConfig": "{\"workflowKey\":\"project_approval_v2\"}",
            "isRequired": true,
            "sequenceNumber": 1,
            "isStatic": false,
            "parentId": null,
            "parentVersion": null,
            "templatePath": "00001",
            "children": [
                {
                    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
                    "name": "设计文档",
                    "description": "项目设计相关文档",
                    "tags": ["设计", "技术"],
                    "version": 2,
                    "isLatest": true,
                    "attachReceiveType": 1,
                    "workflowConfig": null,
                    "isRequired": true,
                    "sequenceNumber": 1,
                    "isStatic": false,
                    "parentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
                    "parentVersion": 1,
                    "templatePath": "00001.00001",
                    "children": [],
                    "facetType": 0,
                    "templatePurpose": 1,
                    "textVector": [0.1, 0.2, 0.3],
                    "vectorDimension": 768,
                    "permissions": [],
                    "metaFields": [],
                    "templateIdentifierDescription": "通用分面 - 分类管理",
                    "isRoot": false,
                    "isLeaf": true,
                    "depth": 1,
                    "path": "00001.00001",
                    "creationTime": "2024-01-01T00:00:00Z",
                    "lastModificationTime": "2024-01-15T10:30:00Z",
                    "creatorId": "3fa85f64-5717-4562-b3fc-2c963f66afa8",
                    "lastModifierId": "3fa85f64-5717-4562-b3fc-2c963f66afa9"
                }
            ],
            "facetType": 0,
            "templatePurpose": 1,
            "textVector": [0.1, 0.2, 0.3],
            "vectorDimension": 768,
            "permissions": [],
            "metaFields": [],
            "templateIdentifierDescription": "通用分面 - 分类管理",
            "isRoot": true,
            "isLeaf": false,
            "depth": 0,
            "path": "00001",
            "creationTime": "2024-01-01T00:00:00Z",
            "lastModificationTime": "2024-01-15T10:30:00Z",
            "creatorId": "3fa85f64-5717-4562-b3fc-2c963f66afa8",
            "lastModifierId": "3fa85f64-5717-4562-b3fc-2c963f66afa9"
        },
        {
            "id": "3fa85f64-5717-4562-b3fc-2c963f66afaa",
            "name": "项目文档模板",
            "description": "用于管理项目相关文档的模板（历史版本）",
            "tags": ["项目", "文档", "管理"],
            "version": 1,
            "isLatest": false,
            "attachReceiveType": 1,
            "workflowConfig": "{\"workflowKey\":\"project_approval_v1\"}",
            "isRequired": true,
            "sequenceNumber": 1,
            "isStatic": false,
            "parentId": null,
            "parentVersion": null,
            "templatePath": "00001",
            "children": [],
            "facetType": 0,
            "templatePurpose": 1,
            "textVector": [0.1, 0.2, 0.3],
            "vectorDimension": 768,
            "permissions": [],
            "metaFields": [],
            "templateIdentifierDescription": "通用分面 - 分类管理",
            "isRoot": true,
            "isLeaf": true,
            "depth": 0,
            "path": "00001",
            "creationTime": "2024-01-01T00:00:00Z",
            "lastModificationTime": "2024-01-10T15:20:00Z",
            "creatorId": "3fa85f64-5717-4562-b3fc-2c963f66afa8",
            "lastModifierId": "3fa85f64-5717-4562-b3fc-2c963f66afa9"
        }
    ],
    "currentVersion": {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "name": "项目文档模板",
        "description": "用于管理项目相关文档的模板",
        "version": 2,
        "isLatest": true,
        "facetType": 0,
        "templatePurpose": 1,
        "creationTime": "2024-01-01T00:00:00Z",
        "lastModificationTime": "2024-01-15T10:30:00Z"
    },
    "historyVersions": [
        {
            "id": "3fa85f64-5717-4562-b3fc-2c963f66afaa",
            "name": "项目文档模板",
            "description": "用于管理项目相关文档的模板（历史版本）",
            "version": 1,
            "isLatest": false,
            "facetType": 0,
            "templatePurpose": 1,
            "creationTime": "2024-01-01T00:00:00Z",
            "lastModificationTime": "2024-01-10T15:20:00Z"
        }
    ],
    "basicInfo": {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "name": "项目文档模板",
        "description": "用于管理项目相关文档的模板",
        "version": 2,
        "isLatest": true,
        "facetType": 0,
        "templatePurpose": 1,
        "creationTime": "2024-01-01T00:00:00Z",
        "lastModificationTime": "2024-01-15T10:30:00Z"
    },
    "versionStats": {
        "totalVersions": 2,
        "currentVersionNumber": 2,
        "hasHistory": true,
        "latestVersionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "firstCreatedTime": "2024-01-01T00:00:00Z",
        "lastModifiedTime": "2024-01-15T10:30:00Z"
    }
}
```

#### React Axios 调用示例

```javascript
const getTemplateStructure = async (id, includeHistory = false) => {
    try {
        const response = await axios.get(
            `/api/attach-catalogue-template/structure/${id}`,
            {
                params: {
                    includeHistory: includeHistory,
                },
                headers: {
                    Authorization: `Bearer ${token}`,
                },
            }
        );

        console.log('获取模板结构成功:', response.data);

        // 使用优化后的数据结构
        const {
            versions,
            currentVersion,
            historyVersions,
            basicInfo,
            versionStats,
        } = response.data;

        // 当前版本（包含完整的子模板树形结构）
        console.log('当前版本:', currentVersion);

        // 历史版本列表
        console.log('历史版本:', historyVersions);

        // 基本信息
        console.log('基本信息:', basicInfo);

        // 版本统计
        console.log('版本统计:', versionStats);

        return response.data;
    } catch (error) {
        console.error(
            '获取模板结构失败:',
            error.response?.data || error.message
        );
        throw error;
    }
};
```

#### 使用说明

-   使用 `currentVersion` 获取当前版本信息
-   使用 `historyVersions` 获取历史版本列表
-   使用 `basicInfo` 获取模板基本信息
-   使用 `versionStats` 获取版本统计信息
-   通过 `currentVersion.children` 访问子模板树形结构

## 版本信息

-   **文档版本**: 1.0
-   **API 版本**: v1
-   **最后更新**: 2024-12-19
-   **维护人员**: 开发团队
