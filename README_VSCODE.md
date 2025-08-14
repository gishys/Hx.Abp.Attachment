# VS Code 配置说明

## 概述

本项目已配置了完整的 VS Code 开发环境，支持 .NET 8 C# 开发，提供智能提示、错误检查、代码格式化等功能。

## 必需扩展

### 核心扩展

-   **C#** (`ms-dotnettools.csharp`) - C# 语言支持
-   **.NET Runtime Install Tool** (`ms-dotnettools.vscode-dotnet-runtime`) - .NET 运行时
-   **C# Dev Kit** (`ms-dotnettools.csdevkit`) - C# 开发工具包

### 辅助扩展

-   **JSON Language Features** (`ms-vscode.vscode-json`) - JSON 支持
-   **PowerShell** (`ms-vscode.powershell`) - PowerShell 支持
-   **.NET Core Test Explorer** (`formulahendry.dotnet-test-explorer`) - 测试资源管理器
-   **C# Extensions** (`kreativ-software.csharpextensions`) - C# 扩展功能
-   **Namespace** (`adrianwilczynski.namespace`) - 命名空间管理

### 前端扩展（可选）

-   **TypeScript** (`ms-vscode.vscode-typescript-next`) - TypeScript 支持
-   **Tailwind CSS** (`bradlc.vscode-tailwindcss`) - Tailwind CSS 支持
-   **Prettier** (`esbenp.prettier-vscode`) - 代码格式化
-   **ESLint** (`ms-vscode.vscode-eslint`) - JavaScript 代码检查

## 功能特性

### 1. 智能提示

-   自动补全
-   语法高亮
-   错误检查
-   重构建议

### 2. 代码格式化

-   保存时自动格式化
-   自动整理 using 语句
-   统一的代码风格

### 3. 调试支持

-   断点调试
-   变量查看
-   调用堆栈

### 4. 任务支持

-   构建项目
-   发布项目
-   清理项目
-   热重载

## 使用方法

### 1. 安装扩展

打开 VS Code，按 `Ctrl+Shift+X` 打开扩展面板，搜索并安装推荐的扩展。

### 2. 打开项目

```bash
code .
```

### 3. 构建项目

-   按 `Ctrl+Shift+P` 打开命令面板
-   输入 "Tasks: Run Task"
-   选择 "build" 任务

### 4. 调试项目

-   按 `F5` 开始调试
-   选择 "Launch API" 或 "Launch Web" 配置

### 5. 代码格式化

-   按 `Shift+Alt+F` 格式化当前文件
-   保存时自动格式化（已启用）

## 快捷键

| 功能       | 快捷键         |
| ---------- | -------------- |
| 构建项目   | `Ctrl+Shift+B` |
| 开始调试   | `F5`           |
| 停止调试   | `Shift+F5`     |
| 格式化代码 | `Shift+Alt+F`  |
| 查找引用   | `Shift+F12`    |
| 转到定义   | `F12`          |
| 重命名符号 | `F2`           |
| 快速修复   | `Ctrl+.`       |

## 配置说明

### settings.json

-   设置默认解决方案文件
-   启用 Roslyn 分析器
-   配置自动格式化
-   隐藏编译输出文件夹

### launch.json

-   配置 API 项目调试
-   配置 Web 项目调试
-   设置环境变量

### tasks.json

-   构建任务
-   发布任务
-   清理任务
-   热重载任务

### .editorconfig

-   统一代码风格
-   缩进设置
-   换行符设置
-   C# 特定规则

## 故障排除

### 1. 智能提示不工作

-   确保安装了 C# 扩展
-   重启 VS Code
-   检查 .NET SDK 是否正确安装

### 2. 调试不工作

-   确保项目已构建
-   检查 launch.json 配置
-   验证目标框架版本

### 3. 格式化不工作

-   检查 .editorconfig 文件
-   确保启用了格式化功能
-   重启 VS Code

## 推荐工作流

1. **打开项目**：使用 `code .` 命令
2. **安装扩展**：安装推荐的扩展
3. **构建项目**：使用 `Ctrl+Shift+B`
4. **开始开发**：编写代码，享受智能提示
5. **调试测试**：使用 `F5` 进行调试
6. **提交代码**：格式化后提交

## 注意事项

1. 确保安装了 .NET 8 SDK
2. 首次打开项目可能需要一些时间来加载依赖
3. 建议定期更新扩展以获得最新功能
4. 如果遇到性能问题，可以禁用不必要的扩展
