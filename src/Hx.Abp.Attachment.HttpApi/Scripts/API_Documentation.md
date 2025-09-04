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

| 参数名            | 类型              | 必填 | 描述                  | 示例值                                               |
| ----------------- | ----------------- | ---- | --------------------- | ---------------------------------------------------- |
| templateName      | string            | 是   | 模板名称              | "合同文档模板"                                       |
| description       | string            | 否   | 模板描述              | "用于存储各类合同文档的模板"                         |
| tags              | string[]          | 否   | 标签数组              | ["合同", "法律", "重要"]                             |
| attachReceiveType | AttachReceiveType | 是   | 附件接收类型          | 1                                                    |
| workflowConfig    | string            | 否   | 工作流配置(JSON 格式) | `{"workflowKey":"contract_approval","timeout":3600}` |
| isRequired        | boolean           | 是   | 是否必填              | true                                                 |
| sequenceNumber    | int               | 是   | 排序号                | 100                                                  |
| isStatic          | boolean           | 是   | 是否静态模板          | false                                                |
| parentId          | Guid?             | 否   | 父模板 ID             | "3fa85f64-5717-4562-b3fc-2c963f66afa6"               |
| templatePath      | string            | 否   | 模板路径              | "00001.00002"                                        |
| facetType         | FacetType         | 是   | 分面类型              | 0                                                    |
| templatePurpose   | TemplatePurpose   | 是   | 模板用途              | 1                                                    |
| textVector        | string            | 否   | 文本向量              | null                                                 |
| permissions       | PermissionDto[]   | 否   | 权限配置              | []                                                   |
| metaFields        | MetaFieldDto[]    | 否   | 元数据字段            | []                                                   |

#### 枚举值说明

**AttachReceiveType**:

-   0: 单文件
-   1: 多文件
-   2: 文件夹

**FacetType**:

-   0: 文档类型
-   1: 业务类型
-   2: 组织类型

**TemplatePurpose**:

-   0: 通用模板
-   1: 业务模板
-   2: 系统模板

#### 响应结果

**成功响应** (200 OK):

```json
{
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "templateName": "合同文档模板",
    "description": "用于存储各类合同文档的模板",
    "tags": ["合同", "法律", "重要"],
    "attachReceiveType": 1,
    "workflowConfig": "{\"workflowKey\":\"contract_approval\",\"timeout\":3600}",
    "isRequired": true,
    "sequenceNumber": 100,
    "isStatic": false,
    "parentId": null,
    "templatePath": "00001",
    "facetType": 0,
    "templatePurpose": 1,
    "textVector": null,
    "permissions": [],
    "metaFields": [],
    "creationTime": "2024-12-19T10:00:00Z",
    "lastModificationTime": null,
    "isDeleted": false,
    "isLatest": true
}
```

**错误响应** (400 Bad Request):

```json
{
    "error": {
        "code": "ValidationError",
        "message": "模板名称不能为空",
        "details": "TemplateName is required"
    }
}
```

#### 应用场景

##### 场景 1: 创建根级合同模板

```json
{
    "templateName": "合同文档模板",
    "description": "用于存储各类合同文档的根级模板",
    "tags": ["合同", "法律", "重要"],
    "attachReceiveType": 1,
    "workflowConfig": "{\"workflowKey\":\"contract_approval\",\"timeout\":3600,\"skipApprovers\":[\"admin\"],\"scripts\":[\"validate_contract.js\"],\"webhooks\":[\"https://api.company.com/contract-notify\"]}",
    "isRequired": true,
    "sequenceNumber": 100,
    "isStatic": false,
    "parentId": null,
    "facetType": 0,
    "templatePurpose": 1,
    "permissions": [],
    "metaFields": [
        {
            "fieldName": "contractType",
            "fieldType": "string",
            "isRequired": true,
            "defaultValue": "服务合同"
        },
        {
            "fieldName": "contractAmount",
            "fieldType": "decimal",
            "isRequired": true,
            "defaultValue": "0.00"
        }
    ]
}
```

