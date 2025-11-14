# MetaFieldPreset API 接口文档

## 概述

MetaFieldPreset API 提供了预设元数据内容的完整管理功能，包括创建、更新、删除、查询、搜索、推荐等功能。预设元数据内容用于快速创建分类模板，提高模板创建效率。

**基础路径**: `/api/app/meta-field-preset`

**认证**: 需要身份认证（Bearer Token）

---

## 1. 创建预设

创建新的预设元数据内容。

**接口**: `POST /api/app/meta-field-preset`

**请求头**:

```
Content-Type: application/json
Authorization: Bearer {token}
```

**请求体** (`CreateUpdateMetaFieldPresetDto`):

```json
{
    "presetName": "项目文档预设",
    "description": "适用于项目文档的元数据预设，包含项目基本信息字段",
    "tags": ["项目", "文档", "常用"],
    "metaFields": [
        {
            "entityType": "Project",
            "fieldKey": "project_name",
            "fieldName": "项目名称",
            "dataType": "string",
            "isRequired": true,
            "unit": null,
            "regexPattern": null,
            "options": null,
            "description": "项目名称字段",
            "defaultValue": null,
            "order": 1,
            "isEnabled": true,
            "group": "基本信息",
            "validationRules": "{\"minLength\":1,\"maxLength\":100}",
            "tags": ["重要", "必填"]
        },
        {
            "entityType": "Project",
            "fieldKey": "project_code",
            "fieldName": "项目编号",
            "dataType": "string",
            "isRequired": true,
            "unit": null,
            "regexPattern": "^[A-Z0-9]{6,12}$",
            "options": null,
            "description": "项目编号，格式：大写字母+数字，6-12位",
            "defaultValue": null,
            "order": 2,
            "isEnabled": true,
            "group": "基本信息",
            "validationRules": null,
            "tags": ["必填"]
        },
        {
            "entityType": "Project",
            "fieldKey": "project_budget",
            "fieldName": "项目预算",
            "dataType": "number",
            "isRequired": false,
            "unit": "万元",
            "regexPattern": null,
            "options": null,
            "description": "项目预算金额",
            "defaultValue": "0",
            "order": 3,
            "isEnabled": true,
            "group": "财务信息",
            "validationRules": "{\"min\":0,\"max\":999999}",
            "tags": null
        },
        {
            "entityType": "Project",
            "fieldKey": "project_status",
            "fieldName": "项目状态",
            "dataType": "select",
            "isRequired": true,
            "unit": null,
            "regexPattern": null,
            "options": "[\"进行中\",\"已完成\",\"已暂停\",\"已取消\"]",
            "description": "项目当前状态",
            "defaultValue": "进行中",
            "order": 4,
            "isEnabled": true,
            "group": "基本信息",
            "validationRules": null,
            "tags": ["必填"]
        }
    ],
    "businessScenarios": ["Project", "Document"],
    "applicableFacetTypes": [1, 2],
    "applicableTemplatePurposes": [1, 2],
    "sortOrder": 0
}
```

**响应** (`MetaFieldPresetDto`):

```json
{
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "presetName": "项目文档预设",
    "description": "适用于项目文档的元数据预设，包含项目基本信息字段",
    "tags": ["项目", "文档", "常用"],
    "metaFields": [
        {
            "entityType": "Project",
            "fieldKey": "project_name",
            "fieldName": "项目名称",
            "dataType": "string",
            "isRequired": true,
            "unit": null,
            "regexPattern": null,
            "options": null,
            "description": "项目名称字段",
            "defaultValue": null,
            "order": 1,
            "isEnabled": true,
            "group": "基本信息",
            "validationRules": "{\"minLength\":1,\"maxLength\":100}",
            "tags": ["重要", "必填"],
            "fieldValue": null
        }
    ],
    "businessScenarios": ["Project", "Document"],
    "applicableFacetTypes": [1, 2],
    "applicableTemplatePurposes": [1, 2],
    "usageCount": 0,
    "recommendationWeight": 0.5,
    "isEnabled": true,
    "isSystemPreset": false,
    "sortOrder": 0,
    "lastUsedTime": null
}
```

**状态码**:

-   `200 OK`: 创建成功
-   `400 Bad Request`: 请求参数错误（如预设名称已存在）
-   `401 Unauthorized`: 未授权

---

## 2. 更新预设

