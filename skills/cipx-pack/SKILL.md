---
name: cipx-pack
description: 打包 ClassIsland 插件为 .cipx 格式：构建、生成插件包和校验和。
---

# 打包

## 构建插件包

使用以下命令打包：

```pwsh
dotnet publish -p:CreateCipx=true
```

## 输出

运行命令后，以下文件会输出在项目目录下的 `cipx` 目录中：

- `xxx.cipx` — 插件安装包
- `checksums.md` — 校验和信息

## 发布与校验建议

- 在发布前先本地校验包内容与校验和：

```pwsh
# 生成 SHA256 校验和示例
Get-FileHash .\cipx\xxx.cipx -Algorithm SHA256 | Format-List
```

- 将生成的 `.cipx` 上传到 GitHub Release 或私有包源时，确保 `artifactName` 与 `checksums.md` 中名称一致。
- 建议在 CI 中自动化执行 `dotnet publish -p:CreateCipx=true`，并将 `cipx/` 下产物作为 release artifact。

## 参考文档

- 官方插件打包说明（参考）： https://docs.classisland.tech/dev/plugins/packaging.html
- GitHub Releases 上传说明： https://docs.github.com/en/repositories/releasing-projects-on-github/about-releases
