# 参考与资源来源

查阅日期：2026-10-23。

| 来源 | 本项目的使用范围 |
| --- | --- |
| [Cat-Match-AR](https://github.com/Reality-Virtually-Hackathon/Cat-Match-AR) | 参考抚摸、喂食、玩耍、切换猫咪及偏好影响好感度的交互思路。其 README 说明使用 Unity 2017 与旧版 ARCore；本工程未复制代码、APK、猫模型或动画。 |
| [IMR-CatCafe](https://github.com/AndraCoteanu/IMR-CatCafe) | 参考猫咖空间主题与环境组织；未导入仓库中的模型、贴图或课程资产。 |
| [Central Purrk](https://aritrachak94.itch.io/centralpurrk) | 参考温暖、风格化、适配移动端的猫咖方向。该页提供场景查看器 APK 与作者设备帧率数据；本工程未使用其 APK 或场景资源，也不把作者的性能数据视为本工程结果。 |
| [Android XR Unity Samples](https://github.com/android/xr-unity-samples) | 查阅官方功能与版本基线，作为迁移指导；未复制示例源码。 |
| [Android XR Extensions](https://github.com/android/android-xr-unity-package) | 查阅扩展功能与配置，未作为当前运行依赖。 |
| [EmBARDiment](https://github.com/google/embardiment) | 作为后续 AI 功能参考；当前没有集成。 |

## 实际打包资源

- 猫咪、耳朵网格、家具、地板、盆栽、猫爬架、场景标记、UI 圆角图和音效均为本工程以代码生成的内容。
- `Assets/CatCafe/Resources/Fonts/CafeChinese.otf` 是 **Noto Sans CJK SC Regular**，来自 [notofonts/noto-cjk](https://github.com/notofonts/noto-cjk/tree/main/Sans/OTF/SimplifiedChinese)。
- 字体随附 **SIL Open Font License 1.1**，授权文本保存在同目录 `OFL.txt`。字体未修改。
- Unity 引擎、uGUI、Standard Shader 依其各自许可证使用。本工程没有为 Unity 或第三方资源授予额外许可。

后续导入任何开源项目的实际资产前，应逐项核对模型、贴图、动画和音频许可；仓库的代码许可不自动覆盖作者引用的 Asset Store 内容。
