# Neo4j 图数据库部署与使用指南

## 📋 目录

1. [Neo4j 简介](#1-neo4j-简介)
2. [下载与安装](#2-下载与安装)
3. [配置与启动](#3-配置与启动)
4. [项目集成](#4-项目集成)
5. [连接与使用](#5-连接与使用)
6. [常见问题排查](#6-常见问题排查)
7. [性能优化建议](#7-性能优化建议)

---

## 1. Neo4j 简介

### 1.1 什么是 Neo4j？

Neo4j 是一个高性能的图数据库管理系统，专门用于存储和查询图结构数据。它使用 Cypher 查询语言，非常适合处理复杂的关系网络。

### 1.2 为什么选择 Neo4j？

- ✅ **图数据原生支持**：专为图数据设计，性能优异
- ✅ **Cypher 查询语言**：直观易用的图查询语言
- ✅ **ACID 事务支持**：保证数据一致性
- ✅ **丰富的生态系统**：完善的驱动和工具支持
- ✅ **社区版免费**：适合开发和中小型项目

### 1.3 在本项目中的应用

本项目使用 Neo4j 存储知识图谱的实体节点和关系边，支持：
- 五维实体（分类、人员、部门、业务实体、工作流）的节点存储
- 实体间复杂关系的边存储
- 高效的图查询和路径分析
- 影响分析和关系追溯

---

## 2. 下载与安装

### 2.1 Windows 安装

#### 方式一：使用安装程序（推荐）

1. **下载 Neo4j Desktop**
   - 访问官网：https://neo4j.com/download/
   - 下载 Neo4j Desktop（包含社区版）
   - 文件大小约 200MB

2. **安装步骤**
   ```powershell
   # 1. 运行安装程序 Neo4j-Desktop-Setup-x.x.x.exe
   # 2. 按照向导完成安装
   # 3. 启动 Neo4j Desktop
   ```

3. **创建数据库**
   - 打开 Neo4j Desktop
   - 点击 "New Project" 创建项目
   - 点击 "Add Database" → "Create a Local DBMS"
   - 设置数据库名称和密码（例如：`neo4j` / `password`）
   - 点击 "Create"

4. **启动数据库**
   - 在数据库卡片上点击 "Start" 按钮
   - 等待状态变为 "Active"
   - 点击 "Open" 打开 Neo4j Browser（Web 界面）

#### 方式二：使用 ZIP 包

1. **下载 Neo4j Community Edition**
   ```powershell
   # 访问：https://neo4j.com/download-center/#community
   # 下载：neo4j-community-x.x.x-windows.zip
   ```

2. **解压和配置**
   ```powershell
   # 解压到目录，例如：C:\neo4j
   cd C:\neo4j\neo4j-community-x.x.x

   # 编辑配置文件 conf\neo4j.conf
   # 设置数据目录和日志目录
   ```

3. **启动服务**
   ```powershell
   # 在 bin 目录下运行
   .\neo4j.bat console
   # 或作为服务安装
   .\neo4j.bat install-service
   .\neo4j.bat start
   ```

### 2.2 Linux 安装

#### 方式一：使用 APT（Ubuntu/Debian）

```bash
# 1. 添加 Neo4j 官方仓库
wget -O - https://debian.neo4j.com/neotechnology.gpg.key | sudo apt-key add -
echo 'deb https://debian.neo4j.com stable latest' | sudo tee /etc/apt/sources.list.d/neo4j.list

# 2. 更新包列表
sudo apt-get update

# 3. 安装 Neo4j
sudo apt-get install neo4j

# 4. 启动服务
sudo systemctl start neo4j
sudo systemctl enable neo4j  # 设置开机自启

# 5. 检查状态
sudo systemctl status neo4j
```

#### 方式二：使用 YUM（CentOS/RHEL）

```bash
# 1. 添加 Neo4j 官方仓库
cat > /etc/yum.repos.d/neo4j.repo <<EOF
[neo4j]
name=Neo4j Yum Repo
baseurl=https://yum.neo4j.com/stable
enabled=1
gpgcheck=1
EOF

# 2. 安装 Neo4j
sudo yum install neo4j

# 3. 启动服务
sudo systemctl start neo4j
sudo systemctl enable neo4j
```

#### 方式三：使用 TAR 包

```bash
# 1. 下载
wget https://neo4j.com/artifact.php?name=neo4j-community-x.x.x-unix.tar.gz

# 2. 解压
tar -xzf neo4j-community-x.x.x-unix.tar.gz
cd neo4j-community-x.x.x

# 3. 启动
./bin/neo4j start
```

### 2.3 Docker 安装（推荐用于生产环境）

#### 使用 Docker Compose

1. **创建 docker-compose.yml**
   ```yaml
   version: '3.8'
   
   services:
     neo4j:
       image: neo4j:5.15
       container_name: neo4j-kg
       ports:
         - "7474:7474"  # HTTP 端口（Web 界面）
         - "7687:7687"  # Bolt 端口（应用程序连接）
       environment:
         - NEO4J_AUTH=neo4j/password  # 用户名/密码
         - NEO4J_PLUGINS=["apoc"]     # 可选：安装 APOC 插件
         - NEO4J_dbms_memory_heap_max__size=2G
         - NEO4J_dbms_memory_pagecache_size=1G
       volumes:
         - neo4j_data:/data
         - neo4j_logs:/logs
         - neo4j_import:/var/lib/neo4j/import
         - neo4j_plugins:/plugins
       restart: unless-stopped
       networks:
         - kg-network
   
   volumes:
     neo4j_data:
       driver: local
     neo4j_logs:
       driver: local
     neo4j_import:
       driver: local
     neo4j_plugins:
       driver: local
   
   networks:
     kg-network:
       driver: bridge
   ```

2. **启动服务**
   ```bash
   # 启动 Neo4j
   docker-compose up -d
   
   # 查看日志
   docker-compose logs -f neo4j
   
   # 停止服务
   docker-compose stop
   
   # 删除服务（保留数据）
   docker-compose down
   
   # 删除服务和数据
   docker-compose down -v
   ```

#### 使用 Docker 命令

```bash
# 运行 Neo4j 容器
docker run -d \
  --name neo4j-kg \
  -p 7474:7474 \
  -p 7687:7687 \
  -e NEO4J_AUTH=neo4j/password \
  -e NEO4J_dbms_memory_heap_max__size=2G \
  -v neo4j_data:/data \
  -v neo4j_logs:/logs \
  neo4j:5.15

# 查看运行状态
docker ps | grep neo4j

# 查看日志
docker logs -f neo4j-kg

# 停止容器
docker stop neo4j-kg

# 启动容器
docker start neo4j-kg

# 删除容器（保留数据卷）
docker rm neo4j-kg
```

---

## 3. 配置与启动

### 3.1 基本配置

#### 配置文件位置

- **Windows**: `conf\neo4j.conf`
- **Linux**: `/etc/neo4j/neo4j.conf` 或 `$NEO4J_HOME/conf/neo4j.conf`
- **Docker**: 通过环境变量或挂载配置文件

#### 关键配置项

```properties
# ====================================
# 网络配置
# ====================================

# HTTP 连接配置（Web 界面）
dbms.default_listen_address=0.0.0.0
dbms.default_advertised_address=localhost
dbms.connector.http.enabled=true
dbms.connector.http.listen_address=:7474

# Bolt 连接配置（应用程序）
dbms.connector.bolt.enabled=true
dbms.connector.bolt.listen_address=:7687
dbms.connector.bolt.advertised_address=localhost:7687

# ====================================
# 内存配置（根据服务器内存调整）
# ====================================

# 堆内存最大大小（建议设置为服务器内存的 50-75%）
dbms.memory.heap.max_size=2G

# 页面缓存大小（建议设置为服务器内存的 50%）
dbms.memory.pagecache.size=1G

# ====================================
# 数据目录配置
# ====================================

dbms.directories.data=data
dbms.directories.logs=logs
dbms.directories.import=import

# ====================================
# 安全配置
# ====================================

# 初始密码（首次启动后需要修改）
dbms.security.auth_enabled=true

# ====================================
# 性能配置
# ====================================

# 事务日志配置
dbms.tx_log.rotation.retention_policy=7 days

# 查询超时时间（毫秒）
dbms.transaction.timeout=60s
```

### 3.2 启动和停止

#### Windows

```powershell
# 使用 Neo4j Desktop
# 在界面中点击 "Start" 按钮

# 或使用命令行
cd C:\neo4j\bin
.\neo4j.bat start      # 后台启动
.\neo4j.bat console    # 前台启动（查看日志）
.\neo4j.bat stop       # 停止
.\neo4j.bat status     # 查看状态
```

#### Linux

```bash
# 使用 systemd
sudo systemctl start neo4j
sudo systemctl stop neo4j
sudo systemctl restart neo4j
sudo systemctl status neo4j

# 或使用服务脚本
sudo service neo4j start
sudo service neo4j stop
sudo service neo4j restart
```

#### Docker

```bash
# 启动
docker start neo4j-kg

# 停止
docker stop neo4j-kg

# 重启
docker restart neo4j-kg

# 查看状态
docker ps | grep neo4j
```

### 3.3 验证安装

1. **访问 Web 界面**
   - 打开浏览器访问：http://localhost:7474
   - 首次登录使用默认用户名：`neo4j`
   - 输入初始密码（安装时设置的密码）

2. **测试连接**
   ```cypher
   // 在 Neo4j Browser 中执行
   RETURN "Hello Neo4j!" AS message;
   ```

3. **检查版本**
   ```cypher
   CALL dbms.components()
   YIELD name, versions, edition
   RETURN name, versions, edition;
   ```

---

## 4. 项目集成

### 4.1 安装 NuGet 包

在项目中安装 Neo4j 官方 .NET 驱动：

```xml
<!-- 在 .csproj 文件中添加 -->
<ItemGroup>
  <PackageReference Include="Neo4j.Driver" Version="5.15.0" />
</ItemGroup>
```

或使用 NuGet 包管理器：

```powershell
# Package Manager Console
Install-Package Neo4j.Driver -Version 5.15.0

# .NET CLI
dotnet add package Neo4j.Driver --version 5.15.0
```

### 4.2 配置连接字符串

#### 在 appsettings.json 中配置

```json
{
  "Neo4j": {
    "Uri": "bolt://localhost:7687",
    "Username": "neo4j",
    "Password": "password",
    "Database": "neo4j"
  }
}
```

#### 在 appsettings.Development.json 中配置（开发环境）

```json
{
  "Neo4j": {
    "Uri": "bolt://localhost:7687",
    "Username": "neo4j",
    "Password": "password",
    "Database": "neo4j"
  }
}
```

#### 在 appsettings.Production.json 中配置（生产环境）

```json
{
  "Neo4j": {
    "Uri": "bolt://neo4j-server:7687",
    "Username": "neo4j",
    "Password": "your-secure-password",
    "Database": "neo4j"
  }
}
```

### 4.3 注册 Neo4j 驱动服务

#### 创建配置类

```csharp
// Hx.Abp.Attachment.Domain/KnowledgeGraph/Neo4jOptions.cs
namespace Hx.Abp.Attachment.Domain.KnowledgeGraph
{
    public class Neo4jOptions
    {
        public string Uri { get; set; } = "bolt://localhost:7687";
        public string Username { get; set; } = "neo4j";
        public string Password { get; set; } = "password";
        public string Database { get; set; } = "neo4j";
    }
}
```

#### 在模块中注册服务

```csharp
// Hx.Abp.Attachment.Api/AppModule.cs 或相应的模块类
using Neo4j.Driver;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace Hx.Abp.Attachment.Api
{
    [DependsOn(
        // ... 其他依赖
    )]
    public class AppModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();
            
            // 配置 Neo4j 选项
            var neo4jOptions = configuration.GetSection("Neo4j").Get<Neo4jOptions>();
            context.Services.Configure<Neo4jOptions>(configuration.GetSection("Neo4j"));
            
            // 注册 Neo4j 驱动（单例模式）
            context.Services.AddSingleton<IDriver>(sp =>
            {
                return GraphDatabase.Driver(
                    neo4jOptions.Uri,
                    AuthTokens.Basic(neo4jOptions.Username, neo4jOptions.Password),
                    options => options
                        .WithMaxConnectionLifetime(TimeSpan.FromHours(1))
                        .WithMaxConnectionPoolSize(50)
                        .WithConnectionAcquisitionTimeout(TimeSpan.FromMinutes(2))
                );
            });
            
            // 注册 Neo4j 服务
            // context.Services.AddScoped<IKnowledgeGraphService, KnowledgeGraphService>();
        }
        
        public override void OnApplicationShutdown(ApplicationShutdownContext context)
        {
            // 关闭 Neo4j 驱动连接
            var driver = context.ServiceProvider.GetService<IDriver>();
            driver?.Dispose();
        }
    }
}
```

### 4.4 创建 Neo4j 服务接口和实现

#### 定义接口

```csharp
// Hx.Abp.Attachment.Application.Contracts/KnowledgeGraph/INeo4jService.cs
using Neo4j.Driver;

namespace Hx.Abp.Attachment.Application.Contracts.KnowledgeGraph
{
    public interface INeo4jService
    {
        Task<IAsyncSession> GetSessionAsync();
        Task<T> ExecuteReadAsync<T>(Func<IAsyncSession, Task<T>> work);
        Task<T> ExecuteWriteAsync<T>(Func<IAsyncSession, Task<T>> work);
    }
}
```

#### 实现服务

```csharp
// Hx.Abp.Attachment.Application/KnowledgeGraph/Neo4jService.cs
using Neo4j.Driver;
using Volo.Abp.DependencyInjection;
using Microsoft.Extensions.Options;
using Hx.Abp.Attachment.Domain.KnowledgeGraph;

namespace Hx.Abp.Attachment.Application.KnowledgeGraph
{
    public class Neo4jService : INeo4jService, ITransientDependency
    {
        private readonly IDriver _driver;
        private readonly Neo4jOptions _options;

        public Neo4jService(IDriver driver, IOptions<Neo4jOptions> options)
        {
            _driver = driver;
            _options = options.Value;
        }

        public async Task<IAsyncSession> GetSessionAsync()
        {
            return _driver.AsyncSession(config =>
                config.WithDatabase(_options.Database));
        }

        public async Task<T> ExecuteReadAsync<T>(Func<IAsyncSession, Task<T>> work)
        {
            var session = await GetSessionAsync();
            try
            {
                return await work(session);
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        public async Task<T> ExecuteWriteAsync<T>(Func<IAsyncSession, Task<T>> work)
        {
            var session = await GetSessionAsync();
            try
            {
                return await work(session);
            }
            finally
            {
                await session.CloseAsync();
            }
        }
    }
}
```

---

## 5. 连接与使用

### 5.1 基本连接示例

```csharp
using Neo4j.Driver;

// 创建驱动
var driver = GraphDatabase.Driver(
    "bolt://localhost:7687",
    AuthTokens.Basic("neo4j", "password")
);

// 获取会话
var session = driver.AsyncSession();

try
{
    // 执行查询
    var result = await session.RunAsync(
        "MATCH (n) RETURN count(n) AS count"
    );
    
    var record = await result.SingleAsync();
    var count = record["count"].As<long>();
    Console.WriteLine($"节点数量: {count}");
}
finally
{
    await session.CloseAsync();
    driver.Dispose();
}
```

### 5.2 在服务中使用

```csharp
// Hx.Abp.Attachment.Application/KnowledgeGraph/KnowledgeGraphService.cs
using Neo4j.Driver;
using Volo.Abp.Application.Services;

namespace Hx.Abp.Attachment.Application.KnowledgeGraph
{
    public class KnowledgeGraphService : ApplicationService
    {
        private readonly IDriver _driver;
        private readonly Neo4jOptions _options;

        public KnowledgeGraphService(IDriver driver, IOptions<Neo4jOptions> options)
        {
            _driver = driver;
            _options = options.Value;
        }

        /// <summary>
        /// 创建实体节点
        /// </summary>
        public async Task CreateEntityAsync(Guid entityId, string entityType, string name)
        {
            var session = _driver.AsyncSession(config =>
                config.WithDatabase(_options.Database));
            
            try
            {
                var query = @"
                    MERGE (e:Entity {id: $id})
                    SET e.type = $type,
                        e.name = $name,
                        e.createdTime = datetime()
                    RETURN e";

                await session.RunAsync(query, new
                {
                    id = entityId.ToString(),
                    type = entityType,
                    name = name
                });
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        /// <summary>
        /// 创建关系
        /// </summary>
        public async Task CreateRelationshipAsync(
            Guid sourceId, 
            string sourceType,
            Guid targetId, 
            string targetType,
            string relationshipType)
        {
            var session = _driver.AsyncSession(config =>
                config.WithDatabase(_options.Database));
            
            try
            {
                var query = @"
                    MATCH (source:Entity {id: $sourceId})
                    MATCH (target:Entity {id: $targetId})
                    MERGE (source)-[r:RELATES_TO {type: $relType}]->(target)
                    SET r.createdTime = datetime()
                    RETURN r";

                await session.RunAsync(query, new
                {
                    sourceId = sourceId.ToString(),
                    targetId = targetId.ToString(),
                    relType = relationshipType
                });
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        /// <summary>
        /// 查询实体及其关系
        /// </summary>
        public async Task<GraphDataDto> GetGraphDataAsync(Guid centerEntityId, int depth = 2)
        {
            var session = _driver.AsyncSession(config =>
                config.WithDatabase(_options.Database));
            
            try
            {
                var query = @"
                    MATCH path = (center:Entity {id: $centerId})-[*1..$depth]-(related:Entity)
                    WITH DISTINCT related as node, relationships(path) as rels
                    RETURN node, rels
                    LIMIT 500";

                var result = await session.RunAsync(query, new
                {
                    centerId = centerEntityId.ToString(),
                    depth = depth
                });

                var nodes = new List<NodeDto>();
                var edges = new List<EdgeDto>();

                await foreach (var record in result)
                {
                    var node = record["node"].As<INode>();
                    nodes.Add(new NodeDto
                    {
                        Id = Guid.Parse(node["id"].As<string>()),
                        Type = node["type"].As<string>(),
                        Name = node["name"].As<string>()
                    });

                    var relationships = record["rels"].As<List<IRelationship>>();
                    foreach (var rel in relationships)
                    {
                        edges.Add(new EdgeDto
                        {
                            Source = Guid.Parse(rel.StartNodeElementId),
                            Target = Guid.Parse(rel.EndNodeElementId),
                            Type = rel.Type
                        });
                    }
                }

                return new GraphDataDto
                {
                    Nodes = nodes,
                    Edges = edges
                };
            }
            finally
            {
                await session.CloseAsync();
            }
        }
    }
}
```

### 5.3 常用 Cypher 查询示例

#### 创建节点

```cypher
// 创建分类节点
CREATE (c:Catalogue {
  id: 'catalog-001',
  name: '项目档案',
  type: 'Catalogue',
  status: 'ACTIVE'
})
RETURN c;
```

#### 创建关系

```cypher
// 创建分类之间的父子关系
MATCH (parent:Catalogue {id: 'catalog-001'})
MATCH (child:Catalogue {id: 'catalog-002'})
CREATE (parent)-[:HAS_CHILD]->(child)
RETURN parent, child;
```

#### 查询节点

```cypher
// 查询所有分类节点
MATCH (c:Catalogue)
RETURN c
LIMIT 100;
```

#### 查询关系

```cypher
// 查询分类的所有子分类
MATCH (parent:Catalogue {id: 'catalog-001'})-[:HAS_CHILD]->(child:Catalogue)
RETURN parent, child;
```

#### 路径查询

```cypher
// 查询两个节点之间的路径
MATCH path = (start:Catalogue {id: 'catalog-001'})-[*1..5]-(end:Catalogue {id: 'catalog-010'})
RETURN path
LIMIT 10;
```

#### 删除节点和关系

```cypher
// 删除节点及其所有关系
MATCH (c:Catalogue {id: 'catalog-001'})
DETACH DELETE c;
```

---

## 6. 常见问题排查

### 6.1 连接问题

#### 问题：无法连接到 Neo4j

**症状**：
```
System.Net.Sockets.SocketException: No connection could be made
```

**解决方案**：
1. 检查 Neo4j 服务是否运行
   ```bash
   # Windows
   .\neo4j.bat status
   
   # Linux
   sudo systemctl status neo4j
   
   # Docker
   docker ps | grep neo4j
   ```

2. 检查端口是否开放
   ```bash
   # Windows
   netstat -an | findstr 7687
   
   # Linux
   netstat -tuln | grep 7687
   ```

3. 检查防火墙设置
   ```bash
   # Windows 防火墙
   # 允许端口 7687 和 7474
   
   # Linux 防火墙
   sudo ufw allow 7687/tcp
   sudo ufw allow 7474/tcp
   ```

4. 检查连接字符串
   ```csharp
   // 确保 URI 格式正确
   "bolt://localhost:7687"  // 本地连接
   "bolt://192.168.1.100:7687"  // 远程连接
   ```

#### 问题：认证失败

**症状**：
```
Neo4j.Driver.Exceptions.AuthenticationException: The client is unauthorized
```

**解决方案**：
1. 检查用户名和密码
   ```cypher
   // 在 Neo4j Browser 中测试登录
   // http://localhost:7474
   ```

2. 重置密码
   ```bash
   # 停止 Neo4j
   sudo systemctl stop neo4j
   
   # 删除认证文件（Linux）
   sudo rm -rf /var/lib/neo4j/data/dbms/auth
   
   # 重启 Neo4j，使用默认密码登录后立即修改
   ```

### 6.2 性能问题

#### 问题：查询速度慢

**解决方案**：
1. 创建索引
   ```cypher
   // 为常用查询字段创建索引
   CREATE INDEX catalogue_id_index FOR (c:Catalogue) ON (c.id);
   CREATE INDEX catalogue_name_index FOR (c:Catalogue) ON (c.name);
   CREATE INDEX entity_type_index FOR (e:Entity) ON (e.type);
   ```

2. 优化查询
   ```cypher
   // 使用 LIMIT 限制结果数量
   MATCH (n:Entity)
   RETURN n
   LIMIT 100;
   
   // 使用 WHERE 子句提前过滤
   MATCH (c:Catalogue)
   WHERE c.status = 'ACTIVE'
   RETURN c;
   ```

3. 调整内存配置
   ```properties
   # 增加堆内存
   dbms.memory.heap.max_size=4G
   
   # 增加页面缓存
   dbms.memory.pagecache.size=2G
   ```

### 6.3 数据同步问题

#### 问题：数据未同步到 Neo4j

**解决方案**：
1. 检查后台作业是否运行
   ```csharp
   // 查看后台作业日志
   // 确保 KnowledgeGraphSyncJob 正常执行
   ```

2. 检查连接配置
   ```json
   // 确保 appsettings.json 中的 Neo4j 配置正确
   {
     "Neo4j": {
       "Uri": "bolt://localhost:7687",
       "Username": "neo4j",
       "Password": "password"
     }
   }
   ```

3. 手动触发同步
   ```csharp
   // 在服务中调用同步方法
   await _syncService.SyncAllEntitiesAsync();
   ```

---

## 7. 性能优化建议

### 7.1 索引优化

```cypher
// 为常用查询字段创建索引
CREATE INDEX catalogue_id_index FOR (c:Catalogue) ON (c.id);
CREATE INDEX catalogue_name_index FOR (c:Catalogue) ON (c.name);
CREATE INDEX catalogue_status_index FOR (c:Catalogue) ON (c.status);
CREATE INDEX person_employee_id_index FOR (p:Person) ON (p.employeeId);
CREATE INDEX workflow_code_index FOR (w:Workflow) ON (w.workflowCode);

// 创建复合索引（Neo4j 5.x 支持）
CREATE INDEX catalogue_reference_index FOR (c:Catalogue) ON (c.reference, c.referenceType);
```

### 7.2 查询优化

```cypher
// ✅ 好的做法：使用索引字段查询
MATCH (c:Catalogue {id: $id})
RETURN c;

// ❌ 避免：全表扫描
MATCH (c:Catalogue)
WHERE c.id = $id
RETURN c;

// ✅ 好的做法：限制结果数量
MATCH (c:Catalogue)
RETURN c
LIMIT 100;

// ✅ 好的做法：使用投影减少数据传输
MATCH (c:Catalogue)
RETURN c.id, c.name, c.status;
```

### 7.3 连接池配置

```csharp
// 优化连接池配置
var driver = GraphDatabase.Driver(
    uri,
    authToken,
    options => options
        .WithMaxConnectionPoolSize(50)           // 最大连接数
        .WithConnectionAcquisitionTimeout(TimeSpan.FromMinutes(2))  // 获取连接超时
        .WithMaxConnectionLifetime(TimeSpan.FromHours(1))  // 连接生命周期
        .WithConnectionTimeout(TimeSpan.FromSeconds(30))   // 连接超时
);
```

### 7.4 批量操作

```csharp
// 使用事务批量创建节点
var session = driver.AsyncSession();
var transaction = await session.BeginTransactionAsync();

try
{
    foreach (var entity in entities)
    {
        await transaction.RunAsync(
            "CREATE (e:Entity {id: $id, name: $name})",
            new { id = entity.Id.ToString(), name = entity.Name }
        );
    }
    
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
finally
{
    await session.CloseAsync();
}
```

---

## 8. 监控和维护

### 8.1 监控指标

```cypher
// 查看数据库统计信息
CALL db.stats.retrieve('GRAPH COUNTS');

// 查看节点数量
MATCH (n)
RETURN labels(n) AS label, count(n) AS count
ORDER BY count DESC;

// 查看关系数量
MATCH ()-[r]->()
RETURN type(r) AS relationshipType, count(r) AS count
ORDER BY count DESC;
```

### 8.2 日志查看

```bash
# Windows
# 日志位置：neo4j\logs\neo4j.log

# Linux
sudo tail -f /var/log/neo4j/neo4j.log

# Docker
docker logs -f neo4j-kg
```

### 8.3 备份和恢复

```bash
# 备份数据库
neo4j-admin database backup neo4j --backup-dir=/backup

# 恢复数据库
neo4j-admin database restore neo4j --from-path=/backup/neo4j.dump
```

---

## 9. 参考资料

- **Neo4j 官方文档**：https://neo4j.com/docs/
- **Neo4j .NET 驱动文档**：https://neo4j.com/docs/dotnet-manual/current/
- **Cypher 查询语言参考**：https://neo4j.com/docs/cypher-manual/current/
- **Neo4j 社区论坛**：https://community.neo4j.com/

---

## 10. 附录：快速参考

### 10.1 常用命令

```bash
# 启动 Neo4j
# Windows
.\neo4j.bat start

# Linux
sudo systemctl start neo4j

# Docker
docker start neo4j-kg

# 停止 Neo4j
# Windows
.\neo4j.bat stop

# Linux
sudo systemctl stop neo4j

# Docker
docker stop neo4j-kg
```

### 10.2 常用 Cypher 查询

```cypher
// 清空数据库（谨慎使用！）
MATCH (n) DETACH DELETE n;

// 查看所有节点
MATCH (n) RETURN n LIMIT 100;

// 查看所有关系
MATCH ()-[r]->() RETURN r LIMIT 100;

// 统计节点数量
MATCH (n) RETURN count(n) AS totalNodes;

// 统计关系数量
MATCH ()-[r]->() RETURN count(r) AS totalRelationships;
```

---

**文档版本**：v1.0  
**最后更新**：2024年  
**维护者**：开发团队

