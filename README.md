# Org.Product Web Backend

基于 .NET 10 的 Web API 后端模板项目，采用 DDD 分层组织代码，内置用户、角色、菜单/权限、JWT 鉴权、验证码、Redis 缓存、PostgreSQL 持久化、Swagger 和 Docker 部署示例。

## 技术栈

- .NET 10 / ASP.NET Core Web API
- Entity Framework Core 10 / Npgsql / PostgreSQL
- Autofac 依赖注入
- AutoMapper DTO 映射
- MediatR 领域事件
- JWT Bearer + ECDSA 密钥
- StackExchange.Redis
- Serilog 日志
- Swashbuckle Swagger
- SkiaSharp 验证码图片生成

## 项目结构

```text
.
├── Org.Product.sln
├── src
│   ├── Org.Product.WebApi          # API 入口、控制器、中间件、Swagger、配置
│   ├── Org.Product.Application     # 应用服务、DTO、鉴权、验证码、AutoMapper
│   ├── Org.Product.Domain          # 实体、值对象、领域服务接口、领域事件
│   ├── Org.Product.Infrastructure  # EF Core DbContext、数据库初始化、领域服务实现
│   └── Org.Product.Domain.Shared            # 通用异常、分页、配置、扩展、JSON 转换器
└── deployment                       # Docker Compose、Nginx、Redis、MQTT、SFTP 配置示例
```

## 快速开始

### 从模板创建新的业务项目

在模板仓库根目录执行：

```powershell
dotnet new install .
dotnet new org-product-web -n Org.Ordering -o D:\Code\Org.Ordering
dotnet build D:\Code\Org.Ordering\Org.Ordering.sln
```

`-n` 必须是业务边界上下文名称，例如 `Org.Ordering`、`Org.Identity` 或
`Org.DeviceManagement`。模板生成时会将 `Org.Product` 同时替换为该名称，覆盖 solution、
项目目录和文件名、程序集、命名空间、项目引用及 Dockerfile 路径。

模板不会复制构建产物、IDE 用户文件、密钥目录或历史 Backup csproj 文件。

### 环境要求

- .NET SDK 10.x
- PostgreSQL
- Redis
- Docker 可选，用于启动本地依赖或构建镜像

### 启动基础设施

仓库提供了 `deployment/docker-compose.yaml`。如果只需要本地运行 API，通常先启动 PostgreSQL 和 Redis：

```bash
cd deployment
docker compose up -d database cache
```

默认连接配置位于 `src/Org.Product.WebApi/appsettings.json`：

```json
{
  "ConnectionStrings": {
    "Postgres": "host=localhost:5432;username=dev;password=dev@2024;database=postgres;Command Timeout=0;Include Error Detail=true",
    "Redis": "localhost:6379"
  }
}
```

### 启动 API

回到仓库根目录执行：

```bash
dotnet restore Org.Product.sln
dotnet build Org.Product.sln
dotnet run --project src/Org.Product.WebApi/Org.Product.WebApi.csproj
```

默认开发地址：

- HTTP: `http://localhost:5156`
- HTTPS: `https://localhost:7247`
- Swagger: `http://localhost:5156/swagger`

也可以使用 HTTPS profile：

```bash
dotnet run --project src/Org.Product.WebApi/Org.Product.WebApi.csproj --launch-profile https
```

## 配置说明

主要配置在 `src/Org.Product.WebApi/appsettings.json`。

| 配置项 | 说明 |
| --- | --- |
| `ConnectionStrings:Postgres` | PostgreSQL 连接字符串 |
| `ConnectionStrings:Redis` | Redis 连接字符串 |
| `Jwt:KeyFolder` | ECDSA 密钥目录，默认 `keys` |
| `Jwt:ExpireMin` | JWT 有效期，单位分钟 |
| `Jwt:Issuer` | JWT issuer |
| `Jwt:Audience` | JWT audience |
| `OpenApiInfo` | Swagger 标题、描述和联系人信息 |
| `Serilog` | 控制台和文件日志配置 |

启动时会从 `Jwt:KeyFolder` 加载 `private-key.pem` 和 `public-key.pem`。如果文件不存在，程序会自动生成一组 ECDSA P-256 密钥。生产环境应挂载稳定密钥目录，避免重启或重新部署后旧 token 全部失效。

## 核心能力

### 用户认证

- `GET /api/home/captcha` 生成验证码。
- `POST /api/home/login` 登录并返回 JWT。
- `POST /api/home/logout` 登出并删除 Redis 中的 token 缓存。
- `POST /api/home/register` 注册用户。
- `PUT /api/user/pwd` 修改当前用户密码。
- `PUT /api/user/{userId}/pwd` 重置指定用户密码。

登录密码字段在服务层会按 Base64 解码后参与校验。开发环境下验证码校验会被跳过；非开发环境需要校验验证码答案。

每个用户只保留一个登录会话。新登录覆盖 Redis 中的旧 Token；每次请求在 JWT 签名校验后，还会检查 Token 是否与当前会话一致。登出只删除该请求对应的会话，较早的登出请求不会删除新登录。用户删除、更新、角色变更和密码变更会撤销当前会话，需要重新登录。此规则同样适用于 `super`。

### 权限与角色

项目使用基于权限字符串的授权策略，权限命名遵循：

```text
{ManagedResource}.{ManagedAction}.{Suffix}
```

示例：

- `user.get.Id`
- `user.get.Query`
- `user.put.Role`
- `role.get.All`
- `role.put.Scopes`