更新已存在的预设元数据内容。

**接口**: `PUT /api/app/meta-field-preset/{id}`

**路径参数**:

-   `id` (Guid): 预设 ID

**请求体**: 同创建预设（`CreateUpdateMetaFieldPresetDto`）

**响应**: 同创建预设（`MetaFieldPresetDto`）

**状态码**:

-   `200 OK`: 更新成功
-   `404 Not Found`: 预设不存在
-   `400 Bad Request`: 请求参数错误

**示例请求**:

```bash
PUT /api/app/meta-field-preset/3fa85f64-5717-4562-b3fc-2c963f66afa6
Content-Type: application/json

{
  "presetName": "项目文档预设（更新）",
  "description": "更新后的描述",
  "tags": ["项目", "文档", "常用", "已更新"],
  "metaFields": [...],
  "businessScenarios": ["Project"],
  "applicableFacetTypes": [1],
  "applicableTemplatePurposes": [1],
  "sortOrder": 1
}
```

---

## 3. 删除预设

删除指定的预设（系统预设不能删除）。

**接口**: `DELETE /api/app/meta-field-preset/{id}`

**路径参数**:

-   `id` (Guid): 预设 ID

**响应**: 无内容

**状态码**:

-   `200 OK`: 删除成功
-   `404 Not Found`: 预设不存在
-   `400 Bad Request`: 系统预设不能删除

**示例请求**:

```bash
DELETE /api/app/meta-field-preset/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

---

## 4. 根据 ID 获取预设

获取指定 ID 的预设详情。

**接口**: `GET /api/app/meta-field-preset/{id}`

**路径参数**:

-   `id` (Guid): 预设 ID

**响应**: `MetaFieldPresetDto`（同创建预设的响应）

**状态码**:

-   `200 OK`: 获取成功
-   `404 Not Found`: 预设不存在

**示例请求**:

```bash
GET /api/app/meta-field-preset/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

---

## 5. 获取所有预设

获取所有启用的预设列表。

**接口**: `GET /api/app/meta-field-preset`

**响应**: `List<MetaFieldPresetDto>`

**状态码**:

-   `200 OK`: 获取成功

**示例响应**:

```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "presetName": "项目文档预设",
    "description": "适用于项目文档的元数据预设",
    "tags": ["项目", "文档"],
    "metaFields": [...],
    "businessScenarios": ["Project"],
    "applicableFacetTypes": [1],
    "applicableTemplatePurposes": [1],
    "usageCount": 15,
    "recommendationWeight": 0.75,
    "isEnabled": true,
    "isSystemPreset": false,
    "sortOrder": 0,
    "lastUsedTime": "2024-01-14T15:20:00Z"
  }
]
```

---

## 6. 搜索预设

根据关键词、标签、业务场景等条件搜索预设。

**接口**: `POST /api/app/meta-field-preset/search`

**请求体** (`PresetSearchRequestDto`):

```json
{
    "keyword": "项目",
    "tags": ["文档", "常用"],
    "businessScenario": "Project",
    "facetType": 1,
    "templatePurpose": 1,
    "onlyEnabled": true,
    "maxResults": 50,
    "skipCount": 0
}
```

**字段说明**:

-   `keyword` (string, 可选): 搜索关键词，匹配预设名称或描述
-   `tags` (List<string>, 可选): 标签列表，匹配包含任一标签的预设
-   `businessScenario` (string, 可选): 业务场景，匹配包含该业务场景的预设
-   `facetType` (FacetType, 可选): 分面类型枚举值（1=General, 2=Discipline, 3=Classification, 4=Document, 99=Other）
-   `templatePurpose` (TemplatePurpose, 可选): 模板用途枚举值（1=Classification, 2=Document, 3=Workflow, 4=Archive, 99=Other）
-   `onlyEnabled` (bool, 默认 true): 是否只返回启用的预设
-   `maxResults` (int, 默认 50): 最大返回数量
-   `skipCount` (int, 默认 0): 跳过数量（用于分页）

**响应**: `List<MetaFieldPresetDto>`

**状态码**:

-   `200 OK`: 搜索成功

**示例请求**:

```bash
POST /api/app/meta-field-preset/search
Content-Type: application/json

{
  "keyword": "项目",
  "tags": ["文档"],
  "onlyEnabled": true,
  "maxResults": 20
}
```