##### 场景 2: 创建子级采购合同模板

```json
{
    "templateName": "采购合同模板",
    "description": "专门用于采购合同的子级模板",
    "tags": ["采购", "合同", "子模板"],
    "attachReceiveType": 1,
    "workflowConfig": "{\"workflowKey\":\"purchase_approval\",\"timeout\":1800,\"skipApprovers\":[\"purchase_manager\"],\"scripts\":[\"validate_purchase.js\"],\"webhooks\":[\"https://api.company.com/purchase-notify\"]}",
    "isRequired": true,
    "sequenceNumber": 200,
    "isStatic": false,
    "parentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "facetType": 0,
    "templatePurpose": 1,
    "permissions": [
        {
            "userId": "user123",
            "permission": "read"
        }
    ],
    "metaFields": [
        {
            "fieldName": "supplierName",
            "fieldType": "string",
            "isRequired": true,
            "defaultValue": ""
        },
        {
            "fieldName": "purchaseAmount",
            "fieldType": "decimal",
            "isRequired": true,
            "defaultValue": "0.00"
        }
    ]
}
```

##### 场景 3: 创建系统级模板

```json
{
    "templateName": "系统日志模板",
    "description": "用于存储系统运行日志的模板",
    "tags": ["系统", "日志", "监控"],
    "attachReceiveType": 2,
    "workflowConfig": "{\"workflowKey\":\"log_processing\",\"timeout\":600,\"scripts\":[\"process_log.js\"],\"webhooks\":[\"https://api.company.com/log-notify\"]}",
    "isRequired": false,
    "sequenceNumber": 999,
    "isStatic": true,
    "parentId": null,
    "facetType": 2,
    "templatePurpose": 2,
    "permissions": [
        {
            "userId": "system",
            "permission": "full"
        }
    ],
    "metaFields": [
        {
            "fieldName": "logLevel",
            "fieldType": "string",
            "isRequired": true,
            "defaultValue": "INFO"
        },
        {
            "fieldName": "moduleName",
            "fieldName": "string",
            "isRequired": true,
            "defaultValue": "unknown"
        }
    ]
}
```

#### 调用示例

**cURL 示例**:

```bash
curl -X POST "https://api.company.com/api/attach-catalogue-template" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer your-token" \
  -d '{
    "templateName": "合同文档模板",
    "description": "用于存储各类合同文档的模板",
    "tags": ["合同", "法律", "重要"],
    "attachReceiveType": 1,
    "workflowConfig": "{\"workflowKey\":\"contract_approval\",\"timeout\":3600}",
    "isRequired": true,
    "sequenceNumber": 100,
    "isStatic": false,
    "facetType": 0,
    "templatePurpose": 1,
    "permissions": [],
    "metaFields": []
  }'
```

**JavaScript 示例**:

```javascript
const createTemplate = async (templateData) => {
    try {
        const response = await fetch('/api/attach-catalogue-template', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                Authorization: 'Bearer ' + token,
            },
            body: JSON.stringify(templateData),
        });

        if (response.ok) {
            const result = await response.json();
            console.log('模板创建成功:', result);
            return result;
        } else {
            const error = await response.json();
            console.error('模板创建失败:', error);
            throw new Error(error.message);
        }
    } catch (error) {
        console.error('请求失败:', error);
        throw error;
    }
};

// 使用示例
const templateData = {
    templateName: '合同文档模板',
    description: '用于存储各类合同文档的模板',
    tags: ['合同', '法律', '重要'],
    attachReceiveType: 1,
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
    facetType: 0,
    templatePurpose: 1,
    permissions: [],
    metaFields: [],
};

createTemplate(templateData);
```

---

### 2. 获取根节点模板接口

#### 接口信息

-   **接口路径**: `GET /api/attach-catalogue-template/tree/roots`
-   **接口描述**: 获取根节点模板列表，用于树状展示
-   **请求方式**: GET
-   **Content-Type**: application/json

#### 请求参数

**查询参数**:

| 参数名          | 类型             | 必填 | 描述               | 示例值 |
| --------------- | ---------------- | ---- | ------------------ | ------ |
| facetType       | FacetType?       | 否   | 分面类型过滤       | 0      |
| templatePurpose | TemplatePurpose? | 否   | 模板用途过滤       | 1      |
| includeChildren | boolean          | 否   | 是否包含子节点     | true   |
| onlyLatest      | boolean          | 否   | 是否只返回最新版本 | true   |

#### 响应结果

**成功响应** (200 OK):

```json
{
    "items": [
        {
            "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
            "templateName": "合同文档模板",
            "description": "用于存储各类合同文档的模板",
            "tags": ["合同", "法律", "重要"],
            "attachReceiveType": 1,
            "workflowConfig": "{\"workflowKey\":\"contract_approval\",\"timeout\":3600}",
            "isRequired": true,
            "sequenceNumber": 100,
            "isStatic": false,
            "parentId": null,
            "templatePath": "00001",
            "facetType": 0,
            "templatePurpose": 1,
            "textVector": null,
            "permissions": [],
            "metaFields": [],
            "children": [
                {
                    "id": "4fa85f64-5717-4562-b3fc-2c963f66afa7",
                    "templateName": "采购合同模板",
                    "description": "专门用于采购合同的子级模板",
                    "tags": ["采购", "合同", "子模板"],
                    "attachReceiveType": 1,
                    "workflowConfig": "{\"workflowKey\":\"purchase_approval\",\"timeout\":1800}",
                    "isRequired": true,
                    "sequenceNumber": 200,
                    "isStatic": false,
                    "parentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
                    "templatePath": "00001.00001",
                    "facetType": 0,
                    "templatePurpose": 1,
                    "textVector": null,
                    "permissions": [],
                    "metaFields": [],
                    "children": []
                }
            ],
            "creationTime": "2024-12-19T10:00:00Z",
            "lastModificationTime": null,
            "isDeleted": false,
            "isLatest": true
        }
    ]
}
```

#### 应用场景

##### 场景 1: 获取所有根级模板（用于树状展示）

```
GET /api/attach-catalogue-template/tree/roots?includeChildren=true&onlyLatest=true
```

**应用场景**: 在管理后台的模板管理页面，需要展示完整的模板树结构，包括所有子节点。

##### 场景 2: 获取特定类型的根级模板

```
GET /api/attach-catalogue-template/tree/roots?facetType=0&templatePurpose=1&includeChildren=true&onlyLatest=true
```

**应用场景**: 在业务系统中，用户需要选择特定类型的模板（如业务模板），只显示相关的根级模板。

##### 场景 3: 获取根级模板概览（不包含子节点）

```
GET /api/attach-catalogue-template/tree/roots?includeChildren=false&onlyLatest=true
```

**应用场景**: 在模板选择页面，用户只需要看到根级模板的概览，点击后再加载子节点。

##### 场景 4: 获取所有版本（包括历史版本）

```
GET /api/attach-catalogue-template/tree/roots?includeChildren=true&onlyLatest=false
```

**应用场景**: 在模板版本管理页面，管理员需要查看所有版本的模板，包括历史版本。

#### 调用示例

**cURL 示例**:

```bash
# 获取所有根级模板（包含子节点）
curl -X GET "https://api.company.com/api/attach-catalogue-template/tree/roots?includeChildren=true&onlyLatest=true" \
  -H "Authorization: Bearer your-token"

# 获取特定类型的根级模板
curl -X GET "https://api.company.com/api/attach-catalogue-template/tree/roots?facetType=0&templatePurpose=1&includeChildren=true&onlyLatest=true" \
  -H "Authorization: Bearer your-token"

# 获取根级模板概览（不包含子节点）
curl -X GET "https://api.company.com/api/attach-catalogue-template/tree/roots?includeChildren=false&onlyLatest=true" \
  -H "Authorization: Bearer your-token"
```

**JavaScript 示例**:

```javascript
const getRootTemplates = async (params = {}) => {
    try {
        const queryParams = new URLSearchParams();

        if (params.facetType !== undefined) {
            queryParams.append('facetType', params.facetType);
        }
        if (params.templatePurpose !== undefined) {
            queryParams.append('templatePurpose', params.templatePurpose);
        }
        if (params.includeChildren !== undefined) {
            queryParams.append('includeChildren', params.includeChildren);
        }
        if (params.onlyLatest !== undefined) {
            queryParams.append('onlyLatest', params.onlyLatest);
        }

        const url = `/api/attach-catalogue-template/tree/roots?${queryParams.toString()}`;

        const response = await fetch(url, {
            method: 'GET',
            headers: {
                Authorization: 'Bearer ' + token,
            },
        });

        if (response.ok) {
            const result = await response.json();
            console.log('获取根级模板成功:', result);
            return result;
        } else {
            const error = await response.json();
            console.error('获取根级模板失败:', error);
            throw new Error(error.message);
        }
    } catch (error) {
        console.error('请求失败:', error);
        throw error;
    }
};

// 使用示例
// 1. 获取所有根级模板（包含子节点）
getRootTemplates({
    includeChildren: true,
    onlyLatest: true,
});

// 2. 获取特定类型的根级模板
getRootTemplates({
    facetType: 0,
    templatePurpose: 1,
    includeChildren: true,
    onlyLatest: true,
});

// 3. 获取根级模板概览（不包含子节点）
getRootTemplates({
    includeChildren: false,
    onlyLatest: true,
});
```

**Vue.js 组件示例**:

```vue
<template>
    <div class="template-tree">
        <div
            v-for="template in rootTemplates"
            :key="template.id"
            class="template-node"
        >
            <div class="template-item" @click="toggleNode(template)">
                <span
                    class="expand-icon"
                    :class="{ expanded: template.expanded }"
                >
                    {{ template.children.length > 0 ? '▶' : '•' }}
                </span>
                <span class="template-name">{{ template.templateName }}</span>
                <span class="template-description">{{
                    template.description
                }}</span>
            </div>

            <div
                v-if="template.expanded && template.children.length > 0"
                class="children"
            >
                <div
                    v-for="child in template.children"
                    :key="child.id"
                    class="child-node"
                >
                    <span class="child-name">{{ child.templateName }}</span>
                    <span class="child-description">{{
                        child.description
                    }}</span>
                </div>
            </div>
        </div>
    </div>
</template>

<script>
export default {
    data() {
        return {
            rootTemplates: [],
            loading: false,
        };
    },

    async mounted() {
        await this.loadRootTemplates();
    },

    methods: {
        async loadRootTemplates() {
            this.loading = true;
            try {
                const response = await this.$http.get(
                    '/api/attach-catalogue-template/tree/roots',
                    {
                        params: {
                            includeChildren: true,
                            onlyLatest: true,
                        },
                    }
                );

                this.rootTemplates = response.data.items.map((template) => ({
                    ...template,
                    expanded: false,
                }));
            } catch (error) {
                console.error('加载根级模板失败:', error);
                this.$message.error('加载模板失败');
            } finally {
                this.loading = false;
            }
        },

        toggleNode(template) {
            template.expanded = !template.expanded;
        },
    },
};
</script>
```

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

-   确保 `templateName` 唯一且有意义
-   合理设置 `workflowConfig` 的工作流参数
-   根据业务需求设置 `metaFields` 元数据字段
-   合理设置 `permissions` 权限配置

### 2. 获取模板树时

-   根据实际需求设置 `includeChildren` 参数
-   在管理页面使用 `onlyLatest=true`
-   在版本管理页面使用 `onlyLatest=false`
-   合理使用 `facetType` 和 `templatePurpose` 过滤

### 3. 性能优化

-   对于大型模板树，考虑分页加载
-   使用缓存减少重复请求
-   合理设置 `includeChildren` 参数

## 版本信息

-   **文档版本**: 1.0
-   **API 版本**: v1
-   **最后更新**: 2024-12-19
-   **维护人员**: 开发团队
