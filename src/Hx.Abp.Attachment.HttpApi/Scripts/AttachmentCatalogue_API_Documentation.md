# 附件目录接口文档

## 概述

本文档详细描述了附件目录相关的 API 接口，包括接口说明、参数详解、应用场景和调用示例。附件目录系统支持元数据字段管理、权限控制、全文检索、混合检索等高级功能。

## 接口列表

### 1. 创建附件目录接口

#### 接口信息

-   **接口路径**: `POST /api/app/attachment`
-   **接口描述**: 创建新的附件目录
-   **请求方式**: POST
-   **Content-Type**: application/json

#### 请求参数

**查询参数**:

| 参数名 | 类型                | 必填 | 描述     | 示例值 |
| ------ | ------------------- | ---- | -------- | ------ |
| mode   | CatalogueCreateMode | 否   | 创建模式 | 0      |

**请求体**: `AttachCatalogueCreateDto`

| 参数名             | 类型                                           | 必填 | 描述          | 示例值                                 |
| ------------------ | ---------------------------------------------- | ---- | ------------- | -------------------------------------- |
| attachReceiveType  | AttachReceiveType                              | 是   | 附件收取类型  | 2                                      |
| catalogueName      | string                                         | 是   | 分类名称      | "合同文档分类"                         |
| tags               | string[]                                       | 否   | 分类标签      | ["合同", "法律", "重要"]               |
| sequenceNumber     | int                                            | 否   | 序号          | 100                                    |
| referenceType      | int                                            | 是   | 业务类型标识  | 1                                      |
| reference          | string                                         | 是   | 业务 Id       | "CONTRACT_001"                         |
| parentId           | Guid?                                          | 否   | 父节点 Id     | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |
| isVerification     | boolean                                        | 否   | 是否核验      | false                                  |
| verificationPassed | boolean                                        | 否   | 核验通过      | false                                  |
| isRequired         | boolean                                        | 是   | 是否必收      | true                                   |
| isStatic           | boolean                                        | 否   | 静态标识      | false                                  |
| children           | AttachCatalogueCreateDto[]                     | 否   | 子文件夹      | []                                     |
| attachFiles        | AttachFileCreateDto[]                          | 否   | 子文件        | []                                     |
| templateId         | Guid?                                          | 否   | 关联的模板 ID | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |
| catalogueFacetType | FacetType                                      | 否   | 分类分面类型  | 0                                      |
| cataloguePurpose   | TemplatePurpose                                | 否   | 分类用途      | 1                                      |
| templateRole       | TemplateRole                                   | 否   | 分类角色      | 3                                      |
| textVector         | double[]                                       | 否   | 文本向量      | null                                   |
| path               | string                                         | 否   | 分类路径      | "0000001.0000002.0000003"              |
| metaFields         | [MetaFieldDto](#metafielddto-用于查询和返回)[] | 否   | 元数据字段    | []                                     |

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

**TemplateRole**:

-   1: 根分类 - 可以作为根节点创建动态分类树
-   2: 导航分类 - 仅用于导航，不参与动态分类树创建
-   3: 分支节点 - 可以有子节点，但不能直接上传文件
-   4: 叶子节点 - 不能有子节点，但可以直接上传文件

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

#### 复杂类型说明

**MetaFieldDto** (用于查询和返回):

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
| order                | int      | 是   | 字段顺序            | 1                                     |
| isEnabled            | boolean  | 是   | 是否启用            | true                                  |
| group                | string   | 否   | 字段分组            | "基本信息"                            |
| validationRules      | string   | 否   | 验证规则(JSON 格式) | "{\"minLength\":1,\"maxLength\":100}" |
| tags                 | string[] | 否   | 元数据标签          | ["重要", "必填"]                      |
| creationTime         | DateTime | 是   | 创建时间            | "2024-01-01T00:00:00Z"                |
| lastModificationTime | DateTime | 否   | 最后修改时间        | "2024-01-01T00:00:00Z"                |

**CreateUpdateMetaFieldDto** (用于创建和更新):

| 字段名          | 类型     | 必填 | 描述                | 示例值                                |
| --------------- | -------- | ---- | ------------------- | ------------------------------------- |
| entityType      | string   | 是   | 实体类型            | "Project"                             |
| fieldKey        | string   | 是   | 字段键名            | "project_name"                        |
| fieldName       | string   | 是   | 字段显示名称        | "项目名称"                            |
| dataType        | string   | 是   | 数据类型            | "string"                              |
| unit            | string   | 否   | 单位                | "万元"                                |
| isRequired      | boolean  | 是   | 是否必填            | true                                  |
| regexPattern    | string   | 否   | 正则表达式模式      | "^[A-Za-z0-9]+$"                      |
| options         | string   | 否   | 枚举选项(JSON 格式) | "[\"选项 1\",\"选项 2\"]"             |
| description     | string   | 否   | 字段描述            | "项目名称字段"                        |
| defaultValue    | string   | 否   | 默认值              | ""                                    |
| order           | int      | 是   | 字段顺序            | 1                                     |
| isEnabled       | boolean  | 是   | 是否启用            | true                                  |
| group           | string   | 否   | 字段分组            | "基本信息"                            |
| validationRules | string   | 否   | 验证规则(JSON 格式) | "{\"minLength\":1,\"maxLength\":100}" |
| tags            | string[] | 否   | 元数据标签          | ["重要", "必填"]                      |

**AttachCatalogueTemplatePermissionDto**:

| 字段名              | 类型             | 必填 | 描述                | 示例值                                 |
| ------------------- | ---------------- | ---- | ------------------- | -------------------------------------- |
| id                  | Guid?            | 否   | 权限 ID             | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |
| permissionType      | string           | 是   | 权限类型            | "Role"                                 |
| permissionTarget    | string           | 是   | 权限目标            | "Admin"                                |
| action              | PermissionAction | 是   | 权限动作            | 1                                      |
| effect              | PermissionEffect | 是   | 权限效果            | 1                                      |
| attributeConditions | string?          | 否   | 属性条件(JSON 格式) | "{\"department\":\"IT\"}"              |
| isEnabled           | boolean          | 是   | 是否启用            | true                                   |
| effectiveTime       | DateTime?        | 否   | 生效时间            | "2024-01-01T00:00:00Z"                 |
| expirationTime      | DateTime?        | 否   | 失效时间            | "2024-12-31T23:59:59Z"                 |
| description         | string?          | 否   | 权限描述            | "管理员权限"                           |

**AttachFileDto** (附件文件信息):

| 字段名            | 类型                                 | 必填 | 描述           | 示例值                                 |
| ----------------- | ------------------------------------ | ---- | -------------- | -------------------------------------- |
| id                | Guid                                 | 是   | 文件 ID        | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |
| fileAlias         | string                               | 是   | 文件别名       | "合同正文"                             |
| sequenceNumber    | int                                  | 是   | 序号           | 1                                      |
| filePath          | string                               | 是   | 文件路径       | "/host/attachment/contract_001.pdf"    |
| fileName          | string                               | 是   | 文件名称       | "contract_001.pdf"                     |
| fileType          | string                               | 是   | 文件类型       | "pdf"                                  |
| fileSize          | int                                  | 是   | 文件大小(字节) | 1024000                                |
| downloadTimes     | int                                  | 是   | 下载次数       | 5                                      |
| attachCatalogueId | Guid?                                | 否   | 关联分类 ID    | "3fa85f64-5717-4562-b3fc-2c963f66afa7" |
| reference         | string?                              | 否   | 业务引用       | "CONTRACT_001"                         |
| templatePurpose   | [TemplatePurpose](#templatepurpose)? | 否   | 模板用途       | 1                                      |
| isCategorized     | bool                                 | 是   | 是否已归类     | true                                   |

#### 响应结果

**成功响应** (200 OK):

```json
{
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "reference": "CONTRACT_001",
    "attachReceiveType": 2,
    "referenceType": 1,
    "catalogueName": "合同文档分类",
    "tags": ["合同", "法律", "重要"],
    "sequenceNumber": 100,
    "parentId": null,
    "isRequired": true,
    "attachCount": 0,
    "pageCount": 0,
    "isStatic": false,
    "isVerification": false,
    "verificationPassed": false,
    "templateId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "catalogueFacetType": 0,
    "cataloguePurpose": 1,
    "templateRole": 3,
    "textVector": null,
    "path": "0000001.0000002.0000003",
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
    "creationTime": "2024-12-19T10:00:00Z",
    "lastModificationTime": null,
    "isDeleted": false
}
```

#### React Axios 调用示例

```javascript
import axios from 'axios';

const createAttachmentCatalogue = async (catalogueData, mode = 0) => {
    try {
        const response = await axios.post(
            '/api/app/attachment',
            {
                attachReceiveType: 2, // 复印件
                catalogueName: '合同文档分类',
                tags: ['合同', '法律', '重要'],
                sequenceNumber: 100,
                referenceType: 1,
                reference: 'CONTRACT_001',
                parentId: null,
                isVerification: false,
                verificationPassed: false,
                isRequired: true,
                isStatic: false,
                children: [],
                attachFiles: [],
                templateId: '3fa85f64-5717-4562-b3fc-2c963f66afa6',
                catalogueFacetType: 0, // 通用分面
                cataloguePurpose: 1, // 分类管理
                textVector: null,
                path: '0000001.0000002.0000003', // 分类路径
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
                ],
            },
            {
                params: {
                    mode: mode,
                },
                headers: {
                    'Content-Type': 'application/json',
                    Authorization: `Bearer ${token}`,
                },
            }
        );

        console.log('附件目录创建成功:', response.data);
        return response.data;
    } catch (error) {
        console.error(
            '附件目录创建失败:',
            error.response?.data || error.message
        );
        throw error;
    }
};

// 使用示例
createAttachmentCatalogue();
```

---

### 2. 批量创建附件目录接口

#### 接口信息

-   **接口路径**: `POST /api/app/attachment/createmany`
-   **接口描述**: 批量创建附件目录
-   **请求方式**: POST
-   **Content-Type**: application/json

#### 请求参数

**查询参数**:

| 参数名 | 类型                | 必填 | 描述     | 示例值 |
| ------ | ------------------- | ---- | -------- | ------ |
| mode   | CatalogueCreateMode | 是   | 创建模式 | 0      |

**请求体**: `AttachCatalogueCreateDto[]`

#### React Axios 调用示例

```javascript
const createManyAttachmentCatalogues = async (catalogueList, mode) => {
    try {
        const response = await axios.post(
            '/api/app/attachment/createmany',
            [
                {
                    attachReceiveType: 2,
                    catalogueName: '合同文档分类1',
                    tags: ['合同', '法律'],
                    referenceType: 1,
                    reference: 'CONTRACT_001',
                    isRequired: true,
                    catalogueFacetType: 0,
                    cataloguePurpose: 1,
                },
                {
                    attachReceiveType: 2,
                    catalogueName: '合同文档分类2',
                    tags: ['合同', '重要'],
                    referenceType: 1,
                    reference: 'CONTRACT_002',
                    isRequired: true,
                    catalogueFacetType: 0,
                    cataloguePurpose: 1,
                },
            ],
            {
                params: {
                    mode: mode,
                },
                headers: {
                    'Content-Type': 'application/json',
                    Authorization: `Bearer ${token}`,
                },
            }
        );

        console.log('批量创建附件目录成功:', response.data);
        return response.data;
    } catch (error) {
        console.error(
            '批量创建附件目录失败:',
            error.response?.data || error.message
        );
        throw error;
    }
};
```

---

### 3. 查询目录下的文件接口

#### 接口信息

-   **接口路径**: `GET /api/app/attachment/queryfiles`
-   **接口描述**: 查询指定目录下的所有文件
-   **请求方式**: GET

#### 请求参数

**查询参数**:

| 参数名      | 类型 | 必填 | 描述    | 示例值                                 |
| ----------- | ---- | ---- | ------- | -------------------------------------- |
| catalogueId | Guid | 是   | 目录 ID | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |

#### React Axios 调用示例

```javascript
const queryFiles = async (catalogueId) => {
    try {
        const response = await axios.get('/api/app/attachment/queryfiles', {
            params: {
                catalogueId: catalogueId,
            },
            headers: {
                Authorization: `Bearer ${token}`,
            },
        });

        console.log('查询文件成功:', response.data);
        return response.data;
    } catch (error) {
        console.error('查询文件失败:', error.response?.data || error.message);
        throw error;
    }
};
```

---

### 4. 查询单个文件接口

#### 接口信息

-   **接口路径**: `GET /api/app/attachment/query`
-   **接口描述**: 根据文件 ID 查询单个文件
-   **请求方式**: GET

#### 请求参数

**查询参数**:

| 参数名       | 类型 | 必填 | 描述    | 示例值                                 |
| ------------ | ---- | ---- | ------- | -------------------------------------- |
| attachFileId | Guid | 是   | 文件 ID | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |

#### React Axios 调用示例

```javascript
const queryFile = async (attachFileId) => {
    try {
        const response = await axios.get('/api/app/attachment/query', {
            params: {
                attachFileId: attachFileId,
            },
            headers: {
                Authorization: `Bearer ${token}`,
            },
        });

        console.log('查询文件成功:', response.data);
        return response.data;
    } catch (error) {
        console.error('查询文件失败:', error.response?.data || error.message);
        throw error;
    }
};
```

---

### 5. 上传文件接口

#### 接口信息

-   **接口路径**: `POST /api/app/attachment/uploadfiles`
-   **接口描述**: 上传文件到指定目录
-   **请求方式**: POST
-   **Content-Type**: multipart/form-data

#### 请求参数

**查询参数**:

| 参数名 | 类型   | 必填 | 描述     | 示例值                                 |
| ------ | ------ | ---- | -------- | -------------------------------------- |
| id     | Guid?  | 否   | 目录 ID  | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |
| prefix | string | 否   | 文件前缀 | "contract"                             |

**请求体**: FormData (包含文件)

#### React Axios 调用示例

```javascript
const uploadFiles = async (files, catalogueId = null, prefix = null) => {
    try {
        const formData = new FormData();

        // 添加文件到FormData
        files.forEach((file, index) => {
            formData.append(`file${index}`, file);
        });

        const response = await axios.post(
            '/api/app/attachment/uploadfiles',
            formData,
            {
                params: {
                    id: catalogueId,
                    prefix: prefix,
                },
                headers: {
                    'Content-Type': 'multipart/form-data',
                    Authorization: `Bearer ${token}`,
                },
            }
        );

        console.log('文件上传成功:', response.data);
        return response.data;
    } catch (error) {
        console.error('文件上传失败:', error.response?.data || error.message);
        throw error;
    }
};

// 使用示例
const handleFileUpload = async (event) => {
    const files = Array.from(event.target.files);
    await uploadFiles(
        files,
        '3fa85f64-5717-4562-b3fc-2c963f66afa6',
        'contract'
    );
};
```

---

### 6. 更新附件目录接口

#### 接口信息

-   **接口路径**: `PUT /api/app/attachment/update`
-   **接口描述**: 更新指定的附件目录
-   **请求方式**: PUT
-   **Content-Type**: application/json

#### 请求参数

**查询参数**:

| 参数名 | 类型 | 必填 | 描述    | 示例值                                 |
| ------ | ---- | ---- | ------- | -------------------------------------- |
| id     | Guid | 是   | 目录 ID | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |

**请求体**: `AttachCatalogueCreateDto` (同创建接口)

#### React Axios 调用示例

```javascript
const updateAttachmentCatalogue = async (catalogueId, updateData) => {
    try {
        const response = await axios.put(
            '/api/app/attachment/update',
            {
                attachReceiveType: 2,
                catalogueName: '更新后的合同文档分类',
                tags: ['合同', '法律', '重要', '更新'],
                sequenceNumber: 100,
                referenceType: 1,
                reference: 'CONTRACT_001_UPDATED',
                isRequired: true,
                catalogueFacetType: 0,
                cataloguePurpose: 1,
            },
            {
                params: {
                    id: catalogueId,
                },
                headers: {
                    'Content-Type': 'application/json',
                    Authorization: `Bearer ${token}`,
                },
            }
        );

        console.log('附件目录更新成功:', response.data);
        return response.data;
    } catch (error) {
        console.error(
            '附件目录更新失败:',
            error.response?.data || error.message
        );
        throw error;
    }
};
```

---

### 7. 删除附件目录接口

#### 接口信息

-   **接口路径**: `DELETE /api/app/attachment/delete`
-   **接口描述**: 删除指定的附件目录
-   **请求方式**: DELETE

#### 请求参数

**查询参数**:

| 参数名 | 类型 | 必填 | 描述    | 示例值                                 |
| ------ | ---- | ---- | ------- | -------------------------------------- |
| id     | Guid | 是   | 目录 ID | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |

#### React Axios 调用示例

```javascript
const deleteAttachmentCatalogue = async (catalogueId) => {
    try {
        await axios.delete('/api/app/attachment/delete', {
            params: {
                id: catalogueId,
            },
            headers: {
                Authorization: `Bearer ${token}`,
            },
        });

        console.log('附件目录删除成功');
    } catch (error) {
        console.error(
            '附件目录删除失败:',
            error.response?.data || error.message
        );
        throw error;
    }
};
```

---

### 8. 删除单个文件接口

#### 接口信息

-   **接口路径**: `DELETE /api/app/attachment/deletefile`
-   **接口描述**: 删除指定的单个文件
-   **请求方式**: DELETE

#### 请求参数

**查询参数**:

| 参数名       | 类型 | 必填 | 描述    | 示例值                                 |
| ------------ | ---- | ---- | ------- | -------------------------------------- |
| attachFileId | Guid | 是   | 文件 ID | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |

#### React Axios 调用示例

```javascript
const deleteSingleFile = async (attachFileId) => {
    try {
        await axios.delete('/api/app/attachment/deletefile', {
            params: {
                attachFileId: attachFileId,
            },
            headers: {
                Authorization: `Bearer ${token}`,
            },
        });

        console.log('文件删除成功');
    } catch (error) {
        console.error('文件删除失败:', error.response?.data || error.message);
        throw error;
    }
};
```

---

### 9. 更新单个文件接口

#### 接口信息

-   **接口路径**: `PUT /api/app/attachment/updatefile`
-   **接口描述**: 更新指定的单个文件
-   **请求方式**: PUT
-   **Content-Type**: application/json

#### 请求参数

**查询参数**:

| 参数名       | 类型 | 必填 | 描述    | 示例值                                 |
| ------------ | ---- | ---- | ------- | -------------------------------------- |
| catalogueId  | Guid | 是   | 目录 ID | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |
| attachFileId | Guid | 是   | 文件 ID | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |

**请求体**: `AttachFileCreateDto`

#### React Axios 调用示例

```javascript
const updateSingleFile = async (catalogueId, attachFileId, fileData) => {
    try {
        const response = await axios.put(
            '/api/app/attachment/updatefile',
            {
                fileAlias: '更新后的文件名.pdf',
                documentContent: fileData.documentContent,
            },
            {
                params: {
                    catalogueId: catalogueId,
                    attachFileId: attachFileId,
                },
                headers: {
                    'Content-Type': 'application/json',
                    Authorization: `Bearer ${token}`,
                },
            }
        );

        console.log('文件更新成功:', response.data);
        return response.data;
    } catch (error) {
        console.error('文件更新失败:', error.response?.data || error.message);
        throw error;
    }
};
```

---

### 10. 根据引用查找目录接口

#### 接口信息

-   **接口路径**: `POST /api/app/attachment/findbyreference`
-   **接口描述**: 根据业务引用查找附件目录
-   **请求方式**: POST
-   **Content-Type**: application/json

#### 请求参数

**请求体**: `GetAttachListInput[]`

#### React Axios 调用示例

```javascript
const findByReference = async (references) => {
    try {
        const response = await axios.post(
            '/api/app/attachment/findbyreference',
            [
                {
                    reference: 'CONTRACT_001',
                    referenceType: 1,
                },
                {
                    reference: 'CONTRACT_002',
                    referenceType: 1,
                },
            ],
            {
                headers: {
                    'Content-Type': 'application/json',
                    Authorization: `Bearer ${token}`,
                },
            }
        );

        console.log('根据引用查找成功:', response.data);
        return response.data;
    } catch (error) {
        console.error(
            '根据引用查找失败:',
            error.response?.data || error.message
        );
        throw error;
    }
};
```

---

### 11. 验证上传接口

#### 接口信息

-   **接口路径**: `POST /api/app/attachment/verifyupload`
-   **接口描述**: 验证上传的附件
-   **请求方式**: POST
-   **Content-Type**: application/json

#### 请求参数

**查询参数**:

| 参数名  | 类型    | 必填 | 描述         | 示例值 |
| ------- | ------- | ---- | ------------ | ------ |
| details | boolean | 否   | 是否返回详情 | false  |

**请求体**: `GetAttachListInput[]`

#### React Axios 调用示例

```javascript
const verifyUpload = async (references, details = false) => {
    try {
        const response = await axios.post(
            '/api/app/attachment/verifyupload',
            [
                {
                    reference: 'CONTRACT_001',
                    referenceType: 1,
                },
            ],
            {
                params: {
                    details: details,
                },
                headers: {
                    'Content-Type': 'application/json',
                    Authorization: `Bearer ${token}`,
                },
            }
        );

        console.log('验证上传成功:', response.data);
        return response.data;
    } catch (error) {
        console.error('验证上传失败:', error.response?.data || error.message);
        throw error;
    }
};
```

---

### 12. 根据引用删除接口

#### 接口信息

-   **接口路径**: `POST /api/app/attachment/deletebyreference`
-   **接口描述**: 根据业务引用删除附件目录
-   **请求方式**: POST
-   **Content-Type**: application/json

#### 请求参数

**请求体**: `AttachCatalogueCreateDto[]`

#### React Axios 调用示例

```javascript
const deleteByReference = async (catalogueList) => {
    try {
        await axios.post(
            '/api/app/attachment/deletebyreference',
            [
                {
                    attachReceiveType: 2,
                    catalogueName: '合同文档分类',
                    referenceType: 1,
                    reference: 'CONTRACT_001',
                    isRequired: true,
                    catalogueFacetType: 0,
                    cataloguePurpose: 1,
                },
            ],
            {
                headers: {
                    'Content-Type': 'application/json',
                    Authorization: `Bearer ${token}`,
                },
            }
        );

        console.log('根据引用删除成功');
    } catch (error) {
        console.error(
            '根据引用删除失败:',
            error.response?.data || error.message
        );
        throw error;
    }
};
```

---

### 13. 根据文件 ID 获取目录接口

#### 接口信息

-   **接口路径**: `GET /api/app/attachment/getbyfileid`
-   **接口描述**: 根据文件 ID 获取所属的附件目录
-   **请求方式**: GET

#### 请求参数

**查询参数**:

| 参数名 | 类型 | 必填 | 描述    | 示例值                                 |
| ------ | ---- | ---- | ------- | -------------------------------------- |
| fileId | Guid | 是   | 文件 ID | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |

#### React Axios 调用示例

```javascript
const getCatalogueByFileId = async (fileId) => {
    try {
        const response = await axios.get('/api/app/attachment/getbyfileid', {
            params: {
                fileId: fileId,
            },
            headers: {
                Authorization: `Bearer ${token}`,
            },
        });

        console.log('根据文件ID获取目录成功:', response.data);
        return response.data;
    } catch (error) {
        console.error(
            '根据文件ID获取目录失败:',
            error.response?.data || error.message
        );
        throw error;
    }
};
```

---

### 14. 全文检索接口

#### 接口信息

-   **接口路径**: `GET /api/app/attachment/search/fulltext`
-   **接口描述**: 全文检索附件目录
-   **请求方式**: GET

#### 请求参数

**查询参数**:

| 参数名        | 类型   | 必填 | 描述         | 示例值         |
| ------------- | ------ | ---- | ------------ | -------------- |
| searchText    | string | 是   | 搜索文本     | "合同"         |
| reference     | string | 否   | 业务引用     | "CONTRACT_001" |
| referenceType | int    | 否   | 业务类型     | 1              |
| limit         | int    | 否   | 返回数量限制 | 10             |

#### React Axios 调用示例

```javascript
const searchByFullText = async (
    searchText,
    reference = null,
    referenceType = null,
    limit = 10
) => {
    try {
        const response = await axios.get(
            '/api/app/attachment/search/fulltext',
            {
                params: {
                    searchText,
                    reference,
                    referenceType,
                    limit,
                },
                headers: {
                    Authorization: `Bearer ${token}`,
                },
            }
        );

        console.log('全文检索成功:', response.data);
        return response.data;
    } catch (error) {
        console.error('全文检索失败:', error.response?.data || error.message);
        throw error;
    }
};
```

---

### 15. 混合检索接口

#### 接口信息

-   **接口路径**: `GET /api/app/attachment/search/hybrid`
-   **接口描述**: 混合检索附件目录（结合全文检索和向量检索）
-   **请求方式**: GET

#### 请求参数

**查询参数**:

| 参数名              | 类型   | 必填 | 描述         | 示例值         |
| ------------------- | ------ | ---- | ------------ | -------------- |
| searchText          | string | 是   | 搜索文本     | "合同"         |
| reference           | string | 否   | 业务引用     | "CONTRACT_001" |
| referenceType       | int    | 否   | 业务类型     | 1              |
| limit               | int    | 否   | 返回数量限制 | 10             |
| queryTextVector     | string | 否   | 查询文本向量 | null           |
| similarityThreshold | float  | 否   | 相似度阈值   | 0.7            |

#### 返回类型

**成功响应** (200 OK):

返回 `List<AttachCatalogueDto>` 类型的混合检索结果列表。

**AttachCatalogueDto 字段说明**:

| 字段名                         | 类型                                                                                | 必填 | 描述                         | 示例值                                 |
| ------------------------------ | ----------------------------------------------------------------------------------- | ---- | ---------------------------- | -------------------------------------- |
| id                             | Guid                                                                                | 是   | 分类 ID                      | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |
| reference                      | string                                                                              | 是   | 业务引用                     | "CONTRACT_001"                         |
| attachReceiveType              | [AttachReceiveType](#attachreceivetype)                                             | 是   | 附件收取类型                 | 2                                      |
| referenceType                  | int                                                                                 | 是   | 业务类型标识                 | 1                                      |
| catalogueName                  | string                                                                              | 是   | 分类名称                     | "合同文档分类"                         |
| tags                           | List<string>                                                                        | 否   | 分类标签                     | ["合同", "法律", "重要"]               |
| sequenceNumber                 | int                                                                                 | 是   | 顺序号                       | 100                                    |
| parentId                       | Guid?                                                                               | 否   | 父分类 ID                    | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |
| isRequired                     | bool                                                                                | 是   | 是否必收                     | true                                   |
| attachCount                    | int                                                                                 | 是   | 附件数量                     | 5                                      |
| pageCount                      | int                                                                                 | 是   | 页数                         | 10                                     |
| isStatic                       | bool                                                                                | 是   | 静态标识                     | false                                  |
| isVerification                 | bool                                                                                | 是   | 是否核验                     | false                                  |
| verificationPassed             | bool                                                                                | 是   | 核验通过                     | false                                  |
| children                       | List<AttachCatalogueDto>                                                            | 否   | 子分类列表                   | []                                     |
| attachFiles                    | Collection<[AttachFileDto](#attachfiledto-附件文件信息)>?                           | 否   | 附件文件集合                 | null                                   |
| templateId                     | Guid?                                                                               | 否   | 关联的模板 ID                | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |
| templateVersion                | int?                                                                                | 否   | 关联的模板版本号             | 1                                      |
| fullTextContent                | string?                                                                             | 否   | 全文内容                     | "合同文档内容..."                      |
| fullTextContentUpdatedTime     | DateTime?                                                                           | 否   | 全文内容更新时间             | "2024-01-01T00:00:00Z"                 |
| catalogueFacetType             | [FacetType](#facettype)                                                             | 是   | 分类分面类型                 | 0                                      |
| cataloguePurpose               | [TemplatePurpose](#templatepurpose)                                                 | 是   | 分类用途                     | 1                                      |
| templateRole                   | [TemplateRole](#templaterole)                                                       | 是   | 分类角色                     | 3                                      |
| textVector                     | List<double>?                                                                       | 否   | 文本向量                     | [0.1, 0.2, 0.3]                        |
| vectorDimension                | int                                                                                 | 是   | 向量维度                     | 128                                    |
| path                           | string?                                                                             | 否   | 分类路径（用于快速查询层级） | "0000001.0000002.0000003"              |
| permissions                    | List<[AttachCatalogueTemplatePermissionDto](#attachcataloguetemplatepermissiondto)> | 否   | 权限集合                     | []                                     |
| metaFields                     | List<[MetaFieldDto](#metafielddto-用于查询和返回)>                                  | 否   | 元数据字段集合               | []                                     |
| catalogueIdentifierDescription | string                                                                              | 是   | 分类标识描述（计算属性）     | "General - Classification"             |
| creationTime                   | DateTime                                                                            | 是   | 创建时间                     | "2024-01-01T00:00:00Z"                 |
| lastModificationTime           | DateTime?                                                                           | 否   | 最后修改时间                 | "2024-01-01T00:00:00Z"                 |
| creatorId                      | Guid?                                                                               | 否   | 创建者 ID                    | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |
| lastModifierId                 | Guid?                                                                               | 否   | 最后修改者 ID                | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |

**响应示例**:

```json
[
    {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "reference": "CONTRACT_001",
        "attachReceiveType": 2,
        "referenceType": 1,
        "catalogueName": "合同文档分类",
        "tags": ["合同", "法律", "重要"],
        "sequenceNumber": 100,
        "parentId": null,
        "isRequired": true,
        "attachCount": 5,
        "pageCount": 10,
        "isStatic": false,
        "isVerification": false,
        "verificationPassed": false,
        "children": [],
        "attachFiles": null,
        "templateId": "8fa85f64-5717-4562-b3fc-2c963f66afab",
        "templateVersion": 1,
        "fullTextContent": "合同文档分类的全文内容...",
        "fullTextContentUpdatedTime": "2024-01-01T00:00:00Z",
        "catalogueFacetType": 0,
        "cataloguePurpose": 1,
        "templateRole": 1,
        "textVector": [0.1, 0.2, 0.3],
        "vectorDimension": 128,
        "path": "0000001",
        "permissions": [],
        "metaFields": [],
        "catalogueIdentifierDescription": "General - Classification",
        "creationTime": "2024-01-01T00:00:00Z",
        "lastModificationTime": "2024-01-01T00:00:00Z",
        "creatorId": "9fa85f64-5717-4562-b3fc-2c963f66afac",
        "lastModifierId": "afa85f64-5717-4562-b3fc-2c963f66afad"
    }
]
```

#### React Axios 调用示例

```javascript
const searchByHybrid = async (
    searchText,
    reference = null,
    referenceType = null,
    limit = 10,
    queryTextVector = null,
    similarityThreshold = 0.7
) => {
    try {
        const response = await axios.get('/api/app/attachment/search/hybrid', {
            params: {
                searchText,
                reference,
                referenceType,
                limit,
                queryTextVector,
                similarityThreshold,
            },
            headers: {
                Authorization: `Bearer ${token}`,
            },
        });

        console.log('混合检索成功:', response.data);
        return response.data;
    } catch (error) {
        console.error('混合检索失败:', error.response?.data || error.message);
        throw error;
    }
};
```

---

### 16. 设置权限接口

#### 接口信息

-   **接口路径**: `PUT /api/app/attachment/permissions/set`
-   **接口描述**: 设置附件目录的权限
-   **请求方式**: PUT
-   **Content-Type**: application/json

#### 请求参数

**查询参数**:

| 参数名 | 类型 | 必填 | 描述    | 示例值                                 |
| ------ | ---- | ---- | ------- | -------------------------------------- |
| id     | Guid | 是   | 目录 ID | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |

**请求体**: `[AttachCatalogueTemplatePermissionDto](#attachcataloguetemplatepermissiondto)[]`

#### React Axios 调用示例

```javascript
const setPermissions = async (catalogueId, permissions) => {
    try {
        await axios.put(
            '/api/app/attachment/permissions/set',
            [
                {
                    permissionType: 'User',
                    permissionTarget: 'user123',
                    action: 'Read',
                    effect: 'Allow',
                    description: '用户读取权限',
                },
            ],
            {
                params: {
                    id: catalogueId,
                },
                headers: {
                    'Content-Type': 'application/json',
                    Authorization: `Bearer ${token}`,
                },
            }
        );

        console.log('设置权限成功');
    } catch (error) {
        console.error('设置权限失败:', error.response?.data || error.message);
        throw error;
    }
};
```

---

### 17. 获取权限接口

#### 接口信息

-   **接口路径**: `GET /api/app/attachment/permissions/get`
-   **接口描述**: 获取附件目录的权限列表
-   **请求方式**: GET

#### 请求参数

**查询参数**:

| 参数名 | 类型 | 必填 | 描述    | 示例值                                 |
| ------ | ---- | ---- | ------- | -------------------------------------- |
| id     | Guid | 是   | 目录 ID | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |

#### React Axios 调用示例

```javascript
const getPermissions = async (catalogueId) => {
    try {
        const response = await axios.get(
            '/api/app/attachment/permissions/get',
            {
                params: {
                    id: catalogueId,
                },
                headers: {
                    Authorization: `Bearer ${token}`,
                },
            }
        );

        console.log('获取权限成功:', response.data);
        return response.data;
    } catch (error) {
        console.error('获取权限失败:', error.response?.data || error.message);
        throw error;
    }
};
```

---

### 18. 检查权限接口

#### 接口信息

-   **接口路径**: `GET /api/app/attachment/permissions/check`
-   **接口描述**: 检查用户是否具有指定权限
-   **请求方式**: GET

#### 请求参数

**查询参数**:

| 参数名 | 类型             | 必填 | 描述     | 示例值                                 |
| ------ | ---------------- | ---- | -------- | -------------------------------------- |
| id     | Guid             | 是   | 目录 ID  | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |
| userId | Guid             | 是   | 用户 ID  | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |
| action | PermissionAction | 是   | 权限动作 | "Read"                                 |

#### React Axios 调用示例

```javascript
const hasPermission = async (catalogueId, userId, action) => {
    try {
        const response = await axios.get(
            '/api/app/attachment/permissions/check',
            {
                params: {
                    id: catalogueId,
                    userId: userId,
                    action: action,
                },
                headers: {
                    Authorization: `Bearer ${token}`,
                },
            }
        );

        console.log('检查权限成功:', response.data);
        return response.data;
    } catch (error) {
        console.error('检查权限失败:', error.response?.data || error.message);
        throw error;
    }
};
```

---

### 19. 获取目录标识描述接口

#### 接口信息

-   **接口路径**: `GET /api/app/attachment/identifier/description`
-   **接口描述**: 获取附件目录的标识描述
-   **请求方式**: GET

#### 请求参数

**查询参数**:

| 参数名 | 类型 | 必填 | 描述    | 示例值                                 |
| ------ | ---- | ---- | ------- | -------------------------------------- |
| id     | Guid | 是   | 目录 ID | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |

#### React Axios 调用示例

```javascript
const getCatalogueIdentifierDescription = async (catalogueId) => {
    try {
        const response = await axios.get(
            '/api/app/attachment/identifier/description',
            {
                params: {
                    id: catalogueId,
                },
                headers: {
                    Authorization: `Bearer ${token}`,
                },
            }
        );

        console.log('获取标识描述成功:', response.data);
        return response.data;
    } catch (error) {
        console.error(
            '获取标识描述失败:',
            error.response?.data || error.message
        );
        throw error;
    }
};
```

---

### 20. 根据标识查询目录接口

#### 接口信息

-   **接口路径**: `GET /api/app/attachment/search/by-identifier`
-   **接口描述**: 根据目录标识查询附件目录
-   **请求方式**: GET

#### 请求参数

**查询参数**:

| 参数名             | 类型            | 必填 | 描述         | 示例值 |
| ------------------ | --------------- | ---- | ------------ | ------ |
| catalogueFacetType | FacetType       | 否   | 分类分面类型 | 0      |
| cataloguePurpose   | TemplatePurpose | 否   | 分类用途     | 1      |

#### React Axios 调用示例

```javascript
const getByCatalogueIdentifier = async (
    catalogueFacetType = null,
    cataloguePurpose = null
) => {
    try {
        const response = await axios.get(
            '/api/app/attachment/search/by-identifier',
            {
                params: {
                    catalogueFacetType,
                    cataloguePurpose,
                },
                headers: {
                    Authorization: `Bearer ${token}`,
                },
            }
        );

        console.log('根据标识查询成功:', response.data);
        return response.data;
    } catch (error) {
        console.error(
            '根据标识查询失败:',
            error.response?.data || error.message
        );
        throw error;
    }
};
```

---

### 21. 根据向量维度查询目录接口

#### 接口信息

-   **接口路径**: `GET /api/app/attachment/search/by-vector-dimension`
-   **接口描述**: 根据向量维度查询附件目录
-   **请求方式**: GET

#### 请求参数

**查询参数**:

| 参数名       | 类型 | 必填 | 描述     | 示例值 |
| ------------ | ---- | ---- | -------- | ------ |
| minDimension | int  | 否   | 最小维度 | 64     |
| maxDimension | int  | 否   | 最大维度 | 2048   |

#### React Axios 调用示例

```javascript
const getByVectorDimension = async (
    minDimension = null,
    maxDimension = null
) => {
    try {
        const response = await axios.get(
            '/api/app/attachment/search/by-vector-dimension',
            {
                params: {
                    minDimension,
                    maxDimension,
                },
                headers: {
                    Authorization: `Bearer ${token}`,
                },
            }
        );

        console.log('根据向量维度查询成功:', response.data);
        return response.data;
    } catch (error) {
        console.error(
            '根据向量维度查询失败:',
            error.response?.data || error.message
        );
        throw error;
    }
};
```

---

### 22. 批量设置元数据字段接口

#### 接口信息

-   **接口路径**: `PUT /api/app/attachment/metafields/set`
-   **接口描述**: 批量设置附件目录的元数据字段（创建、更新、删除），基于行业最佳实践实现
-   **请求方式**: PUT
-   **Content-Type**: application/json

#### 请求参数

**查询参数**:

| 参数名 | 类型 | 必填 | 描述    | 示例值                                 |
| ------ | ---- | ---- | ------- | -------------------------------------- |
| id     | Guid | 是   | 目录 ID | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |

**请求体**: `[CreateUpdateMetaFieldDto](#createupdatemetafielddto-用于创建和更新)[]`

#### 功能特点

-   **批量操作**: 一次请求可以完成多个元数据字段的创建、更新和删除操作
-   **数据一致性**: 确保所有元数据字段的变更在同一事务中完成
-   **性能优化**: 减少网络请求次数，提高系统性能
-   **字段验证**: 自动验证字段键名唯一性和数据格式
-   **原子性**: 要么全部成功，要么全部失败，保证数据完整性

#### 操作说明

1. **创建新字段**: 在请求体中包含新的元数据字段
2. **更新现有字段**: 在请求体中包含需要更新的字段（保持相同的 fieldKey）
3. **删除字段**: 从请求体中移除不需要的字段
4. **字段键名唯一性**: 系统会自动验证字段键名的唯一性

#### React Axios 调用示例

```javascript
const setMetaFields = async (catalogueId, metaFields) => {
    try {
        const response = await axios.put(
            '/api/app/attachment/metafields/set',
            [
                {
                    entityType: 'Project',
                    fieldKey: 'project_name',
                    fieldName: '项目名称',
                    dataType: 'string',
                    unit: null,
                    isRequired: true,
                    regexPattern: '^[A-Za-z0-9\\s]+$',
                    options: null,
                    description: '项目名称字段',
                    defaultValue: '',
                    order: 1,
                    isEnabled: true,
                    group: '基本信息',
                    validationRules: '{"minLength":2,"maxLength":200}',
                    tags: ['重要', '必填'],
                },
                {
                    entityType: 'Project',
                    fieldKey: 'project_budget',
                    fieldName: '项目预算',
                    dataType: 'number',
                    unit: '万元',
                    isRequired: false,
                    regexPattern: null,
                    options: null,
                    description: '项目预算字段',
                    defaultValue: '0',
                    order: 2,
                    isEnabled: true,
                    group: '财务信息',
                    validationRules: '{"min":0,"max":10000}',
                    tags: ['财务', '预算'],
                },
                {
                    entityType: 'Project',
                    fieldKey: 'project_status',
                    fieldName: '项目状态',
                    dataType: 'string',
                    unit: null,
                    isRequired: true,
                    regexPattern: null,
                    options: '["进行中","已完成","已暂停","已取消"]',
                    description: '项目当前状态',
                    defaultValue: '进行中',
                    order: 3,
                    isEnabled: true,
                    group: '状态信息',
                    validationRules: null,
                    tags: ['状态', '必填'],
                },
            ],
            {
                params: {
                    id: catalogueId,
                },
                headers: {
                    'Content-Type': 'application/json',
                    Authorization: `Bearer ${token}`,
                },
            }
        );

        console.log('批量设置元数据字段成功:', response.data);
        return response.data;
    } catch (error) {
        console.error(
            '批量设置元数据字段失败:',
            error.response?.data || error.message
        );
        throw error;
    }
};

// 使用示例
const catalogueId = '3fa85f64-5717-4562-b3fc-2c963f66afa6';
const metaFields = [
    // 项目基本信息字段
    {
        entityType: 'Project',
        fieldKey: 'project_name',
        fieldName: '项目名称',
        dataType: 'string',
        isRequired: true,
        order: 1,
        isEnabled: true,
        group: '基本信息',
        tags: ['重要', '必填'],
    },
    // 项目财务字段
    {
        entityType: 'Project',
        fieldKey: 'project_budget',
        fieldName: '项目预算',
        dataType: 'number',
        unit: '万元',
        isRequired: false,
        order: 2,
        isEnabled: true,
        group: '财务信息',
        tags: ['财务'],
    },
];

setMetaFields(catalogueId, metaFields);
```

---

### 23. 获取元数据字段接口

#### 接口信息

-   **接口路径**: `GET /api/app/attachment/metafields/get`
-   **接口描述**: 获取指定附件目录的特定元数据字段
-   **请求方式**: GET

#### 请求参数

**查询参数**:

| 参数名   | 类型   | 必填 | 描述     | 示例值                                 |
| -------- | ------ | ---- | -------- | -------------------------------------- |
| id       | Guid   | 是   | 目录 ID  | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |
| fieldKey | string | 是   | 字段键名 | "project_name"                         |

#### React Axios 调用示例

```javascript
const getMetaField = async (catalogueId, fieldKey) => {
    try {
        const response = await axios.get('/api/app/attachment/metafields/get', {
            params: {
                id: catalogueId,
                fieldKey: fieldKey,
            },
            headers: {
                Authorization: `Bearer ${token}`,
            },
        });

        console.log('获取元数据字段成功:', response.data);
        return response.data;
    } catch (error) {
        console.error(
            '获取元数据字段失败:',
            error.response?.data || error.message
        );
        throw error;
    }
};
```

---

### 24. 获取启用的元数据字段接口

#### 接口信息

-   **接口路径**: `GET /api/app/attachment/metafields/enabled`
-   **接口描述**: 获取指定附件目录的所有启用元数据字段
-   **请求方式**: GET

#### 请求参数

**查询参数**:

| 参数名 | 类型 | 必填 | 描述    | 示例值                                 |
| ------ | ---- | ---- | ------- | -------------------------------------- |
| id     | Guid | 是   | 目录 ID | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |

#### React Axios 调用示例

```javascript
const getEnabledMetaFields = async (catalogueId) => {
    try {
        const response = await axios.get(
            '/api/app/attachment/metafields/enabled',
            {
                params: {
                    id: catalogueId,
                },
                headers: {
                    Authorization: `Bearer ${token}`,
                },
            }
        );

        console.log('获取启用的元数据字段成功:', response.data);
        return response.data;
    } catch (error) {
        console.error(
            '获取启用的元数据字段失败:',
            error.response?.data || error.message
        );
        throw error;
    }
};
```

---

### 25. 根据模板查询目录接口

#### 接口信息

-   **接口路径**: `GET /api/app/attachment/search/by-template`
-   **接口描述**: 根据模板 ID 和版本查询附件目录
-   **请求方式**: GET

#### 请求参数

**查询参数**:

| 参数名          | 类型 | 必填 | 描述     | 示例值                                 |
| --------------- | ---- | ---- | -------- | -------------------------------------- |
| templateId      | Guid | 是   | 模板 ID  | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |
| templateVersion | int  | 否   | 模板版本 | 1                                      |

#### React Axios 调用示例

```javascript
const findByTemplate = async (templateId, templateVersion = null) => {
    try {
        const response = await axios.get(
            '/api/app/attachment/search/by-template',
            {
                params: {
                    templateId: templateId,
                    templateVersion: templateVersion,
                },
                headers: {
                    Authorization: `Bearer ${token}`,
                },
            }
        );

        console.log('根据模板查询成功:', response.data);
        return response.data;
    } catch (error) {
        console.error(
            '根据模板查询失败:',
            error.response?.data || error.message
        );
        throw error;
    }
};
```

---

### 26. 根据模板 ID 查询目录接口

#### 接口信息

-   **接口路径**: `GET /api/app/attachment/search/by-template-id`
-   **接口描述**: 根据模板 ID 查询附件目录（不限制版本）
-   **请求方式**: GET

#### 请求参数

**查询参数**:

| 参数名     | 类型 | 必填 | 描述    | 示例值                                 |
| ---------- | ---- | ---- | ------- | -------------------------------------- |
| templateId | Guid | 是   | 模板 ID | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |

#### React Axios 调用示例

```javascript
const findByTemplateId = async (templateId) => {
    try {
        const response = await axios.get(
            '/api/app/attachment/search/by-template-id',
            {
                params: {
                    templateId: templateId,
                },
                headers: {
                    Authorization: `Bearer ${token}`,
                },
            }
        );

        console.log('根据模板ID查询成功:', response.data);
        return response.data;
    } catch (error) {
        console.error(
            '根据模板ID查询失败:',
            error.response?.data || error.message
        );
        throw error;
    }
};
```

---

### 27. 获取分类树形结构接口

#### 接口信息

-   **接口路径**: `GET /api/app/attachment/tree`
-   **接口描述**: 获取分类树形结构（用于树状展示），基于行业最佳实践，支持多种查询条件和性能优化
-   **请求方式**: GET

#### 请求参数

**查询参数**:

| 参数名             | 类型            | 必填 | 描述             | 示例值                                 |
| ------------------ | --------------- | ---- | ---------------- | -------------------------------------- |
| reference          | string          | 否   | 业务引用         | "CONTRACT_001"                         |
| referenceType      | int             | 否   | 业务类型         | 1                                      |
| catalogueFacetType | FacetType       | 否   | 分类分面类型     | 0                                      |
| cataloguePurpose   | TemplatePurpose | 否   | 分类用途         | 1                                      |
| includeChildren    | boolean         | 否   | 是否包含子节点   | true                                   |
| includeFiles       | boolean         | 否   | 是否包含附件文件 | false                                  |
| fulltextQuery      | string          | 否   | 全文搜索查询     | "合同"                                 |
| templateId         | Guid            | 否   | 模板 ID 过滤     | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |
| templateVersion    | int             | 否   | 模板版本过滤     | 1                                      |

#### React Axios 调用示例

```javascript
const getCataloguesTree = async (
    reference = null,
    referenceType = null,
    catalogueFacetType = null,
    cataloguePurpose = null,
    includeChildren = true,
    includeFiles = false,
    fulltextQuery = null,
    templateId = null,
    templateVersion = null
) => {
    try {
        const response = await axios.get('/api/app/attachment/tree', {
            params: {
                reference,
                referenceType,
                catalogueFacetType,
                cataloguePurpose,
                includeChildren,
                includeFiles,
                fulltextQuery,
                templateId,
                templateVersion,
            },
            headers: {
                Authorization: `Bearer ${token}`,
            },
        });

        console.log('获取分类树形结构成功:', response.data);
        return response.data;
    } catch (error) {
        console.error(
            '获取分类树形结构失败:',
            error.response?.data || error.message
        );
        throw error;
    }
};
```

#### 返回类型

**成功响应** (200 OK):

返回 `List<AttachCatalogueTreeDto>` 类型的分类树形结构列表。

**AttachCatalogueTreeDto 字段说明**:

| 字段名                         | 类型                                                                                | 必填 | 描述                                    | 示例值                                 |
| ------------------------------ | ----------------------------------------------------------------------------------- | ---- | --------------------------------------- | -------------------------------------- |
| id                             | Guid                                                                                | 是   | 分类 ID                                 | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |
| reference                      | string                                                                              | 是   | 业务引用                                | "CONTRACT_001"                         |
| attachReceiveType              | [AttachReceiveType](#attachreceivetype)                                             | 是   | 附件收取类型                            | 2                                      |
| referenceType                  | int                                                                                 | 是   | 业务类型标识                            | 1                                      |
| catalogueName                  | string                                                                              | 是   | 分类名称                                | "合同文档分类"                         |
| tags                           | List<string>                                                                        | 否   | 分类标签                                | ["合同", "法律", "重要"]               |
| sequenceNumber                 | int                                                                                 | 是   | 顺序号                                  | 100                                    |
| parentId                       | Guid?                                                                               | 否   | 父分类 ID                               | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |
| isRequired                     | bool                                                                                | 是   | 是否必收                                | true                                   |
| attachCount                    | int                                                                                 | 是   | 附件数量                                | 5                                      |
| pageCount                      | int                                                                                 | 是   | 页数                                    | 10                                     |
| isStatic                       | bool                                                                                | 是   | 静态标识                                | false                                  |
| isVerification                 | bool                                                                                | 是   | 是否核验                                | false                                  |
| verificationPassed             | bool                                                                                | 是   | 核验通过                                | false                                  |
| children                       | List<AttachCatalogueTreeDto>                                                        | 否   | 子分类列表（树形结构）                  | []                                     |
| attachFiles                    | Collection<[AttachFileDto](#attachfiledto-附件文件信息)?                            | 否   | 附件文件集合（当 includeFiles=true 时） | null                                   |
| templateId                     | Guid?                                                                               | 否   | 关联的模板 ID                           | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |
| fullTextContent                | string?                                                                             | 否   | 全文内容                                | "合同文档内容..."                      |
| fullTextContentUpdatedTime     | DateTime?                                                                           | 否   | 全文内容更新时间                        | "2024-01-01T00:00:00Z"                 |
| catalogueFacetType             | [FacetType](#facettype)                                                             | 是   | 分类分面类型                            | 0                                      |
| cataloguePurpose               | [TemplatePurpose](#templatepurpose)                                                 | 是   | 分类用途                                | 1                                      |
| templateRole                   | [TemplateRole](#templaterole)                                                       | 是   | 分类角色                                | 3                                      |
| textVector                     | List<double>?                                                                       | 否   | 文本向量                                | [0.1, 0.2, 0.3]                        |
| vectorDimension                | int                                                                                 | 是   | 向量维度                                | 128                                    |
| path                           | string?                                                                             | 否   | 分类路径（用于快速查询层级）            | "0000001.0000002.0000003"              |
| permissions                    | List<[AttachCatalogueTemplatePermissionDto](#attachcataloguetemplatepermissiondto)> | 否   | 权限集合                                | []                                     |
| metaFields                     | List<[MetaFieldDto](#metafielddto-用于查询和返回)>                                  | 否   | 元数据字段集合                          | []                                     |
| catalogueIdentifierDescription | string                                                                              | 是   | 分类标识描述（计算属性）                | "General - Classification"             |
| creationTime                   | DateTime                                                                            | 是   | 创建时间                                | "2024-01-01T00:00:00Z"                 |
| lastModificationTime           | DateTime?                                                                           | 否   | 最后修改时间                            | "2024-01-01T00:00:00Z"                 |
| creatorId                      | Guid?                                                                               | 否   | 创建者 ID                               | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |
| lastModifierId                 | Guid?                                                                               | 否   | 最后修改者 ID                           | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |

**响应示例**:

```json
[
    {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "reference": "CONTRACT_001",
        "attachReceiveType": 2,
        "referenceType": 1,
        "catalogueName": "合同文档分类",
        "tags": ["合同", "法律", "重要"],
        "sequenceNumber": 100,
        "parentId": null,
        "isRequired": true,
        "attachCount": 5,
        "pageCount": 10,
        "isStatic": false,
        "isVerification": false,
        "verificationPassed": false,
        "children": [
            {
                "id": "4fa85f64-5717-4562-b3fc-2c963f66afa7",
                "reference": "CONTRACT_001",
                "attachReceiveType": 2,
                "referenceType": 1,
                "catalogueName": "合同正文",
                "tags": ["正文", "核心"],
                "sequenceNumber": 1,
                "parentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
                "isRequired": true,
                "attachCount": 2,
                "pageCount": 5,
                "isStatic": false,
                "isVerification": true,
                "verificationPassed": true,
                "children": [],
                "attachFiles": null,
                "templateId": "5fa85f64-5717-4562-b3fc-2c963f66afa8",
                "fullTextContent": "合同正文内容...",
                "fullTextContentUpdatedTime": "2024-01-01T00:00:00Z",
                "catalogueFacetType": 1,
                "cataloguePurpose": 1,
                "templateRole": 3,
                "textVector": [0.1, 0.2, 0.3],
                "vectorDimension": 128,
                "path": "0000001.0000002",
                "permissions": [],
                "metaFields": [],
                "catalogueIdentifierDescription": "Category - Classification",
                "creationTime": "2024-01-01T00:00:00Z",
                "lastModificationTime": "2024-01-01T00:00:00Z",
                "creatorId": "6fa85f64-5717-4562-b3fc-2c963f66afa9",
                "lastModifierId": "7fa85f64-5717-4562-b3fc-2c963f66afaa"
            }
        ],
        "attachFiles": null,
        "templateId": "8fa85f64-5717-4562-b3fc-2c963f66afab",
        "fullTextContent": "合同文档分类的全文内容...",
        "fullTextContentUpdatedTime": "2024-01-01T00:00:00Z",
        "catalogueFacetType": 0,
        "cataloguePurpose": 1,
        "templateRole": 1,
        "textVector": [0.1, 0.2, 0.3],
        "vectorDimension": 128,
        "path": "0000001",
        "permissions": [],
        "metaFields": [],
        "catalogueIdentifierDescription": "General - Classification",
        "creationTime": "2024-01-01T00:00:00Z",
        "lastModificationTime": "2024-01-01T00:00:00Z",
        "creatorId": "9fa85f64-5717-4562-b3fc-2c963f66afac",
        "lastModifierId": "afa85f64-5717-4562-b3fc-2c963f66afad"
    }
]
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

### 1. 创建目录时

-   确保 `catalogueName` 唯一且有意义
-   合理设置 `reference` 和 `referenceType` 用于业务关联
-   根据业务需求设置 `tags` 标签
-   合理设置 `isRequired` 和 `isStatic` 属性

### 2. 文件上传时

-   使用 `multipart/form-data` 格式上传文件
-   合理设置文件前缀 `prefix` 参数
-   处理大文件上传时的进度和错误

### 3. 搜索时

-   根据实际需求选择合适的搜索方式（全文检索、混合检索）
-   合理设置相似度阈值以获得最佳结果
-   使用过滤条件缩小搜索范围

### 4. 权限管理

-   合理设置目录权限
-   定期检查和更新权限配置
-   使用权限检查接口验证用户权限

### 5. 元数据字段管理

-   **批量操作优先**: 使用批量设置接口进行元数据字段的增删改操作，提高性能和确保数据一致性
-   **字段设计**: 合理设计元数据字段结构，确保字段键名唯一且有意义
-   **验证规则**: 根据业务需求设置字段验证规则和正则表达式
-   **字段分组**: 使用字段分组功能组织相关字段，提高可维护性
-   **状态管理**: 合理设置字段的启用/禁用状态，支持动态字段管理
-   **原子性操作**: 利用批量操作的原子性特性，确保数据完整性
-   **性能优化**: 避免频繁的单个字段操作，优先使用批量接口

### 6. 分类路径管理

-   **路径格式**: 分类路径采用 "0000001.0000002.0000003" 格式（7 位数字，用点分隔）
-   **自动生成**: 系统会自动为新建分类生成路径，无需手动指定
-   **层级查询**: 利用路径字段可以快速进行层级查询和树形结构构建
-   **性能优化**: 路径字段已建立索引，支持高效的层级查询操作
-   **路径验证**: 系统会自动验证路径格式的正确性
-   **父子关系**: 通过路径可以快速判断分类间的父子关系
-   **深度计算**: 使用路径可以快速计算分类的层级深度

## TemplateRole 详细说明

### 分类角色类型

**TemplateRole** 字段用于标识分类在层级结构中的角色，主要用于前端树状展示和动态分类树创建判断：

#### 1. Root (根分类) - 值: 1

-   **特点**: 顶级分类，没有父分类
-   **用途**: 可以作为根节点创建动态分类树
-   **应用场景**: 知识库根目录、档案管理根分类、项目根节点等
-   **权限**: 可以包含子分类，不能直接上传文件

#### 2. Navigation (导航分类) - 值: 2

-   **特点**: 纯导航作用，不参与动态分类树创建
-   **用途**: 仅用于导航，前端展示但不作为树节点
-   **应用场景**: 侧边栏导航、面包屑导航、菜单项等
-   **权限**: 不能包含子分类，不能上传文件

#### 3. Branch (分支节点) - 值: 3

-   **特点**: 中间层节点，用于组织分类结构
-   **用途**: 可以有子节点，但不能直接上传文件
-   **应用场景**: 分类文件夹、组织架构中间层、知识库分类等
-   **权限**: 可以包含子分类，不能直接上传文件

#### 4. Leaf (叶子节点) - 值: 4

-   **特点**: 终端节点，用于存放具体内容
-   **用途**: 不能有子节点，但可以直接上传文件
-   **应用场景**: 具体文档分类、文件存储目录、内容分类等
-   **权限**: 不能包含子分类，可以直接上传文件

### 前端使用建议

1. **树状展示**: 根据 `templateRole` 字段决定节点的显示样式和交互行为
2. **文件上传**: 只有 `Leaf` 类型的分类才允许直接上传文件
3. **子分类创建**: 只有 `Root` 和 `Branch` 类型的分类才允许创建子分类
4. **导航功能**: `Navigation` 类型的分类仅用于导航，不参与业务逻辑

## 29. 智能分类文件上传接口

### 接口描述

基于 OCR 内容进行智能分类推荐的文件上传接口，适用于文件自动归类场景。该接口会：

1. 上传文件并保存到存储系统
2. 对支持的文件类型进行 OCR 处理
3. 基于 OCR 内容调用 AI 智能分类服务
4. 返回分类推荐结果和文件基本信息

### 请求信息

-   **URL**: `/api/app/attachment/smart-classification/upload`
-   **方法**: `POST`
-   **描述**: 智能分类文件上传和推荐

### 请求参数

| 参数名      | 类型   | 必填 | 位置  | 描述           | 示例值                                 |
| ----------- | ------ | ---- | ----- | -------------- | -------------------------------------- |
| catalogueId | Guid   | 是   | Query | 分类 ID        | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |
| prefix      | string | 否   | Query | 文件前缀       | "CONTRACT_2024"                        |
| files       | File[] | 是   | Form  | 上传的文件列表 | 多文件上传                             |

### 请求体说明

上传的文件会自动创建 `AttachFileCreateDto` 对象，包含以下字段：

| 字段名          | 类型   | 必填 | 描述                         | 示例值             |
| --------------- | ------ | ---- | ---------------------------- | ------------------ |
| fileAlias       | string | 是   | 文件别名（来自文件名）       | "contract_001.pdf" |
| documentContent | byte[] | 是   | 文件内容（二进制数据）       | 文件二进制数据     |
| sequenceNumber  | int?   | 否   | 序号（可选，为空时自动分配） | 1 或 null          |

### 返回类型

**成功响应** (200 OK):

返回 `List<SmartClassificationResultDto>` 类型的智能分类结果列表。

**SmartClassificationResultDto 字段说明**:

| 字段名              | 类型                                                                 | 必填 | 描述                     | 示例值                       |
| ------------------- | -------------------------------------------------------------------- | ---- | ------------------------ | ---------------------------- |
| fileInfo            | [AttachFileDto](#attachfiledto-附件文件信息)                         | 是   | 文件基本信息             | 见 AttachFileDto 说明        |
| classification      | [ClassificationResult](#classificationresult-分类推荐结果)           | 是   | 推荐分类结果             | 见 ClassificationResult 说明 |
| availableCategories | List<[CategoryOptionDto](#categoryoptiondto-分类选项)>               | 是   | 可选的分类列表           | 见 CategoryOptionDto 说明    |
| ocrContent          | string?                                                              | 否   | OCR 提取的文本内容       | "合同编号：CONTRACT_001..."  |
| processingTimeMs    | long                                                                 | 是   | 处理时间（毫秒）         | 1500                         |
| status              | [SmartClassificationStatus](#smartclassificationstatus-智能分类状态) | 是   | 处理状态                 | 0                            |
| errorMessage        | string?                                                              | 否   | 错误信息（如果处理失败） | "OCR 处理失败"               |

**ClassificationResult** (分类推荐结果):

| 字段名              | 类型   | 必填 | 描述         | 示例值     |
| ------------------- | ------ | ---- | ------------ | ---------- |
| recommendedCategory | string | 是   | 推荐分类名称 | "合同文档" |
| confidence          | double | 是   | 置信度       | 0.85       |

**CategoryOptionDto** (分类选项):

| 字段名   | 类型   | 必填 | 描述      | 示例值                                 |
| -------- | ------ | ---- | --------- | -------------------------------------- |
| id       | Guid   | 是   | 分类 ID   | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |
| name     | string | 是   | 分类名称  | "合同文档"                             |
| path     | string | 否   | 分类路径  | "0000001.0000002"                      |
| parentId | Guid?  | 否   | 父分类 ID | "3fa85f64-5717-4562-b3fc-2c963f66afa7" |

**SmartClassificationStatus** (智能分类状态):

| 值  | 名称                   | 描述               |
| --- | ---------------------- | ------------------ |
| 0   | Success                | 成功               |
| 1   | OcrFailed              | OCR 处理失败       |
| 2   | ClassificationFailed   | 分类推荐失败       |
| 3   | NoCategoriesAvailable  | 没有可用的分类选项 |
| 4   | FileNotSupportedForOcr | 文件不支持 OCR     |
| 99  | SystemError            | 系统错误           |

### 响应示例

```json
[
    {
        "fileInfo": {
            "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
            "fileAlias": "contract_001.pdf",
            "filePath": "https://example.com/host/attachment/3fa85f64-5717-4562-b3fc-2c963f66afa6_contract_001.pdf",
            "sequenceNumber": 1,
            "fileName": "contract_001.pdf",
            "fileType": "pdf",
            "fileSize": 1024000,
            "downloadTimes": 0,
            "attachCatalogueId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
            "reference": "CONTRACT_001",
            "templatePurpose": 1,
            "isCategorized": true
        },
        "classification": {
            "recommendedCategory": "合同文档",
            "confidence": 0.85
        },
        "availableCategories": [
            {
                "id": "3fa85f64-5717-4562-b3fc-2c963f66afa8",
                "name": "合同文档",
                "path": "0000001.0000002",
                "parentId": "3fa85f64-5717-4562-b3fc-2c963f66afa7"
            },
            {
                "id": "3fa85f64-5717-4562-b3fc-2c963f66afa9",
                "name": "技术文档",
                "path": "0000001.0000003",
                "parentId": "3fa85f64-5717-4562-b3fc-2c963f66afa7"
            }
        ],
        "ocrContent": "合同编号：CONTRACT_001\n甲方：ABC公司\n乙方：XYZ公司\n合同内容：...",
        "processingTimeMs": 1500,
        "status": 0,
        "errorMessage": null
    }
]
```

### 业务说明

1. **智能分类流程**:

    - 文件上传后自动进行 OCR 处理（支持 PDF、图片等格式）
    - 基于 OCR 内容调用 AI 智能分类服务
    - 返回推荐分类和置信度

2. **分类选项来源**:

    - 基于指定的分类 ID 查找所有子分类
    - 只使用叶子节点分类作为推荐选项
    - 确保分类推荐的准确性

3. **错误处理**:

    - 支持多种错误状态的详细反馈
    - 即使部分处理失败也会返回结果
    - 提供详细的错误信息用于调试

4. **性能优化**:

    - 支持批量文件处理
    - 异步处理提高响应速度
    - 详细的处理时间统计

5. **序号处理**:

    - 如果未指定序号，自动分配该分类下的下一个可用序号
    - 如果指定了序号，检查是否与现有文件重复
    - 序号重复时自动递增直到找到可用序号
    - 确保同一分类下文件序号的唯一性

## 30. 确定文件分类接口

### 接口描述

将文件归类到指定分类，并更新相关属性。该接口会：

1. 更新文件的分类 ID
2. 设置文件的归类状态为已归类
3. 从分类中继承相关属性（Reference、TemplatePurpose 等）
4. 处理 OCR 内容（如果提供或文件支持 OCR）

### 请求信息

-   **URL**: `/api/app/attachment/confirm-classification`
-   **方法**: `POST`
-   **描述**: 确定文件分类

### 请求参数

| 参数名      | 类型   | 必填 | 位置  | 描述                 | 示例值                                 |
| ----------- | ------ | ---- | ----- | -------------------- | -------------------------------------- |
| fileId      | Guid   | 是   | Query | 文件 ID              | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |
| catalogueId | Guid   | 是   | Query | 分类 ID              | "3fa85f64-5717-4562-b3fc-2c963f66afa7" |
| ocrContent  | string | 否   | Body  | OCR 全文内容（可选） | "合同编号：CONTRACT_001..."            |

### 返回类型

**成功响应** (200 OK):

返回 `AttachFileDto` 类型的更新后文件信息。

**AttachFileDto 字段说明**:

| 字段名            | 类型                                 | 必填 | 描述           | 示例值                                 |
| ----------------- | ------------------------------------ | ---- | -------------- | -------------------------------------- |
| id                | Guid                                 | 是   | 文件 ID        | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |
| fileAlias         | string                               | 是   | 文件别名       | "合同正文"                             |
| sequenceNumber    | int                                  | 是   | 序号           | 1                                      |
| filePath          | string                               | 是   | 文件路径       | "/host/attachment/contract_001.pdf"    |
| fileName          | string                               | 是   | 文件名称       | "contract_001.pdf"                     |
| fileType          | string                               | 是   | 文件类型       | "pdf"                                  |
| fileSize          | int                                  | 是   | 文件大小(字节) | 1024000                                |
| downloadTimes     | int                                  | 是   | 下载次数       | 5                                      |
| attachCatalogueId | Guid?                                | 否   | 关联分类 ID    | "3fa85f64-5717-4562-b3fc-2c963f66afa7" |
| reference         | string?                              | 否   | 业务引用       | "CONTRACT_001"                         |
| templatePurpose   | [TemplatePurpose](#templatepurpose)? | 否   | 模板用途       | 1                                      |
| isCategorized     | bool                                 | 是   | 是否已归类     | true                                   |

### 响应示例

```json
{
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "fileAlias": "contract_001.pdf",
    "filePath": "https://example.com/host/attachment/3fa85f64-5717-4562-b3fc-2c963f66afa6_contract_001.pdf",
    "sequenceNumber": 1,
    "fileName": "contract_001.pdf",
    "fileType": "pdf",
    "fileSize": 1024000,
    "downloadTimes": 0,
    "attachCatalogueId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
    "reference": "CONTRACT_001",
    "templatePurpose": 1,
    "isCategorized": true
}
```

### 业务说明

1. **文件分类更新**:

    - 更新文件的 `AttachCatalogueId` 属性
    - 设置 `IsCategorized` 为 `true`
    - 从分类中继承 `Reference` 和 `TemplatePurpose` 属性

2. **OCR 处理**:

    - 如果提供了 `ocrContent`，直接设置并标记为完成
    - 如果未提供但文件支持 OCR，自动进行 OCR 处理
    - 如果 OCR 处理失败，设置处理状态为失败

3. **错误处理**:

    - 文件不存在时抛出异常
    - 分类不存在时抛出异常
    - OCR 处理失败时记录警告但不影响分类操作

4. **数据一致性**:

    - 确保文件与分类的关联关系正确
    - 保持文件属性的完整性
    - 记录操作日志用于审计

````

## 31. 批量确定文件分类接口

### 接口描述

批量将多个文件归类到指定分类，并更新相关属性。该接口会：

1. 批量更新文件的分类 ID
2. 设置文件的归类状态为已归类
3. 从分类中继承相关属性（Reference、TemplatePurpose 等）
4. 处理 OCR 内容（如果提供或文件支持 OCR）
5. 支持部分成功，单个文件失败不影响其他文件处理

### 请求信息

-   **URL**: `/api/app/attachment/confirm-classifications`
-   **方法**: `POST`
-   **描述**: 批量确定文件分类

### 请求参数

**请求体**:

| 参数名  | 类型                                                      | 必填 | 描述                 | 示例值                                 |
| ------- | --------------------------------------------------------- | ---- | -------------------- | -------------------------------------- |
| requests | List<[ConfirmFileClassificationRequest](#confirmfileclassificationrequest-文件分类请求)> | 是   | 文件分类请求列表     | 见 ConfirmFileClassificationRequest 说明 |

**ConfirmFileClassificationRequest** (文件分类请求):

| 参数名      | 类型   | 必填 | 描述                 | 示例值                                 |
| ----------- | ------ | ---- | -------------------- | -------------------------------------- |
| fileId      | Guid   | 是   | 文件 ID              | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |
| catalogueId | Guid   | 是   | 分类 ID              | "3fa85f64-5717-4562-b3fc-2c963f66afa7" |
| ocrContent  | string | 否   | OCR 全文内容（可选） | "合同编号：CONTRACT_001..."            |

### 返回类型

**成功响应** (200 OK):

返回 `List<AttachFileDto>` 类型的更新后文件信息列表。

**AttachFileDto 字段说明**:

| 字段名            | 类型                                 | 必填 | 描述           | 示例值                                 |
| ----------------- | ------------------------------------ | ---- | -------------- | -------------------------------------- |
| id                | Guid                                 | 是   | 文件 ID        | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |
| fileAlias         | string                               | 是   | 文件别名       | "合同正文"                             |
| sequenceNumber    | int                                  | 是   | 序号           | 1                                      |
| filePath          | string                               | 是   | 文件路径       | "/host/attachment/contract_001.pdf"    |
| fileName          | string                               | 是   | 文件名称       | "contract_001.pdf"                     |
| fileType          | string                               | 是   | 文件类型       | "pdf"                                  |
| fileSize          | int                                  | 是   | 文件大小(字节) | 1024000                                |
| downloadTimes     | int                                  | 是   | 下载次数       | 5                                      |
| attachCatalogueId | Guid?                                | 否   | 关联分类 ID    | "3fa85f64-5717-4562-b3fc-2c963f66afa7" |
| reference         | string?                              | 否   | 业务引用       | "CONTRACT_001"                         |
| templatePurpose   | [TemplatePurpose](#templatepurpose)? | 否   | 模板用途       | 1                                      |
| isCategorized     | bool                                 | 是   | 是否已归类     | true                                   |

### 响应示例

```json
[
    {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "fileAlias": "contract_001.pdf",
        "filePath": "https://example.com/host/attachment/3fa85f64-5717-4562-b3fc-2c963f66afa6_contract_001.pdf",
        "sequenceNumber": 1,
        "fileName": "contract_001.pdf",
        "fileType": "pdf",
        "fileSize": 1024000,
        "downloadTimes": 0,
        "attachCatalogueId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
        "reference": "CONTRACT_001",
        "templatePurpose": 1,
        "isCategorized": true
    },
    {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa8",
        "fileAlias": "contract_002.pdf",
        "filePath": "https://example.com/host/attachment/3fa85f64-5717-4562-b3fc-2c963f66afa8_contract_002.pdf",
        "sequenceNumber": 2,
        "fileName": "contract_002.pdf",
        "fileType": "pdf",
        "fileSize": 2048000,
        "downloadTimes": 0,
        "attachCatalogueId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
        "reference": "CONTRACT_001",
        "templatePurpose": 1,
        "isCategorized": true
    }
]
````

### 业务说明

1. **批量处理**:

    - 支持一次处理多个文件的分类
    - 单个文件失败不影响其他文件处理
    - 返回成功处理的文件列表

2. **文件分类更新**:

    - 更新文件的 `AttachCatalogueId` 属性
    - 设置 `IsCategorized` 为 `true`
    - 从分类中继承 `Reference` 和 `TemplatePurpose` 属性

3. **OCR 处理**:

    - 如果提供了 `ocrContent`，直接设置并标记为完成
    - 如果未提供但文件支持 OCR，自动进行 OCR 处理
    - 如果 OCR 处理失败，设置处理状态为失败

4. **错误处理**:

    - 文件不存在时跳过该文件
    - 分类不存在时跳过该文件
    - 记录详细的错误日志
    - 返回成功处理的文件列表

5. **性能优化**:

    - 批量处理减少网络请求次数
    - 支持事务性处理（可选）
    - 详细的处理统计信息

### 使用示例

```javascript
// 批量确定文件分类
const requests = [
    {
        fileId: '3fa85f64-5717-4562-b3fc-2c963f66afa6',
        catalogueId: '3fa85f64-5717-4562-b3fc-2c963f66afa7',
        ocrContent: '合同编号：CONTRACT_001...',
    },
    {
        fileId: '3fa85f64-5717-4562-b3fc-2c963f66afa8',
        catalogueId: '3fa85f64-5717-4562-b3fc-2c963f66afa7',
    },
];

const response = await fetch('/api/app/attachment/confirm-classifications', {
    method: 'POST',
    headers: {
        'Content-Type': 'application/json',
    },
    body: JSON.stringify(requests),
});

const results = await response.json();
console.log(`成功处理 ${results.length} 个文件`);
```

## 32. 根据业务引用和模板用途获取文件列表接口

### 接口描述

根据业务引用（Reference）和模板用途（TemplatePurpose）查询未归档的文件列表。该接口用于获取特定业务场景下尚未分类的文件，便于后续的文件管理和分类操作。

### 请求信息

-   **URL**: `/api/app/attachment/files/by-reference-template`
-   **方法**: `GET`
-   **描述**: 根据业务引用和模板用途获取未归档文件列表

### 请求参数

| 参数名          | 类型                                | 必填 | 位置  | 描述     | 示例值         |
| --------------- | ----------------------------------- | ---- | ----- | -------- | -------------- |
| reference       | string                              | 是   | Query | 业务引用 | "CONTRACT_001" |
| templatePurpose | [TemplatePurpose](#templatepurpose) | 是   | Query | 模板用途 | 1              |

### 返回类型

**成功响应** (200 OK):

返回 `List<AttachFileDto>` 类型的文件列表。

**AttachFileDto 字段说明**:

| 字段名            | 类型                                 | 必填 | 描述           | 示例值                                 |
| ----------------- | ------------------------------------ | ---- | -------------- | -------------------------------------- |
| id                | Guid                                 | 是   | 文件 ID        | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |
| fileAlias         | string                               | 是   | 文件别名       | "合同正文"                             |
| sequenceNumber    | int                                  | 是   | 序号           | 1                                      |
| filePath          | string                               | 是   | 文件路径       | "/host/attachment/contract_001.pdf"    |
| fileName          | string                               | 是   | 文件名称       | "contract_001.pdf"                     |
| fileType          | string                               | 是   | 文件类型       | "pdf"                                  |
| fileSize          | int                                  | 是   | 文件大小(字节) | 1024000                                |
| downloadTimes     | int                                  | 是   | 下载次数       | 5                                      |
| attachCatalogueId | Guid?                                | 否   | 关联分类 ID    | "3fa85f64-5717-4562-b3fc-2c963f66afa7" |
| reference         | string?                              | 否   | 业务引用       | "CONTRACT_001"                         |
| templatePurpose   | [TemplatePurpose](#templatepurpose)? | 否   | 模板用途       | 1                                      |
| isCategorized     | bool                                 | 是   | 是否已归类     | false                                  |

### 响应示例

```json
[
    {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "fileAlias": "contract_001.pdf",
        "filePath": "https://example.com/host/attachment/3fa85f64-5717-4562-b3fc-2c963f66afa6_contract_001.pdf",
        "sequenceNumber": 1,
        "fileName": "contract_001.pdf",
        "fileType": "pdf",
        "fileSize": 1024000,
        "downloadTimes": 0,
        "attachCatalogueId": null,
        "reference": "CONTRACT_001",
        "templatePurpose": 1,
        "isCategorized": false
    },
    {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
        "fileAlias": "contract_002.pdf",
        "filePath": "https://example.com/host/attachment/3fa85f64-5717-4562-b3fc-2c963f66afa7_contract_002.pdf",
        "sequenceNumber": 2,
        "fileName": "contract_002.pdf",
        "fileType": "pdf",
        "fileSize": 2048000,
        "downloadTimes": 0,
        "attachCatalogueId": null,
        "reference": "CONTRACT_001",
        "templatePurpose": 1,
        "isCategorized": false
    }
]
```

### 业务说明

1. **查询条件**:

    - 必须提供 `reference` 和 `templatePurpose` 参数
    - 只返回 `IsCategorized = false` 的未归档文件
    - 按序号和创建时间排序

2. **使用场景**:

    - 获取特定业务场景下的待分类文件
    - 批量文件管理操作
    - 文件分类前的预览

3. **性能优化**:

    - 使用数据库索引进行高效查询
    - 只返回必要的文件信息
    - 支持分页（如需要）

4. **错误处理**:

    - 参数验证：业务引用不能为空
    - 异常处理：提供详细的错误信息
    - 日志记录：记录查询操作和结果

### 使用示例

```javascript
// 获取合同相关的未归档文件
const response = await fetch(
    '/api/app/attachment/files/by-reference-template?reference=CONTRACT_001&templatePurpose=1'
);
const files = await response.json();

// 处理文件列表
files.forEach((file) => {
    console.log(`文件: ${file.fileName}, 大小: ${file.fileSize} 字节`);
});
```

## 33. 根据业务引用和模板用途获取文件列表并进行智能分类推荐接口

### 接口描述

根据业务引用（Reference）和模板用途（TemplatePurpose）查询未归档的文件列表，并为每个文件提供智能分类推荐。该接口结合了文件查询和智能分类功能，适用于需要为文件提供分类建议的场景。

### 请求信息

-   **URL**: `/api/app/attachment/files/by-reference-template-with-classification`
-   **方法**: `GET`
-   **描述**: 根据业务引用和模板用途获取文件列表并进行智能分类推荐

### 请求参数

| 参数名          | 类型                                | 必填 | 位置  | 描述     | 示例值         |
| --------------- | ----------------------------------- | ---- | ----- | -------- | -------------- |
| reference       | string                              | 是   | Query | 业务引用 | "CONTRACT_001" |
| templatePurpose | [TemplatePurpose](#templatepurpose) | 是   | Query | 模板用途 | 1              |

### 返回类型

**成功响应** (200 OK):

返回 `List<SmartClassificationResultDto>` 类型的智能分类推荐结果列表。

**SmartClassificationResultDto 字段说明**:

| 字段名              | 类型                                                                   | 必填 | 描述                     | 示例值                             |
| ------------------- | ---------------------------------------------------------------------- | ---- | ------------------------ | ---------------------------------- |
| fileInfo            | [AttachFileDto](#attachfiledto-附件文件信息)                           | 是   | 文件基本信息             | 见 AttachFileDto 说明              |
| classification      | [ClassificationExtentResult](#classificationextentresult-分类推荐结果) | 是   | 推荐分类结果             | 见 ClassificationExtentResult 说明 |
| availableCategories | List<[CategoryOptionDto](#categoryoptiondto-分类选项)>                 | 是   | 可选的分类列表           | 见 CategoryOptionDto 说明          |
| ocrContent          | string?                                                                | 否   | OCR 提取的文本内容       | "合同编号：CONTRACT_001..."        |
| processingTimeMs    | long                                                                   | 是   | 处理时间（毫秒）         | 0                                  |
| status              | [SmartClassificationStatus](#smartclassificationstatus-智能分类状态)   | 是   | 处理状态                 | 0                                  |
| errorMessage        | string?                                                                | 否   | 错误信息（如果处理失败） | null                               |

**ClassificationExtentResult** (扩展分类推荐结果):

| 字段名                | 类型   | 必填 | 描述         | 示例值                                 |
| --------------------- | ------ | ---- | ------------ | -------------------------------------- |
| recommendedCategory   | string | 是   | 推荐分类名称 | "合同文档"                             |
| recommendedCategoryId | Guid   | 是   | 推荐分类 ID  | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |
| confidence            | double | 是   | 置信度       | 0.8                                    |

### 响应示例

```json
[
    {
        "fileInfo": {
            "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
            "fileAlias": "contract_001.pdf",
            "filePath": "https://example.com/host/attachment/3fa85f64-5717-4562-b3fc-2c963f66afa6_contract_001.pdf",
            "sequenceNumber": 1,
            "fileName": "contract_001.pdf",
            "fileType": "pdf",
            "fileSize": 1024000,
            "downloadTimes": 0,
            "attachCatalogueId": null,
            "reference": "CONTRACT_001",
            "templatePurpose": 1,
            "isCategorized": false
        },
        "classification": {
            "recommendedCategory": "合同文档",
            "recommendedCategoryId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
            "confidence": 0.8
        },
        "availableCategories": [
            {
                "id": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
                "name": "合同文档",
                "path": "0000001.0000002",
                "parentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
            },
            {
                "id": "3fa85f64-5717-4562-b3fc-2c963f66afa8",
                "name": "技术文档",
                "path": "0000001.0000003",
                "parentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
            }
        ],
        "ocrContent": "合同编号：CONTRACT_001...",
        "processingTimeMs": 0,
        "status": 0,
        "errorMessage": null
    }
]
```

### 业务说明

1. **功能特点**:

    - 查询指定业务引用和模板用途的未归档文件
    - 为每个文件提供智能分类推荐
    - 基于现有分类结构提供推荐选项

2. **分类推荐逻辑**:

    - 如果文件已有分类，使用现有分类并提高置信度
    - 如果文件未分类，使用默认分类并降低置信度
    - 提供所有可用的分类选项供用户选择

3. **使用场景**:

    - 文件分类前的预览和推荐
    - 批量文件分类管理
    - 智能分类辅助决策

4. **性能优化**:

    - 基于 Path 路径的高效分类查询
    - 单次数据库查询获取所有相关数据
    - 内存中构建分类推荐结果

### 使用示例

```javascript
// 获取合同相关的未归档文件并进行智能分类推荐
const response = await fetch(
    '/api/app/attachment/files/by-reference-template-with-classification?reference=CONTRACT_001&templatePurpose=1'
);
const results = await response.json();

// 处理智能分类结果
results.forEach((result) => {
    console.log(`文件: ${result.fileInfo.fileName}`);
    console.log(`推荐分类: ${result.classification.recommendedCategory}`);
    console.log(`置信度: ${result.classification.confidence}`);
    console.log(
        `可选分类: ${result.availableCategories.map((c) => c.name).join(', ')}`
    );
});
```

## 34. 智能分析分类信息接口

### 接口描述

基于分类下的文件内容，自动生成概要信息、分类标签、全文内容和元数据。该接口会：

1. 分析分类下的所有文件内容
2. 基于文件内容自动生成分类标签
3. 提取和汇总全文内容
4. 生成分类的概要信息
5. 更新分类的元数据字段

### 请求信息

-   **URL**: `/api/app/attachment/intelligent-analysis`
-   **方法**: `POST`
-   **描述**: 智能分析分类信息

### 请求参数

| 参数名      | 类型 | 必填 | 位置  | 描述                       | 示例值                                 |
| ----------- | ---- | ---- | ----- | -------------------------- | -------------------------------------- |
| id          | Guid | 是   | Query | 分类 ID                    | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |
| forceUpdate | bool | 否   | Query | 是否强制更新（默认 false） | false                                  |

### 返回类型

**成功响应** (200 OK):

返回 `IntelligentAnalysisResultDto` 类型的智能分析结果。

**IntelligentAnalysisResultDto 字段说明**:

| 字段名           | 类型                                                              | 必填 | 描述                     | 示例值                                 |
| ---------------- | ----------------------------------------------------------------- | ---- | ------------------------ | -------------------------------------- |
| catalogueId      | Guid                                                              | 是   | 分类 ID                  | "3a1c5fd7-26fa-d2a4-fc3f-45cc6b7f1644" |
| catalogueName    | string                                                            | 是   | 分类名称                 | "抵押登记 002"                         |
| status           | [AnalysisStatus](#analysisstatus-分析状态)                        | 是   | 分析状态                 | 0                                      |
| errorMessage     | string?                                                           | 否   | 错误信息（如果分析失败） | null                                   |
| processingTimeMs | long                                                              | 是   | 分析耗时（毫秒）         | 37814                                  |
| summaryAnalysis  | [SummaryAnalysisResult](#summaryanalysisresult-概要分析结果)?     | 否   | 概要分析结果             | 见 SummaryAnalysisResult 说明          |
| tagsAnalysis     | [TagsAnalysisResult](#tagsanalysisresult-标签分析结果)?           | 否   | 标签分析结果             | 见 TagsAnalysisResult 说明             |
| fullTextAnalysis | [FullTextAnalysisResult](#fulltextanalysisresult-全文分析结果)?   | 否   | 全文分析结果             | 见 FullTextAnalysisResult 说明         |
| metaDataAnalysis | [MetaDataAnalysisResult](#metadataanalysisresult-元数据分析结果)? | 否   | 元数据分析结果           | 见 MetaDataAnalysisResult 说明         |
| updatedFields    | List<string>                                                      | 是   | 更新的字段列表           | ["Summary", "Tags", "FullTextContent"] |
| statistics       | [AnalysisStatistics](#analysisstatistics-分析统计)                | 是   | 分析统计信息             | 见 AnalysisStatistics 说明             |

**AnalysisStatus** (分析状态):

| 值  | 名称           | 描述             |
| --- | -------------- | ---------------- |
| 0   | Success        | 成功             |
| 1   | PartialSuccess | 部分成功         |
| 2   | Failed         | 失败             |
| 3   | Skipped        | 跳过（无需分析） |

**SummaryAnalysisResult** (概要分析结果):

| 字段名           | 类型          | 必填 | 描述           | 示例值                         |
| ---------------- | ------------- | ---- | -------------- | ------------------------------ |
| originalSummary  | string?       | 否   | 原始概要信息   | null                           |
| generatedSummary | string?       | 否   | 生成的概要信息 | "文档内容重复强调"收件受理"... |
| isUpdated        | bool          | 是   | 是否已更新     | true                           |
| confidence       | float         | 是   | 分析置信度     | 0.9                            |
| keywords         | List<string>  | 是   | 提取的关键词   | ["收件受理"]                   |
| semanticVector   | List<double>? | 否   | 语义向量       | [-0.040063563734292984, ...]   |

**TagsAnalysisResult** (标签分析结果):

| 字段名         | 类型                      | 必填 | 描述           | 示例值            |
| -------------- | ------------------------- | ---- | -------------- | ----------------- |
| originalTags   | List<string>              | 是   | 原始标签列表   | []                |
| generatedTags  | List<string>              | 是   | 生成的标签列表 | ["收件受理"]      |
| isUpdated      | bool                      | 是   | 是否已更新     | true              |
| tagConfidences | Dictionary<string, float> | 是   | 标签置信度映射 | {"收件受理": 0.9} |

**FullTextAnalysisResult** (全文分析结果):

| 字段名               | 类型                                                             | 必填 | 描述               | 示例值                       |
| -------------------- | ---------------------------------------------------------------- | ---- | ------------------ | ---------------------------- |
| processedFilesCount  | int                                                              | 是   | 处理的文件数量     | 3                            |
| successfulFilesCount | int                                                              | 是   | 成功处理的文件数量 | 3                            |
| failedFilesCount     | int                                                              | 是   | 处理失败的文件数量 | 0                            |
| isUpdated            | bool                                                             | 是   | 是否已更新         | true                         |
| extractedTextLength  | int                                                              | 是   | 提取的文本长度     | 12                           |
| processingDetails    | List<[FileProcessingDetail](#fileprocessingdetail-文件处理详情)> | 是   | 文件处理详情列表   | 见 FileProcessingDetail 说明 |

**MetaDataAnalysisResult** (元数据分析结果):

| 字段名                   | 类型                                                   | 必填 | 描述                 | 示例值                   |
| ------------------------ | ------------------------------------------------------ | ---- | -------------------- | ------------------------ |
| originalMetaFieldsCount  | int                                                    | 是   | 原始元数据字段数量   | 1                        |
| generatedMetaFieldsCount | int                                                    | 是   | 生成的元数据字段数量 | 0                        |
| isUpdated                | bool                                                   | 是   | 是否已更新           | false                    |
| recognizedEntities       | List<[RecognizedEntity](#recognizedentity-识别的实体)> | 是   | 识别的实体列表       | 见 RecognizedEntity 说明 |
| generatedMetaFields      | List<[MetaFieldDto](#metafielddto-用于查询和返回)>     | 是   | 生成的元数据字段列表 | 见 MetaFieldDto 说明     |

**AnalysisStatistics** (分析统计):

| 字段名                   | 类型 | 必填 | 描述               | 示例值 |
| ------------------------ | ---- | ---- | ------------------ | ------ |
| totalProcessingTimeMs    | long | 是   | 总处理时间（毫秒） | 37814  |
| totalFilesProcessed      | int  | 是   | 总处理文件数量     | 3      |
| successfulFilesProcessed | int  | 是   | 成功处理文件数量   | 3      |
| totalExtractedTextLength | int  | 是   | 总提取文本长度     | 12     |
| generatedTagsCount       | int  | 是   | 生成标签数量       | 1      |
| recognizedEntitiesCount  | int  | 是   | 识别实体数量       | 0      |
| updatedFieldsCount       | int  | 是   | 更新字段数量       | 3      |

**FileProcessingDetail** (文件处理详情):

| 字段名              | 类型                                                       | 必填 | 描述             | 示例值                                 |
| ------------------- | ---------------------------------------------------------- | ---- | ---------------- | -------------------------------------- |
| fileId              | Guid                                                       | 是   | 文件 ID          | "3a1c64ed-9bd5-001d-d7bd-ab1622859886" |
| fileName            | string                                                     | 是   | 文件名称         | "抵押登记智能审核.png"                 |
| status              | [FileProcessingStatus](#fileprocessingstatus-文件处理状态) | 是   | 处理状态         | 0                                      |
| extractedTextLength | int                                                        | 是   | 提取文本长度     | 4                                      |
| extractedText       | string?                                                    | 否   | 提取的文本内容   | "收件受理"                             |
| processingTimeMs    | long                                                       | 是   | 处理时间（毫秒） | 11413                                  |
| errorMessage        | string?                                                    | 否   | 错误信息         | null                                   |

**FileProcessingStatus** (文件处理状态):

| 值  | 名称    | 描述               |
| --- | ------- | ------------------ |
| 0   | Success | 成功               |
| 1   | Failed  | 失败               |
| 2   | Skipped | 跳过（不支持 OCR） |

**RecognizedEntity** (识别的实体):

| 字段名          | 类型   | 必填 | 描述     | 示例值 |
| --------------- | ------ | ---- | -------- | ------ |
| name            | string | 是   | 实体名称 | "张三" |
| type            | string | 是   | 实体类型 | "人名" |
| confidence      | float  | 是   | 置信度   | 0.95   |
| occurrenceCount | int    | 是   | 出现次数 | 3      |

### 响应示例

```json
{
    "catalogueId": "3a1c5fd7-26fa-d2a4-fc3f-45cc6b7f1644",
    "catalogueName": "抵押登记002",
    "status": 0,
    "errorMessage": null,
    "processingTimeMs": 37814,
    "summaryAnalysis": {
        "originalSummary": null,
        "generatedSummary": "文档内容重复强调"收件受理"，核心内容为收件受理相关事宜。主要观点是聚焦于收件受理流程或状态，虽无更多具体内容描述，但重点在于该主题的重复提及，可能用于强调其重要性或作为某种流程提示。摘要需准确反映这一核心内容和重复强调的特点，保持逻辑清晰与语言流畅。",
        "isUpdated": true,
        "confidence": 0.9,
        "keywords": ["收件受理"],
        "semanticVector": [-0.040063563734292984, -0.055492572486400604, 0.05808568373322487, ...]
    },
    "tagsAnalysis": {
        "originalTags": [],
        "generatedTags": ["收件受理"],
        "isUpdated": true,
        "tagConfidences": {
            "收件受理": 0.9
        }
    },
    "fullTextAnalysis": {
        "processedFilesCount": 3,
        "successfulFilesCount": 3,
        "failedFilesCount": 0,
        "isUpdated": true,
        "extractedTextLength": 12,
        "processingDetails": [
            {
                "fileId": "3a1c64ed-9bd5-001d-d7bd-ab1622859886",
                "fileName": "抵押登记智能审核.png",
                "status": 0,
                "extractedTextLength": 4,
                "extractedText": "收件受理",
                "processingTimeMs": 11413,
                "errorMessage": null
            },
            {
                "fileId": "3a1c6503-04d9-6e91-e69b-b98545635e17",
                "fileName": "抵押登记智能审核.png",
                "status": 0,
                "extractedTextLength": 4,
                "extractedText": "收件受理",
                "processingTimeMs": 357,
                "errorMessage": null
            },
            {
                "fileId": "3a1c6517-b0e5-7d7d-5017-e248b496b88d",
                "fileName": "抵押登记智能审核.png",
                "status": 0,
                "extractedTextLength": 4,
                "extractedText": "收件受理",
                "processingTimeMs": 368,
                "errorMessage": null
            }
        ]
    },
    "metaDataAnalysis": {
        "originalMetaFieldsCount": 1,
        "generatedMetaFieldsCount": 0,
        "isUpdated": false,
        "recognizedEntities": [
            {
                "name": "张三",
                "type": "人名",
                "confidence": 0.95,
                "occurrenceCount": 3
            },
            {
                "name": "合同编号",
                "type": "标识符",
                "confidence": 0.88,
                "occurrenceCount": 1
            }
        ],
        "generatedMetaFields": []
    },
    "updatedFields": ["Summary", "Tags", "FullTextContent"],
    "statistics": {
        "totalProcessingTimeMs": 37814,
        "totalFilesProcessed": 3,
        "successfulFilesProcessed": 3,
        "totalExtractedTextLength": 12,
        "generatedTagsCount": 1,
        "recognizedEntitiesCount": 2,
        "updatedFieldsCount": 3
    }
}
```

### 业务说明

1. **智能分析流程**:

    - 扫描分类下的所有文件
    - 对支持的文件类型进行 OCR 处理
    - 基于文件内容进行智能分析
    - 生成分类标签和概要信息

2. **标签生成逻辑**:

    - 基于文件内容提取关键词
    - 使用 AI 算法生成相关标签
    - 过滤重复和无关标签
    - 按重要性排序标签

3. **概要信息生成**:

    - 分析文件内容的主题和类型
    - 生成简洁的分类描述
    - 突出分类的主要特征
    - 提供分类用途说明

4. **全文内容提取**:

    - 提取所有文件的文本内容
    - 合并和整理文本信息
    - 保持内容的完整性和可读性
    - 更新分类的全文内容字段

5. **错误处理**:

    - 支持部分成功的情况
    - 记录处理失败的文件
    - 提供详细的错误信息
    - 确保数据一致性

6. **性能优化**:

    - 异步处理提高响应速度
    - 批量处理减少资源消耗
    - 详细的处理统计信息
    - 支持增量更新

### 使用示例

```javascript
// 智能分析分类信息
const analyzeCatalogue = async (catalogueId, forceUpdate = false) => {
    try {
        const response = await axios.post(
            '/api/app/attachment/intelligent-analysis',
            null,
            {
                params: {
                    id: catalogueId,
                    forceUpdate: forceUpdate,
                },
                headers: {
                    Authorization: `Bearer ${token}`,
                },
            }
        );

        console.log('智能分析成功:', response.data);
        return response.data;
    } catch (error) {
        console.error('智能分析失败:', error.response?.data || error.message);
        throw error;
    }
};

// 使用示例
const catalogueId = '3fa85f64-5717-4562-b3fc-2c963f66afa6';
const result = await analyzeCatalogue(catalogueId, false);

console.log(`分析完成，处理了 ${result.processedFilesCount} 个文件`);
console.log(`生成的标签: ${result.generatedTags.join(', ')}`);
console.log(`概要信息: ${result.summary}`);
```

---

## 版本信息

-   **文档版本**: 1.5.15
-   **API 版本**: v1.5.15
-   **最后更新**: 2024-12-19
-   **维护人员**: 开发团队
-   **更新内容**: 新增智能分析分类信息接口，支持基于文件内容的自动标签生成、概要信息提取和全文内容分析