---

## 7. 获取推荐预设

根据业务场景、分面类型、模板用途等条件获取推荐的预设。

**接口**: `POST /api/app/meta-field-preset/recommendations`

**请求体** (`PresetRecommendationRequestDto`):

```json
{
    "businessScenario": "Project",
    "facetType": 1,
    "templatePurpose": 1,
    "tags": ["常用"],
    "topN": 10,
    "minWeight": 0.3,
    "onlyEnabled": true
}
```

**字段说明**:

-   `businessScenario` (string, 可选): 业务场景
-   `facetType` (FacetType, 可选): 分面类型
-   `templatePurpose` (TemplatePurpose, 可选): 模板用途
-   `tags` (List<string>, 可选): 标签列表
-   `topN` (int, 默认 10): 返回数量
-   `minWeight` (double, 默认 0.3): 最小推荐权重（0.0-1.0）
-   `onlyEnabled` (bool, 默认 true): 是否只返回启用的预设

**响应** (`List<PresetRecommendationDto>`):

```json
[
  {
    "preset": {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "presetName": "项目文档预设",
      "description": "适用于项目文档的元数据预设",
      "tags": ["项目", "文档", "常用"],
      "metaFields": [...],
      "businessScenarios": ["Project"],
      "applicableFacetTypes": [1],
      "applicableTemplatePurposes": [1],
      "usageCount": 25,
      "recommendationWeight": 0.85,
      "isEnabled": true,
      "isSystemPreset": false,
      "sortOrder": 0,
      "lastUsedTime": "2024-01-14T15:20:00Z"
    },
    "score": 0.92,
    "reasons": [
      "已被使用 25 次",
      "适用于业务场景：Project",
      "适用于分面类型：General",
      "适用于模板用途：Classification",
      "标签：项目, 文档, 常用"
    ]
  }
]
```

**字段说明**:

-   `preset`: 预设信息
-   `score`: 推荐分数（0.0-1.0），分数越高推荐度越高
-   `reasons`: 推荐原因列表

**状态码**:

-   `200 OK`: 获取成功

---

## 8. 获取热门预设

获取使用次数最多的预设。

**接口**: `GET /api/app/meta-field-preset/popular`

**查询参数**:

-   `topN` (int, 默认 10): 返回数量

**响应**: `List<MetaFieldPresetDto>`

**状态码**:

-   `200 OK`: 获取成功

**示例请求**:

```bash
GET /api/app/meta-field-preset/popular?topN=5
```

**示例响应**:

```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "presetName": "项目文档预设",
    "usageCount": 125,
    "recommendationWeight": 0.85,
    ...
  }
]
```

---

## 9. 启用预设

启用指定的预设。

**接口**: `POST /api/app/meta-field-preset/{id}/enable`

**路径参数**:

-   `id` (Guid): 预设 ID

**响应**: 无内容

**状态码**:

-   `200 OK`: 启用成功
-   `404 Not Found`: 预设不存在

**示例请求**:

```bash
POST /api/app/meta-field-preset/3fa85f64-5717-4562-b3fc-2c963f66afa6/enable
```

---

## 10. 禁用预设

禁用指定的预设（系统预设不能禁用）。

**接口**: `POST /api/app/meta-field-preset/{id}/disable`

**路径参数**:

-   `id` (Guid): 预设 ID

**响应**: 无内容

**状态码**:

-   `200 OK`: 禁用成功
-   `404 Not Found`: 预设不存在
-   `400 Bad Request`: 系统预设不能禁用

**示例请求**:

```bash
POST /api/app/meta-field-preset/3fa85f64-5717-4562-b3fc-2c963f66afa6/disable
```

---

## 11. 应用预设到模板

将预设的元数据字段应用到指定的模板，并同步更新模板的元数据字段集合，同时记录预设使用次数。

**接口**: `POST /api/app/meta-field-preset/{presetId}/apply-to-template/{templateId}`

**路径参数**:

-   `presetId` (Guid): 预设 ID
-   `templateId` (Guid): 模板 ID

**说明**:
- 此接口会将预设中的所有元数据字段应用到模板的最新版本
- 模板的元数据字段会被完全替换为预设中的元数据字段
- 模板的其他属性（名称、描述、标签等）保持不变
- 预设的使用次数会自动增加

**响应**: `List<MetaFieldDto>`

