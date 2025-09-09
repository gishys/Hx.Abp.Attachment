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

| 参数名             | 类型                       | 必填 | 描述          | 示例值                                 |
| ------------------ | -------------------------- | ---- | ------------- | -------------------------------------- |
| attachReceiveType  | AttachReceiveType          | 是   | 附件收取类型  | 2                                      |
| catalogueName      | string                     | 是   | 分类名称      | "合同文档分类"                         |
| tags               | string[]                   | 否   | 分类标签      | ["合同", "法律", "重要"]               |
| sequenceNumber     | int                        | 否   | 序号          | 100                                    |
| referenceType      | int                        | 是   | 业务类型标识  | 1                                      |
| reference          | string                     | 是   | 业务 Id       | "CONTRACT_001"                         |
| parentId           | Guid?                      | 否   | 父节点 Id     | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |
| isVerification     | boolean                    | 否   | 是否核验      | false                                  |
| verificationPassed | boolean                    | 否   | 核验通过      | false                                  |
| isRequired         | boolean                    | 是   | 是否必收      | true                                   |
| isStatic           | boolean                    | 否   | 静态标识      | false                                  |
| children           | AttachCatalogueCreateDto[] | 否   | 子文件夹      | []                                     |
| attachFiles        | AttachFileCreateDto[]      | 否   | 子文件        | []                                     |
| templateId         | Guid?                      | 否   | 关联的模板 ID | "3fa85f64-5717-4562-b3fc-2c963f66afa6" |
| catalogueFacetType | FacetType                  | 否   | 分类分面类型  | 0                                      |
| cataloguePurpose   | TemplatePurpose            | 否   | 分类用途      | 1                                      |
| textVector         | double[]                   | 否   | 文本向量      | null                                   |
| metaFields         | MetaFieldDto[]             | 否   | 元数据字段    | []                                     |

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
    "textVector": null,
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

**请求体**: `AttachCatalogueTemplatePermissionDto[]`

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

**请求体**: `CreateUpdateMetaFieldDto[]`

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

## 版本信息

-   **文档版本**: 1.0
-   **API 版本**: v1
-   **最后更新**: 2024-12-19
-   **维护人员**: 开发团队
