# Skill Wallet · AI 技能钱包

个人 AI Prompt / Skill 管理工具，纯本地运行的 Windows 桌面应用。

把常用提示词做成卡牌，收藏、分类、查看、复制，并通过 `.skillpack` 文件迁移到其他电脑。

## 当前版本

**Windows v0.6.4**，使用 C#、WPF 和 .NET Framework 4.8。无需账号、服务器或 API 密钥。

安装包见仓库的 **Releases**。下载 Windows ZIP，完整解压后运行 `SkillWallet.exe`。

## 功能

- 悬浮卡牌浏览，拖动或滚轮切换，点击卡牌复制正文，`⋯` 查看详情。
- 搜索、分类、收藏；单框粘贴提示词，通过本地规则整理卡牌信息。
- 批量选择、删除与长按拖拽排序，支持撤销上一次删除或排序。
- 自定义图片、完整封面显示、图片取色及环境光。
- 响应式窗口布局、可关闭的滑动音效。
- `.skillpack` 导入 / 导出，图片随技能库迁移，导入时跳过相同卡牌。
- 再次启动程序会唤回已打开的窗口。

## 构建

需要 Windows 10 / 11、.NET Framework 4.8 和 PowerShell。使用系统自带的 .NET Framework C# 编译器，无需额外的 NuGet 依赖。

在仓库根目录运行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
```

生成的程序位于 `dist/SkillWallet/`。封面、Logo、图标和音效都嵌入 EXE。

## 目录

```text
src/SkillWallet/   应用源码、嵌入素材和测试
docs/             用户说明
build.ps1         构建入口
```

[用户说明](docs/使用说明.md) · [开发与测试说明](src/SkillWallet/README.md)

## 数据

便携模式把技能库保存在程序旁的 `data` 文件夹。仓库不包含个人技能库、收藏设置、诊断日志或临时测试文件；这些文件已加入忽略规则。

运行时不会联网调用模型。自动整理提示词使用本地规则，正文原样保存。

## 已知行为

v0.6.4 导出覆盖已有文件时，会把旧文件额外保留为 `.bak`；原 `.skillpack` 会更新为本次导出内容。后续计划将导出覆盖与内部保存备份分开处理。

程序目前没有代码签名。本仓库暂未授予开源许可。