**状态码**:

-   `200 OK`: 应用成功
-   `404 Not Found`: 预设或模板不存在
-   `400 Bad Request`: 应用失败（如模板验证失败）

**示例请求**:

```bash
POST /api/app/meta-field-preset/3fa85f64-5717-4562-b3fc-2c963f66afa6/apply-to-template/7fa85f64-5717-4562-b3fc-2c963f66afa7
```

**示例响应**:

```json
[
    {
        "entityType": "Project",
        "fieldKey": "project_name",
        "fieldName": "项目名称",
        "dataType": "string",
        "isRequired": true,
        "unit": null,
        "regexPattern": null,
        "options": null,
        "description": "项目名称字段",
        "defaultValue": null,
        "order": 1,
        "isEnabled": true,
        "group": "基本信息",
        "validationRules": "{\"minLength\":1,\"maxLength\":100}",
        "tags": ["重要", "必填"],
        "fieldValue": null
    }
]
```

---

## 12. 批量应用预设到模板

将多个预设的元数据字段批量应用到指定的模板，支持合并策略和重复字段检查。

**接口**: `POST /api/app/meta-field-preset/{templateId}/apply-presets`

**路径参数**:

-   `templateId` (Guid): 模板 ID

**请求体**: `ApplyPresetsToTemplateRequestDto`

| 字段名            | 类型              | 必填 | 描述                                                         | 示例值                                    |
| ----------------- | ----------------- | ---- | ------------------------------------------------------------ | ----------------------------------------- |
| presetIds         | Guid[]            | 是   | 预设 ID 列表                                                 | `["3fa85f64-5717-4562-b3fc-2c963f66afa6"]` |
| mergeStrategy     | MergeStrategy     | 否   | 合并策略：Skip（跳过重复）或 Overwrite（覆盖重复），默认 Skip | `0` (Skip) 或 `1` (Overwrite)             |
| keepExistingFields| boolean           | 否   | 是否保留模板中已存在但不在预设中的字段，默认 true             | `true`                                    |

**合并策略说明**:

-   **Skip (0)**: 跳过重复字段，保留模板中的原有字段
-   **Overwrite (1)**: 覆盖重复字段，用预设中的字段替换模板中的字段

**说明**:
- 此接口会将多个预设中的所有元数据字段合并应用到模板的最新版本
- 支持检查字段键名（`fieldKey`）重复，避免重复添加相同键名的字段
- 支持两种合并策略：跳过重复或覆盖重复
- 可以选择是否保留模板中已存在但不在预设中的字段
- 模板的其他属性（名称、描述、标签等）保持不变
- 所有应用的预设的使用次数会自动增加
- 返回详细的 appliedFields（应用的字段）和 skippedFields（跳过的字段）信息

**响应**: `ApplyPresetsToTemplateResponseDto`

| 字段名            | 类型                | 描述                     |
| ---------------- | ------------------- | ------------------------ |
| appliedFields    | MetaFieldDto[]      | 成功应用的元数据字段列表 |
| skippedFields    | SkippedFieldInfo[]  | 跳过的字段信息           |
| appliedPresetCount | int               | 应用的预设数量           |
| totalFieldCount  | int                 | 总字段数量               |
| appliedFieldCount| int                 | 成功应用的字段数量       |
| skippedFieldCount| int                 | 跳过的字段数量           |

**SkippedFieldInfo**:

| 字段名     | 类型   | 描述           |
| ---------- | ------ | -------------- |
| fieldKey   | string | 字段键名       |
| fieldName  | string | 字段显示名称   |
| reason     | string | 跳过的原因     |
| presetId   | Guid?  | 来源预设 ID    |
| presetName | string?| 来源预设名称   |

**状态码**:

-   `200 OK`: 应用成功
-   `404 Not Found`: 模板不存在
-   `400 Bad Request`: 应用失败（如预设ID列表为空、没有可用的预设等）

**示例请求**:

```bash
POST /api/app/meta-field-preset/7fa85f64-5717-4562-b3fc-2c963f66afa7/apply-presets
Content-Type: application/json

{
    "presetIds": [
        "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "4fa85f64-5717-4562-b3fc-2c963f66afa7"
    ],
    "mergeStrategy": 0,
    "keepExistingFields": true
}
```

**示例响应**:

