# User Management API（ASP.NET Core + CI/CD 示范）

面向课程演示的 REST API：用户增删改查、锁定/解锁，配套 GitHub Actions（单元测试、集成测试、压力测试）及 IIS 自动部署。

## 技术栈

- ASP.NET Core 8 Web API
- Entity Framework Core（开发/CI 用 **SQLite**，IIS 生产用 **SQL Server**）
- xUnit + WebApplicationFactory

## API 端点

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/api/users` | 列表 |
| GET | `/api/users/{id}` | 详情 |
| POST | `/api/users` | 创建用户 |
| PUT | `/api/users/{id}` | 修改用户 |
| POST | `/api/users/{id}/lock` | 锁定 |
| POST | `/api/users/{id}/unlock` | 解锁 |
| DELETE | `/api/users/{id}` | 删除 |
| GET | `/health` | 健康检查 |

### 创建用户示例

```json
POST /api/users
{ "email": "alice@example.com", "displayName": "Alice" }
```

## 本地运行

```powershell
cd UserManagementApi
dotnet restore
dotnet ef database update --project src/UserManagement.Infrastructure --startup-project src/UserManagement.Api
dotnet run --project src/UserManagement.Api
```

Swagger（开发环境）：`https://localhost:7xxx/swagger`

## 测试

```powershell
dotnet test tests/UserManagement.UnitTests
dotnet test tests/UserManagement.IntegrationTests
dotnet test tests/UserManagement.StressTests
```

## GitHub Actions

| 工作流 | 文件 | 说明 |
|--------|------|------|
| CI | `.github/workflows/ci.yml` | 构建 + **单元测试** + **集成测试** |
| Stress | `.github/workflows/stress-tests.yml` | CI 成功后运行 **压力测试** |
| CD | `.github/workflows/cd-iis.yml` | 部署到 **IIS**（需自托管 Runner） |

### IIS 部署前置条件

1. Windows Server / Windows 10+ 已安装 **IIS** 与 **ASP.NET Core Hosting Bundle**
2. 在 IIS 机器上注册 **GitHub Actions Self-Hosted Runner**，标签：`self-hosted`, `Windows`, `IIS`
3. 在 GitHub 仓库 **Settings → Environments → production** 配置：
   - **Variables**: `IIS_SITE_NAME`, `IIS_APP_POOL`, `IIS_PHYSICAL_PATH`
   - **Secrets**: `IIS_CONNECTION_STRING`（SQL Server 连接串）

CD 工作流会 `dotnet publish` 并执行 `deploy/deploy-to-iis.ps1` 停止应用池、复制文件、启动站点。

## 项目结构

```
UserManagementApi/
├── src/
│   ├── UserManagement.Api/          # Web API
│   ├── UserManagement.Core/         # 领域模型与服务
│   └── UserManagement.Infrastructure/  # EF Core + 仓储
├── tests/
│   ├── UserManagement.UnitTests/
│   ├── UserManagement.IntegrationTests/
│   └── UserManagement.StressTests/
├── deploy/
│   └── deploy-to-iis.ps1
└── .github/workflows/
```
