# 附件目录模板接口测试文档

## 概述

本文件夹包含了附件目录模板相关接口的完整测试文档和工具，帮助开发人员和测试人员快速理解和使用这些接口。

## 文件说明

### 📋 文档文件

| 文件名                 | 描述                | 用途                                                     |
| ---------------------- | ------------------- | -------------------------------------------------------- |
| `API_Documentation.md` | 详细的 API 接口文档 | 开发人员参考，包含接口说明、参数详解、应用场景和调用示例 |
| `Test_Cases.md`        | 完整的测试用例文档  | 测试人员参考，包含详细的测试步骤、预期结果和验证点       |
| `README.md`            | 本说明文档          | 使用指南和文件说明                                       |

### 🧪 测试工具

| 文件名                                                | 描述             | 用途                            |
| ----------------------------------------------------- | ---------------- | ------------------------------- |
| `AttachCatalogueTemplate_API.postman_collection.json` | Postman 测试集合 | 可直接导入 Postman 进行接口测试 |

## 接口概览

### 1. 创建分类模板接口

-   **路径**: `POST /api/attach-catalogue-template`
-   **功能**: 创建新的附件目录模板
-   **主要参数**: templateName, description, tags, workflowConfig, metaFields 等

### 2. 获取根节点模板接口

-   **路径**: `GET /api/attach-catalogue-template/tree/roots`
-   **功能**: 获取根节点模板列表，用于树状展示
-   **主要参数**: facetType, templatePurpose, includeChildren, onlyLatest

## 快速开始

### 方法一：使用 Postman 测试

1. **导入测试集合**

    - 打开 Postman
    - 点击"Import"按钮
    - 选择`AttachCatalogueTemplate_API.postman_collection.json`文件
    - 导入成功后会看到"附件目录模板 API 测试集合"

2. **配置环境变量**

    - 在 Postman 中创建新环境
    - 设置以下变量：
        ```
        baseUrl: https://your-api-domain.com
        token: your-bearer-token
        ```

3. **运行测试**
    - 选择要测试的接口
    - 点击"Send"按钮
    - 查看响应结果和测试结果

### 方法二：使用 cURL 测试

1. **创建根级模板**

    ```bash
    curl -X POST "https://your-api-domain.com/api/attach-catalogue-template" \
      -H "Content-Type: application/json" \
      -H "Authorization: Bearer your-token" \
      -d '{
        "templateName": "测试根级模板",
        "description": "这是一个测试用的根级模板",
        "tags": ["测试", "根级"],
        "attachReceiveType": 1,
        "workflowConfig": "{\"workflowKey\":\"test_workflow\",\"timeout\":3600}",
        "isRequired": true,
        "sequenceNumber": 100,
        "isStatic": false,
        "facetType": 0,
        "templatePurpose": 1,
        "permissions": [],
        "metaFields": []
      }'
    ```

2. **获取根级模板**
    ```bash
    curl -X GET "https://your-api-domain.com/api/attach-catalogue-template/tree/roots?includeChildren=true&onlyLatest=true" \
      -H "Authorization: Bearer your-token"
    ```

### 方法三：使用 JavaScript 测试

```javascript
// 创建模板
const createTemplate = async (templateData) => {
    const response = await fetch('/api/attach-catalogue-template', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            Authorization: 'Bearer ' + token,
        },
        body: JSON.stringify(templateData),
    });
    return response.json();
};

// 获取根级模板
const getRootTemplates = async (params = {}) => {
    const queryParams = new URLSearchParams(params);
    const response = await fetch(
        `/api/attach-catalogue-template/tree/roots?${queryParams}`
    );
    return response.json();
};
```

## 测试场景说明

### 创建模板测试场景

1. **正常场景**

    - 创建根级模板
    - 创建子级模板
    - 创建带元数据字段的模板
    - 创建带权限的模板

2. **异常场景**
    - 必填字段验证
    - 字段长度验证
    - 枚举值验证
    - 工作流配置验证

### 获取模板测试场景

1. **正常场景**

    - 获取所有根级模板
    - 获取特定类型的根级模板
    - 获取根级模板概览
    - 获取所有版本（包括历史版本）

2. **边界场景**
    - 空结果测试
    - 参数边界值测试
    - 性能测试
    - 并发测试

## 应用场景示例

### 场景 1：合同管理系统

```json
{
    "templateName": "合同文档模板",
    "description": "用于存储各类合同文档的模板",
    "tags": ["合同", "法律", "重要"],
    "attachReceiveType": 1,
    "workflowConfig": "{\"workflowKey\":\"contract_approval\",\"timeout\":3600,\"skipApprovers\":[\"admin\"],\"scripts\":[\"validate_contract.js\"],\"webhooks\":[\"https://api.company.com/contract-notify\"]}",
    "isRequired": true,
    "sequenceNumber": 100,
    "isStatic": false,
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

### 场景 2：采购管理系统

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
    "parentId": "parent-template-id",
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

### 场景 3：系统日志管理

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
            "fieldType": "string",
            "isRequired": true,
            "defaultValue": "unknown"
        }
    ]
}
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

## 测试数据清理

### 清理步骤

1. 删除测试创建的模板
2. 清理测试数据
3. 重置测试环境

### 清理脚本

```sql
-- 删除测试模板
DELETE FROM "APPATTACH_CATALOGUE_TEMPLATES"
WHERE "TEMPLATE_NAME" LIKE '测试%'
OR "TEMPLATE_NAME" LIKE '%测试%';
```

## 版本信息

-   **文档版本**: 1.0
-   **API 版本**: v1
-   **最后更新**: 2024-12-19
-   **维护人员**: 开发团队

## 联系方式

如有问题或建议，请联系：

-   开发团队: dev-team@company.com
-   测试团队: test-team@company.com
-   技术支持: support@company.com