```json
{
    "appliedFields": [
        {
            "entityType": "Project",
            "fieldKey": "project_name",
            "fieldName": "项目名称",
            "dataType": "string",
            "isRequired": true,
            "unit": null,
            "regexPattern": null,
            "options": null,
            "description": "项目名称字段",
            "defaultValue": null,
            "order": 1,
            "isEnabled": true,
            "group": "基本信息",
            "validationRules": "{\"minLength\":1,\"maxLength\":100}",
            "tags": ["重要", "必填"]
        },
        {
            "entityType": "Project",
            "fieldKey": "project_budget",
            "fieldName": "项目预算",
            "dataType": "number",
            "isRequired": false,
            "unit": "万元",
            "regexPattern": null,
            "options": null,
            "description": "项目预算字段",
            "defaultValue": null,
            "order": 2,
            "isEnabled": true,
            "group": "财务信息",
            "validationRules": null,
            "tags": ["财务"]
        }
    ],
    "skippedFields": [
        {
            "fieldKey": "project_name",
            "fieldName": "项目名称",
            "reason": "模板中已存在相同键名的字段，已跳过",
            "presetId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
            "presetName": "项目文档预设"
        }
    ],
    "appliedPresetCount": 2,
    "totalFieldCount": 3,
    "appliedFieldCount": 2,
    "skippedFieldCount": 1
}
```

**注意事项**:

- 预设必须处于启用状态（`IsEnabled = true`）才能被应用
- 如果预设ID列表中有不存在的预设，会被跳过，不会影响其他预设的应用
- 字段键名（`fieldKey`）是唯一标识，相同键名的字段会被视为重复
- 在跳过策略下，如果模板中已存在相同键名的字段，预设中的字段会被跳过
- 在覆盖策略下，如果模板中已存在相同键名的字段，预设中的字段会替换模板中的字段
- 如果多个预设中存在相同键名的字段，在覆盖策略下，后面的预设会覆盖前面的预设

---

## 13. 记录预设使用

记录预设的使用情况（当模板使用预设时调用）。

**接口**: `POST /api/app/meta-field-preset/{presetId}/record-usage`

**路径参数**:

-   `presetId` (Guid): 预设 ID

**查询参数**:

-   `templateId` (Guid, 可选): 模板 ID

**响应**: 无内容

**状态码**:

-   `200 OK`: 记录成功（静默失败，不影响主流程）

**示例请求**:

```bash
POST /api/app/meta-field-preset/3fa85f64-5717-4562-b3fc-2c963f66afa6/record-usage?templateId=7fa85f64-5717-4562-b3fc-2c963f66afa7
```

---

## 14. 获取统计信息

获取预设的统计信息，包括总数、使用情况、热门预设等。

**接口**: `GET /api/app/meta-field-preset/statistics`

**响应** (`PresetStatisticsDto`):

```json
{
  "totalCount": 50,
  "enabledCount": 45,
  "systemPresetCount": 5,
  "totalUsageCount": 1250,
  "topPresets": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "presetName": "项目文档预设",
      "usageCount": 125,
      "recommendationWeight": 0.85,
      ...
    }
  ],
  "businessScenarioStats": {
    "Project": 20,
    "Document": 15,
    "Archive": 10,
    "Workflow": 5
  },
  "tagStats": {
    "常用": 30,
    "项目": 20,
    "文档": 15,
    "重要": 10
  }
}
```

**字段说明**:

-   `totalCount`: 总预设数
-   `enabledCount`: 启用预设数
-   `systemPresetCount`: 系统预设数
-   `totalUsageCount`: 总使用次数
-   `topPresets`: 最热门预设（Top 10）
-   `businessScenarioStats`: 业务场景统计（业务场景 -> 预设数量）
-   `tagStats`: 标签统计（标签 -> 预设数量）

**状态码**:

-   `200 OK`: 获取成功

---

## 15. 批量更新推荐权重

批量更新所有预设的推荐权重（用于自我进化算法）。

**接口**: `POST /api/app/meta-field-preset/batch-update-weights`

**响应**: 无内容

**状态码**:

-   `200 OK`: 更新成功

**说明**: 此接口会根据预设的使用频率、最近使用时间等因素自动计算并更新推荐权重。

**示例请求**:

```bash
POST /api/app/meta-field-preset/batch-update-weights
```

---

## 数据模型说明

