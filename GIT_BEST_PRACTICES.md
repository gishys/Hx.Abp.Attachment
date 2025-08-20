# Git 最佳实践指南

## 概述

本文档描述了在 ABP Framework 项目中使用 Git 的最佳实践，确保仓库保持整洁和高效。

## 文件跟踪规则

### ✅ 应该跟踪的文件

-   源代码文件 (`.cs`, `.cshtml`, `.js`, `.css`, `.html`)
-   项目文件 (`.csproj`, `.sln`)
-   配置文件 (`.json`, `.xml`, `.config`)
-   文档文件 (`.md`, `.txt`)
-   数据库迁移脚本 (`.sql`)
-   资源文件 (图片、字体等)

### ❌ 不应该跟踪的文件

-   编译输出文件 (`bin/`, `obj/`)
-   Visual Studio 缓存文件 (`.vs/`)
-   NuGet 包文件 (`packages/`)
-   日志文件 (`.log`)
-   临时文件 (`.tmp`, `.temp`, `.bak`)
-   用户特定文件 (`.user`, `.suo`)
-   环境变量文件 (`.env`)
-   数据库文件 (`.db`, `.sqlite`)

## 日常操作流程

### 1. 提交前检查

```bash
# 检查当前状态
git status

# 查看将要提交的文件
git diff --cached

# 确保没有意外包含编译输出文件
git status --porcelain | grep -E "(bin/|obj/|\.vs/)"
```

### 2. 清理仓库

```powershell
# 运行清理脚本
.\cleanup-repo.ps1

# 或者手动清理
git clean -fd
git reset --hard
```

### 3. 提交代码

```bash
# 添加文件
git add .

# 检查暂存区
git status

# 提交
git commit -m "描述性的提交信息"

# 推送
git push origin main
```

## 常见问题解决

### 问题：意外提交了编译输出文件

```bash
# 从 Git 中移除文件（保留本地文件）
git rm --cached -r src/*/bin/ src/*/obj/ .vs/

# 提交更改
git commit -m "移除编译输出文件跟踪"

# 推送
git push origin main
```

### 问题：.gitignore 不生效

```bash
# 清除 Git 缓存
git rm -r --cached .

# 重新添加文件
git add .

# 提交
git commit -m "更新 .gitignore 配置"
```

## 分支管理

### 主分支保护

-   `main` 分支应该受到保护
-   禁止直接推送到 `main` 分支
-   使用 Pull Request 进行代码审查

### 功能分支

```bash
# 创建功能分支
git checkout -b feature/新功能名称

# 开发完成后合并
git checkout main
git merge feature/新功能名称
git branch -d feature/新功能名称
```

## 提交信息规范

### 格式

```
类型(范围): 简短描述

详细描述（可选）

相关问题: #123
```

### 类型

-   `feat`: 新功能
-   `fix`: 修复 bug
-   `docs`: 文档更新
-   `style`: 代码格式调整
-   `refactor`: 代码重构
-   `test`: 测试相关
-   `chore`: 构建过程或辅助工具的变动

### 示例

```
feat(ocr): 添加阿里云 OCR 服务集成

- 实现图片文字识别功能
- 添加 OCR 处理状态跟踪
- 支持批量处理图片文件

相关问题: #45
```

## 自动化工具

### 预提交钩子

建议配置 Git 预提交钩子来自动检查：

-   编译错误
-   代码格式
-   测试通过
-   文件大小限制

### CI/CD 集成

-   自动构建和测试
-   代码质量检查
-   自动部署

## 注意事项

1. **定期清理**: 定期运行清理脚本，保持仓库整洁
2. **小步提交**: 频繁提交小的、逻辑完整的更改
3. **描述性信息**: 使用清晰、描述性的提交信息
4. **代码审查**: 重要更改必须经过代码审查
5. **备份**: 重要更改前创建备份分支

## 有用的命令

```bash
# 查看文件历史
git log --follow -- filename

# 查看分支图
git log --graph --oneline --all

# 撤销最后一次提交
git reset --soft HEAD~1

# 查看文件差异
git diff HEAD~1

# 暂存更改
git stash
git stash pop
```

## 联系信息

如有问题，请联系项目维护者或查看项目文档。
