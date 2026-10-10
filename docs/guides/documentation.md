# 构建与维护文档

文档采用 DocFX，中文使用指南与源码生成的 API 文档共同构建。仓库通过本地 .NET 工具清单固定 DocFX 版本，避免依赖全局安装。

## 构建

在仓库根目录执行：

```powershell
dotnet tool restore
dotnet workload install wasm-tools
dotnet publish samples/Classic.Demo.Browser/Classic.Demo.Browser.csproj -c Release -o artifacts/pages/browser
dotnet docfx docs/docfx.json --warningsAsErrors
```

先发布预览入口供 DocFX 校验导航链接，再读取三个现有库项目生成 API 元数据并构建文档。首次使用需要还原第三方依赖、DocFX 工具和 wasm-tools 工作负载，要求可访问 NuGet。

## 本地预览

```powershell
dotnet docfx serve docs/_site --hostname 127.0.0.1 --port 8090
```

打开 http://127.0.0.1:8090/。端口被占用时改用其他端口。

## 文件组织

| 文件 | 内容 |
| --- | --- |
| docs/docfx.json | API 输入项目、内容、资源和网站配置 |
| docs/index.md | 文档首页 |
| docs/toc.yml / docs/guides/toc.yml | 导航目录 |
| docs/guides/*.md | 手写使用指南 |
| docs/api | 自动生成的 API 元数据，忽略提交 |
| docs/_site | 自动生成的网站，忽略提交 |
| .config/dotnet-tools.json | 固定版本的 DocFX 工具 |

修改公开 API 时更新相应 XML 注释，修改使用方式时同步指南。构建应无文档链接和交叉引用告警。生成的 API 文件与网站无需手工修改。

## 持续集成

Documentation and Preview 工作流构建 DocFX 文档、发布 Classic.Demo.Browser，并合并成一个 GitHub Pages 站点。master 分支的推送和该分支的手动运行会部署到 github-pages 环境；Pull Request 只构建并上传检查产物，不部署。

仓库的 Settings > Pages > Build and deployment > Source 应设为 GitHub Actions。工作流使用 GitHub 提供的临时凭据，不需要额外部署令牌。首次部署可能需要仓库管理员启用 Pages，或批准 github-pages 环境的部署。

站点根目录为文档，demo/ 为在线预览。导航栏与文档首页均提供预览入口。预览使用相对资源地址和 .NET 生成的文件指纹，适用于仓库子路径和自定义域名；.nojekyll 保留 _framework 等以下划线开头的资源目录。

## 本地合并预览产物

```powershell
dotnet publish samples/Classic.Demo.Browser/Classic.Demo.Browser.csproj -c Release -o artifacts/pages/browser
dotnet docfx docs/docfx.json --warningsAsErrors
pwsh -File scripts/Prepare-GitHubPages.ps1
dotnet docfx serve artifacts/pages/site --hostname 127.0.0.1 --port 8091
```

合并脚本仅重新生成 artifacts 下的目标目录。http://127.0.0.1:8091/ 提供文档，http://127.0.0.1:8091/demo/ 提供预览。只构建 docs/_site 时不包含 WASM 预览文件；发布站点应使用合并后的 artifacts/pages/site。
