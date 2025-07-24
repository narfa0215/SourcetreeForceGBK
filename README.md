# SourcetreeForceGBKTool

## 1. Overview / 项目概述

SourcetreeForceGBKTool is a tool designed to inject a patch into SourceTree’s internal assemblies, forcing it to use **GBK** encoding for viewing source code differences, while keeping the default **UTF-8** encoding for other operations. The patch is applied using **IL (Intermediate Language) instructions**, allowing SourceTree to correctly handle Chinese characters in legacy and embedded projects without altering the global encoding settings.

SourcetreeForceGBKTool 是一个工具，旨在通过 **IL 指令** 注入补丁到 SourceTree 的内部程序集，强制其在查看源码差异时使用 **GBK** 编码，同时保持默认的 **UTF-8** 编码用于其他操作。这个补丁特别适用于老旧项目和嵌入式项目，确保在不改变全局编码设置的情况下，SourceTree 能正确处理中文字符。

## 2. Features / 功能

- **Forces GBK encoding** for viewing source code differences in SourceTree.
- **Keeps UTF-8 encoding** for other operations and source code viewing.
- Helps **view Chinese characters correctly** in legacy and embedded projects using SourceTree.
- **No global encoding change**, only affects SourceTree's diff view.
- Patch applied using **IL instructions**.

- **强制使用GBK编码**查看SourceTree中的源码差异。
- **保持UTF-8编码**用于其他操作和源码查看。
- 帮助**在老旧项目和嵌入式项目中正确显示中文字符**。
- **无需全局改变编码**，仅影响SourceTree的差异查看。
- 补丁通过**IL指令**应用。

## 3. Installation / 安装

1. **Close SourceTree** to ensure the patch is applied properly.

2. Download **ILInjector.exe** and run it.

3. Drag and drop the `SourceTree.Api.UI.Wpf.dll` file from your SourceTree installation directory into the **ILInjector.exe** window. The default path for this file is:

`C:\Users<username>\AppData\Local\SourceTree\app-3.4.23\SourceTree.Api.UI.Wpf.dll`


(Note: Replace `<username>` with your actual Windows username. The version number might differ based on your SourceTree version.)

4. Press **Enter** to apply the patch. This will modify SourceTree’s behavior to force GBK encoding when viewing code differences, while leaving UTF-8 encoding for other operations.

5. Restart SourceTree. The patch will be applied automatically every time SourceTree is launched.

---

1. **关闭 SourceTree**，确保补丁正确应用。

2. 下载并运行 **ILInjector.exe**。

3. 将 `SourceTree.Api.UI.Wpf.dll` 文件从你的 SourceTree 安装目录拖入 **ILInjector.exe** 窗口。该文件的默认路径为：

`C:\Users<用户名>\AppData\Local\SourceTree\app-3.4.23\SourceTree.Api.UI.Wpf.dll`

（注意：将 `<用户名>` 替换为你实际的 Windows 用户名，版本号可能因 SourceTree 版本的不同而有所变化。）

4. 按 **回车** 键应用补丁。这将修改 SourceTree 的编码设置，在查看源码差异时使用 GBK 编码，同时保持 UTF-8 编码用于其他操作。

5. 重启 SourceTree，工具将自动应用补丁，每次启动 SourceTree 时都能生效。

## 4. Requirements / 安装要求

- SourceTree v3.4.23 (Compatibility with newer and older versions may vary; please test accordingly)
- .NET Framework v4.5+

- SourceTree v3.4.23（与较新或较早版本的兼容性可能有所不同，请自行测试）
- .NET Framework v4.5+

## 5. Contributing / 贡献

Feel free to fork the repository, open an issue, or submit a pull request if you have improvements or fixes to propose.

欢迎分叉本仓库，提交问题或拉取请求，提出任何改进或修复建议。

## 6. License / 许可证

This project is licensed under the [MIT License](LICENSE).

本项目采用 [MIT 许可证](LICENSE)。