有效会话中的 `super` 角色拥有全部权限，但仍要求角色权限缓存存在。其他角色读取 Redis 中已维护的权限代码。权限按大小写敏感的完整代码或以点号分隔的父级代码匹配：`user` 可授权 `user.get.Id`，`user.get` 可授权用户读取接口，`get` 或 `user.g` 不匹配。

权限领域服务只读取已维护的缓存，不查询数据库、不自动回填或切换缓存版本；缓存缺失时拒绝授权。编辑侧的事务与缓存同步流程尚待实现。没有授权属性的接口默认要求登录，匿名接口必须显式声明 `[AllowAnonymous]`。

### 菜单与权限数据

菜单数据复用 `Permission` 实体，支持树结构：

- `GET /api/menu/tree` 匿名获取树形菜单。
- `POST /api/menu` 新增菜单，需要 `menu.add.New` 权限。
- `DELETE /api/menu/id/{id}` 删除菜单，需要 `menu.delete.Id` 权限。

根菜单为内置种子数据，删除菜单时会阻止删除根节点，也会阻止删除仍存在子节点的菜单。

## API 响应格式

普通接口统一返回 `ResponseWrapper<T>`：

```json
{
  "info": null,
  "data": {},
  "status": 200
}
```

异常由全局异常处理转换为异常 DTO。开发环境会返回更详细的错误信息；非开发环境会隐藏部分堆栈细节。异常文案支持资源文件本地化，当前资源文件位于：

- `src/Org.Product.WebApi/Resources/Exception.zh-CN.resx`
- `src/Org.Product.WebApi/Resources/Exception.en-US.resx`

## 数据库

`ApiDbContext` 使用 EF Core + PostgreSQL，并启用 snake_case 命名约定。启动时会通过 `InitialDatabase` 尝试创建数据库结构。

当前主要实体：

- `User`
- `Role`
- `Permission`
- `UserRole`
- `RolePermission`

内置种子数据：

- 用户：`developer`、`super`、`admin`
- 角色：`developer`、`super`、`admin`、`member`、`visitor`
- 权限/菜单：`Root`

软删除通过 `ISoftDelete` 实现。删除实现会把实体标记为 `SoftDeleted = true`，并通过全局查询过滤器隐藏已删除数据。

## 常用命令

```bash
# 构建解决方案
dotnet build Org.Product.sln

# 运行 Web API
dotnet run --project src/Org.Product.WebApi/Org.Product.WebApi.csproj

# 使用 https profile 运行
dotnet run --project src/Org.Product.WebApi/Org.Product.WebApi.csproj --launch-profile https

# 发布 Release 包
dotnet publish src/Org.Product.WebApi/Org.Product.WebApi.csproj -c Release

# 运行测试
dotnet test
```

## Docker

Web API 的 Dockerfile 位于：

```text
src/Org.Product.WebApi/Dockerfile
```

从仓库根目录构建镜像：

```bash
docker build -f src/Org.Product.WebApi/Dockerfile -t template-web-backend .
```

部署示例在 `deployment/docker-compose.yaml`，包含：

- `database`: PostgreSQL
- `cache`: Redis
- `queue`: Eclipse Mosquitto
- `file`: SFTP
- `proxy`: Nginx/前端代理示例
- `webapi`: Web API 容器示例

实际部署前需要替换镜像名、连接字符串、JWT 密钥目录和外部挂载路径。

## 开发约定

- 代码使用 nullable reference types 和 implicit usings。
- 类、方法、属性使用 `PascalCase`。
- 私有字段使用 `_camelCase`。
- 接口使用 `I` 前缀。
- DTO 使用 `*Dto` 后缀。
- 实体基类位于 `Domain/Entities/Base/`。
- 通用异常优先使用 `Org.Product.Domain.Shared.Exceptions` 下的自定义异常。
- 数据库表名和列名通过 EF Core 约定保持 snake_case。

新增业务功能时建议按以下顺序落地：

1. 在 Domain 中添加实体、值对象或领域服务接口。
2. 在 Infrastructure 中补充 EF 配置或领域服务实现。
3. 在 Application 中添加 DTO、服务接口、服务实现和 AutoMapper Profile。
4. 在 WebApi 中添加 Controller 和权限策略。
5. 补充 Swagger 注释、配置项和测试。

## 测试

`Org.Product.sln` 包含认证授权回归测试项目：

```text
src/Org.Product.Tests/
```

运行全部测试：

```bash
dotnet test
```

测试使用 ASP.NET Core TestServer 验证实际 JWT 认证、授权和 Controller 路由；Redis 与仓储使用测试替身，不连接开发数据库或 Redis。覆盖会话替换、登出、权限匹配、权限缓存只读行为、用户角色和密码变更。它们不替代完整应用启动、真实数据库模型及 Redis 集成验证。

测试命名描述行为，例如：

```text
CreateUser_WhenNameExists_ThrowsBadRequestException
```

## 安全注意事项

- 不要提交生产环境密钥、数据库密码、Redis 密码或 JWT 私钥。
- `appsettings.json` 中的默认连接字符串只适合本地开发。
- 生产环境应通过环境变量、密钥管理系统或部署挂载覆盖敏感配置。
- JWT 密钥文件应长期稳定保存，并限制读取权限。
- 修改鉴权逻辑后，需要验证权限字符串、角色权限关联和 Swagger 可见接口是否一致。
