# PICO CLI 与 MR 接入记录（2026-10-27）

目标：PICO Swan / PICO OS 6，真实办公室透视、空间表面与手部交互。Unity Editor 已有单猫 MR 桌面模拟预览；它不代表真机 MR 已验证。

## 已完成

- 使用已有 PICO CLI 0.6.1 public，没有按文章的旧版 0.5.0 重装。
- 在本工程执行 `pico-cli setup --platform unity --agent-tool codex --scope local --region cn --project <本工程绝对路径> --yes`。
- 安装本地 `pico-unity-agentic-tools@0.1.1`、`pico-general-agentic-tools@0.1.1`，写入工程 AGENTS、技能、知识库及 MCP 配置。
- Unity CLI 更新到 1.0.0-beta.14，Unity 知识库 6.1 静态检查通过。
- 已验证 Unity 6000.3.27f1 与 Android 模块存在；不需要重新下载 Unity。

## 初始化前的诊断与限制（历史记录）

- Unity Android SDK 位于 `/Applications/Unity/Hub/Editor/6000.3.27f1/PlaybackEngines/AndroidPlayer/SDK`。
- 临时给 doctor 传入 `ANDROID_SDK_ROOT` / `ANDROID_HOME` 后，CLI 能识别该 SDK；没有修改用户全局环境变量。文件检查确认 Android 34/35/36/37.0 平台与 build-tools 36.0.0 已安装，缺的是 Sources 目录，因此未通过 CLI 通用检查。这不等于 Unity Android 模块未安装，也尚无 Unity 构建失败证据。
- 本地 `.codex/config.toml` 已写 Unity MCP 配置，但其 relay 可执行文件路径目前不存在；配置写入不代表桥接程序已安装或连接成功。
- 当前对话未暴露 `pico_xr_status`、`pico_xr_vst`、`pico_xr_hand`、`pico_xr_spatial_mesh`、`pico_xr_plane`、`pico_xr_package`；按 buildingblocks 技能要求，在功能修改前停止，不用手改 manifest 绕过。
- 工程尚未添加 PICO XR、AI Assistant、MCP Extensions 包，未配置 Android XR Loader；未启用透视/手追/空间网格。
- 模拟器未安装，未构建或安装 MR APK，未进行头显测试。

## 初始化入口（历史记录）

安装后的 `pico-unity-init` 技能明确要求用户输入 `/pico-unity-init` 才能启动工程初始化。收到该指令后，复用当前 Unity 版本，以已有工程增量方式接入 PICO XR 和桥接依赖，保留猫咪行为代码。完成 Editor/MCP 连接后，再按 buildingblocks 技能启用透视、手部与空间能力，构建并验证模拟器；舔手和真实环境交互需要头显实测。

## 用户触发初始化后的进展

用户已明确发送 `/pico-unity-init`。已验证现有工程结构、备份源码，沿用 Unity 6000.3.27f1、PICO XR、Swan，默认普通开发版 `toC`。

已按初始化技能一次性增量写入 AI Assistant 2.17.0-pre.1、官方 MCP Extensions、PICO XR / SpatialAdapter / gltf-exporter 依赖。GitHub SSH 认证失败，MCP Extensions 使用官方 HTTPS 地址。原有依赖保留。

Unity CLI 已接受 `--build-target Android` 启动，日志确认 `Targeting platform: Android`；`.pico-cli/config.json` 已通过技能自带脚本写入，初始化标记为 true。此标记表示初始化步骤及启动命令完成，不代表 MR 功能和头显测试通过。依赖导入与编译的实际验证见下文。

### 首次编译修复与验证

- 用户已确认普通开发版本 `toC`。
- 实际下载版本：PICO XR 6.1.1、SpatialAdapter 6.1.0、MCP Extensions 0.1.1、AI Assistant 2.17.0-pre.1、glTF Exporter 1.142.0。
- 首次编译暴露初始化依赖遗漏。在桥接尚未建立的本次 bootstrap 中，按 PICO 官方 PICOXR 模板及 SDK `Editor/PXR_Utils.cs` 补齐 `com.unity.modules.androidjni` 和 PICO 配套 USD 包：`https://github.com/Pico-Developer/usd-unity-sdk.git?path=package/com.unity.formats.usd#dev`。实际 USD 版本 3.0.0-exp.5，提交 1ae37ebbc6c0558d338064c92be593445c390266。
- PICO 平台示例另需 `com.unity.modules.unitywebrequesttexture`，已按本地 Unity 内置包定义启用。上述模块均为 Unity 1.0.0 内置模块。
- 从已下载的官方 AI Assistant 包解出 Apple Silicon relay，安装至 `.codex/config.toml` 已指定的 `~/.unity/relay/relay_mac_arm64.app/Contents/MacOS/relay_mac_arm64`。这修复可执行文件缺失，不代表当前对话已加载 MCP 工具。
- Android 目标批处理日志 `~/Library/Logs/Unity/SpatialCatCafe-init-compile-2.log` 报告两轮 `Tundra build success`，并生成 `Assembly-CSharp.dll`、`ByteDance.PICO.MCPExtensions.Tools.dll`、`Unity.AI.MCP.Editor.dll`。资源导入完成，进程退出码 0，日志含 `Exiting batchmode successfully now!`，C# 编译错误为 0。
- `build-android.sh` 改为读取工程 ProjectVersion.txt 定位 Editor，移除过期 6000.3.14f1 路径；`bash -n` 通过。
- 未启用 MR 场景功能，未构建 APK，未进行模拟器或头显运行验证。

### 后续 MR 场景接入进展

在记录上述“下一阶段”状态之后，本次通过 Unity Editor 的 PICO Building Blocks 与 XR Plug-in Management 接入了 MR 场景配置，没有调用 `pico_xr_*` MCP 工具：

- Android XR Plug-in Management 的 Loader 列表已加入 `PXR_Loader`，Android Settings 保持启动时初始化 XR。
- Cafe 场景已保存 PICO Hand Tracking XR Origin、PICO VST Camera Effect 和 PICO Spatial Mesh Building Blocks。
- `CafeBootstrap` 已改为单猫 MR 入口，并增加 Editor 模拟表面、PICO 手部适配、空间网格放置控制器和头显状态卡；状态卡包含喂食、玩耍和亲密度反馈。
- Unity Editor Play 模式下可见桌面模拟预览；资源刷新与脚本编译完成，Console 未见 C# 编译错误。

这些步骤只确认项目配置和桌面模拟预览。**尚未构建 APK、授权真实环境访问、在 Swan 上验证透视／手追踪／空间网格，或测量运行性能。** 请勿把 Editor 预览当作真实透视画面。完整体验状态见 [MR 体验规格](MR-EXPERIENCE.md)。
