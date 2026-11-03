# 空间猫咖 · Purr & Pour

产品目标是 **PICO Swan 真实环境中的 MR 猫咪陪伴**：在头显透视画面里放置虚拟猫，通过伸手、闻手、抚摸和舔手等自然互动建立陪伴关系。交互设计参考 [Cat-Match-AR](https://github.com/Reality-Virtually-Hackathon/Cat-Match-AR) 的喂食、玩耍和亲密度循环；本工程保留 PICO Swan MR 方向，没有改成手机 AR。详见 [真实环境体验规格](Docs/MR-EXPERIENCE.md)。

**当前状态（2026-10-27）：** Unity 6000.3.27f1 的 Cafe 场景已切换为单猫 MR 入口，PICO Android Loader、透视、手追踪与空间网格 Building Blocks 已配置；猫咪放置、手部数据适配和头显状态卡代码已接入。Unity Editor Play 模式可查看桌面模拟预览。**尚未构建 APK 或连接 Swan 真机；编辑器预览不含真实透视、真实手势或空间网格。**

## 打开并体验

1. 在 Unity Hub 安装 **Unity 6000.3.27f1**，勾选 **Android Build Support、Android SDK & NDK Tools、OpenJDK**。本机已安装这些组件。
2. 在 Hub 的 Projects 中选择 **Add project from disk**，添加本 README 所在的 `SpatialCatCafe` 文件夹。首次打开需要联网解析 Unity 官方 uGUI 包。
3. 等待资源导入和脚本编译。工程会配置 Android 参数，并打开 `Assets/CatCafe/Scenes/Cafe.unity`。如果没有自动打开，使用菜单 **Cat Cafe → 1. Configure project**，再选择 **2. Open cafe scene**。
4. 点击 Unity 的 **Play**。场景中的 `CafeBootstrap` 会在运行时生成一只猫、模拟放置表面和 MR 状态卡。
5. 桌面预览中点击可重新放置猫，按 `R` 进入重新放置；把鼠标放到猫附近可查看原型互动反馈。要体验真实办公室透视、手部追踪和真实表面放置，需要构建 Android APK 并在 PICO Swan 上运行。

### 操作

| 操作 | 结果 |
| --- | --- |
| 桌面预览点击模拟表面 | 放置或重新放置猫咪 |
| 桌面预览按 `R` | 进入重新放置状态 |
| Swan 上看向地面或桌面并轻捏 | 将猫咪放到空间网格表面 |
| Swan 上伸直食指、收起其余手指 | 从指尖发出引导线；猫沿有效地面／桌面慢慢跟随光点 |
| Swan 上把手靠近猫鼻 | 触发看手、闻手；停留后尝试舔手动作 |
| Swan 上用指尖抚摸猫头或身体 | 猫闭眼、轻蹭并呼噜，提升亲密度 |
| Swan 上捏合状态卡按钮 | 喂食小鱼干或玩逗猫球，触发猫咪动作并影响状态／亲密度 |
| Swan 上双手捏合并保持约 1 秒 | 重新放置猫咪 |

拿铁偏爱抚摸，芝麻偏爱喂食，布丁偏爱玩耍。保留原有猫咪资料、亲密度和存档规则；当前 MR 入口使用选中的猫咪档案，不显示旧的多猫猫咖场景。

## 生成 Android APK

Unity 菜单选择 **Cat Cafe → 3. Build Android APK**。默认输出 `Builds/SpatialCatCafe.apk`；失败会在 Console 报出错误。

macOS 也可以在工程目录执行：

```bash
./build-android.sh
```

自定义编辑器路径：

```bash
UNITY_EDITOR="/你的编辑器路径/Unity.app/Contents/MacOS/Unity" ./build-android.sh
```

构建日志位于 `Builds/android-build.log`。命令行构建需要 Unity 许可证已经激活。默认设置是 Android 8.0+、ARM64、IL2CPP、OpenGL ES 3；APK 使用本机 Unity 的开发签名。上架需要另行配置正式签名。

## 工程结构

```text
Assets/CatCafe/
  Scenes/Cafe.unity              主场景与入口脚本引用
  Scripts/Core/                 状态、互动规则、存档、启动、合成音效
  Scripts/Scene/                原创几何猫咪、动画和猫咖场景
  Scripts/Input/                触控镜头、世界射线与接触接口
  Scripts/UI/                   中文 uGUI、安全区和横竖屏排版
  Resources/Fonts/               Noto Sans CJK SC 与 SIL OFL 授权
  Editor/CafeBuild.cs            首次配置与 APK 构建菜单
Docs/
  DESIGN.md                     已实现的产品结构与交互规格
  ANDROID-XR.md                 迁移接入点与尚未实现的能力
  REFERENCES.md                 参考材料、使用范围与资源来源
  DELIVERY.md                   检查记录与待验证项
```

工程使用 **Built-in Render Pipeline** 与动态中文字体。运行时优先加载 Cat-Match-AR 项目中的 Cat Lite 资源；资源缺失时回退到工程内的 `GoldenCat.prefab`。Cat Lite 属于 Unity Asset Store 第三方资源，交接压缩包不包含该模型、贴图及其生成的依赖文件；需要继续使用时，请由同事通过自己的授权账户获取并导入。

存档位于 `Application.persistentDataPath/cat-cafe-v1.json`，保留前一次备份；暂停、退出、有效互动、设置变化时保存，每 30 秒补充保存。重新安装时是否保留数据取决于 Android 的备份与卸载行为。

**尚未验证**：Swan 真机透视合成、手部关节坐标和捏合判定、空间网格射线命中、猫咪尺度与遮挡、APK 安装启动及性能。空间锚点、Gemini 对话、账号及云存档也未实现。
