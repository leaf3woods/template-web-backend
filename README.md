# Template Web Backend

基于 .NET 8 的 Web API 后端模板项目，采用 DDD 分层组织代码，内置用户、角色、菜单/权限、JWT 鉴权、验证码、Redis 缓存、PostgreSQL 持久化、Swagger 和 Docker 部署示例。

## 技术栈

- .NET 8 / ASP.NET Core Web API
- Entity Framework Core 8 / Npgsql / PostgreSQL
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
├── TemplateWeb.sln
├── src
│   ├── Template.Web.WebApi          # API 入口、控制器、中间件、Swagger、配置
│   ├── Template.Web.Application     # 应用服务、DTO、鉴权、验证码、AutoMapper
│   ├── Template.Web.Domain          # 实体、值对象、领域服务接口、领域事件
│   ├── Template.Web.Infrastructure  # EF Core DbContext、数据库初始化、领域服务实现
│   └── Template.Web.Core            # 通用异常、分页、配置、扩展、JSON 转换器
└── deployment                       # Docker Compose、Nginx、Redis、MQTT、SFTP 配置示例
```

## 快速开始

### 环境要求

- .NET SDK 8.x
- PostgreSQL
- Redis
- Docker 可选，用于启动本地依赖或构建镜像

### 启动基础设施

仓库提供了 `deployment/docker-compose.yaml`。如果只需要本地运行 API，通常先启动 PostgreSQL 和 Redis：

```bash
cd deployment
docker compose up -d database cache
```

默认连接配置位于 `src/Template.Web.WebApi/appsettings.json`：

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
dotnet restore TemplateWeb.sln
dotnet build TemplateWeb.sln
dotnet run --project src/Template.Web.WebApi/Template.Web.WebApi.csproj
```

默认开发地址：

- HTTP: `http://localhost:5156`
- HTTPS: `https://localhost:7247`
- Swagger: `http://localhost:5156/swagger`

也可以使用 HTTPS profile：

```bash
dotnet run --project src/Template.Web.WebApi/Template.Web.WebApi.csproj --launch-profile https
```

## 配置说明

主要配置在 `src/Template.Web.WebApi/appsettings.json`。

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

### 权限与角色

项目使用基于权限字符串的授权策略，权限命名遵循：

```text
{ManagedResource}.{ManagedAction}.{Suffix}
```

示例：

- `User.Get.Id`
- `User.Get.Query`
- `User.Put.Role`
- `Role.Get.All`
- `Role.Put.Scopes`

`super` 角色拥有全部权限。其他角色会从数据库加载关联的 `Permission`，并与接口要求的 policy 匹配。

### 菜单与权限数据

菜单数据复用 `Permission` 实体，支持树结构：

- `GET /api/menu/tree` 获取树形菜单。
- `POST /api/menu` 新增菜单。
- `DELETE /api/menu/id/{id}` 删除菜单。

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

- `src/Template.Web.WebApi/Resources/Exception.zh-CN.resx`
- `src/Template.Web.WebApi/Resources/Exception.en-US.resx`

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
dotnet build TemplateWeb.sln

# 运行 Web API
dotnet run --project src/Template.Web.WebApi/Template.Web.WebApi.csproj

# 使用 https profile 运行
dotnet run --project src/Template.Web.WebApi/Template.Web.WebApi.csproj --launch-profile https

# 发布 Release 包
dotnet publish src/Template.Web.WebApi/Template.Web.WebApi.csproj -c Release

# 运行测试；当前解决方案尚未包含测试项目
dotnet test
```

## Docker

Web API 的 Dockerfile 位于：

```text
src/Template.Web.WebApi/Dockerfile
```

从仓库根目录构建镜像：

```bash
docker build -f src/Template.Web.WebApi/Dockerfile -t template-web-backend .
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
- 通用异常优先使用 `Template.Web.Core.Exceptions` 下的自定义异常。
- 数据库表名和列名通过 EF Core 约定保持 snake_case。

新增业务功能时建议按以下顺序落地：

1. 在 Domain 中添加实体、值对象或领域服务接口。
2. 在 Infrastructure 中补充 EF 配置或领域服务实现。
3. 在 Application 中添加 DTO、服务接口、服务实现和 AutoMapper Profile。
4. 在 WebApi 中添加 Controller 和权限策略。
5. 补充 Swagger 注释、配置项和测试。

## 测试

当前 `TemplateWeb.sln` 未包含测试项目。新增测试时建议创建：

```text
src/Template.Web.Tests/
```

并加入解决方案：

```bash
dotnet sln TemplateWeb.sln add src/Template.Web.Tests/Template.Web.Tests.csproj
dotnet test
```

测试命名建议描述行为，例如：

```text
CreateUser_WhenNameExists_ThrowsBadRequestException
```

## 安全注意事项

- 不要提交生产环境密钥、数据库密码、Redis 密码或 JWT 私钥。
- `appsettings.json` 中的默认连接字符串只适合本地开发。
- 生产环境应通过环境变量、密钥管理系统或部署挂载覆盖敏感配置。
- JWT 密钥文件应长期稳定保存，并限制读取权限。
- 修改鉴权逻辑后，需要验证权限字符串、角色权限关联和 Swagger 可见接口是否一致。