### FacetType（分面类型）枚举值

-   `1`: General（通用）
-   `2`: Discipline（学科）
-   `3`: Classification（分类）
-   `4`: Document（文档）
-   `99`: Other（其他）

### TemplatePurpose（模板用途）枚举值

-   `1`: Classification（分类）
-   `2`: Document（文档）
-   `3`: Workflow（工作流）
-   `4`: Archive（归档）
-   `99`: Other（其他）

### MetaFieldDto 字段说明

-   `entityType`: 实体类型（如 "Project", "Document"）
-   `fieldKey`: 字段键名（唯一标识）
-   `fieldName`: 字段显示名称
-   `dataType`: 数据类型（支持的类型：`string`, `number`, `date`, `boolean`, `array`, `object`, `select`）
    -   `string`: 字符串类型
    -   `number`: 数字类型（整数或小数）
    -   `date`: 日期类型
    -   `boolean`: 布尔类型
    -   `array`: 数组类型
    -   `object`: 对象类型
    -   `select`: 选择类型（枚举，需要提供 `options` 字段）
-   `isRequired`: 是否必填
-   `unit`: 单位（如 "万元", "个", "天"）
-   `regexPattern`: 正则表达式模式（用于验证）
-   `options`: 枚举选项（JSON 数组字符串，如 `"[\"选项1\",\"选项2\"]"`）
-   `description`: 字段描述
-   `defaultValue`: 默认值
-   `order`: 字段顺序
-   `isEnabled`: 是否启用
-   `group`: 字段分组（如 "基本信息", "财务信息"）
-   `validationRules`: 验证规则（JSON 字符串，如 `"{\"minLength\":1,\"maxLength\":100}"`）
-   `tags`: 元数据标签列表

---

## 错误响应格式

所有接口在发生错误时返回统一格式：

```json
{
    "error": {
        "code": "ERROR_CODE",
        "message": "错误描述信息",
        "details": "详细错误信息（可选）"
    }
}
```

**常见错误码**:

-   `400`: Bad Request（请求参数错误）
-   `401`: Unauthorized（未授权）
-   `404`: Not Found（资源不存在）
-   `500`: Internal Server Error（服务器内部错误）

---

## 注意事项

1. **预设名称唯一性**: 预设名称在未删除的记录中必须唯一
2. **系统预设保护**: 系统预设不能删除和禁用
3. **推荐权重范围**: 推荐权重必须在 0.0 到 1.0 之间
4. **JSONB 字段格式**: Tags、MetaFields、BusinessScenarios 等字段使用 JSONB 格式存储
5. **删除操作**: 删除操作是物理删除，记录会被从数据库中删除
6. **推荐算法**: 推荐分数基于推荐权重、使用频率、业务场景匹配、分面类型匹配、模板用途匹配、标签匹配、最近使用时间等因素计算
7. **无审计字段**: `MetaFieldPreset` 继承自 `Entity<Guid>`，不包含创建时间、修改时间等审计字段
8. **数据类型限制**: `metaFields` 中的 `dataType` 字段只支持以下值：
    - `string`: 字符串类型
    - `number`: 数字类型（整数或小数，不支持 `decimal`、`int`、`float` 等）
    - `date`: 日期类型
    - `boolean`: 布尔类型
    - `array`: 数组类型
    - `object`: 对象类型
    - `select`: 选择类型（枚举类型，必须提供 `options` 字段，不支持 `enum`）

---

## 使用示例

### 完整工作流示例

1. **创建预设**:

```bash
POST /api/app/meta-field-preset
{
  "presetName": "项目文档预设",
  "description": "适用于项目文档",
  "metaFields": [...],
  "businessScenarios": ["Project"],
  "applicableFacetTypes": [1],
  "applicableTemplatePurposes": [1]
}
```

2. **搜索预设**:

```bash
POST /api/app/meta-field-preset/search
{
  "keyword": "项目",
  "onlyEnabled": true
}
```

3. **获取推荐预设**:

```bash
POST /api/app/meta-field-preset/recommendations
{
  "businessScenario": "Project",
  "facetType": 1,
  "topN": 5
}
```

4. **应用预设到模板**:

```bash
POST /api/app/meta-field-preset/{presetId}/apply-to-template/{templateId}
```

5. **查看统计信息**:

```bash
GET /api/app/meta-field-preset/statistics
```
