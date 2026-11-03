# Android XR 迁移说明

当前工程是普通 Android 3D 应用。**未安装或启用 XR provider、AR Foundation、ARCore、XR Hands 或 Android XR Extensions。** 本文记录接入顺序，不能当作已完成的 XR 验证。

## 已有接入点

`Scripts/Input/SpatialCatInteractor.cs` 提供三个 SDK 无关的方法：

```csharp
bool TryInteract(Ray worldRay, CatAction action = CatAction.Pet);
bool TryPetAtPosition(Vector3 worldPosition, float radius = .06f);
void InteractSelected(CatAction action);
```

控制器或注视加捏合输入先由 XR SDK 完成追踪、权限和手势识别，然后把有效世界射线传给 `TryInteract`。指尖接触由 SDK 适配器传入世界坐标；适配器需要只在新的接触或有效抚摸手势时调用，避免持续停留自动刷互动。状态层的冷却仅是业务限制。

猫咪的 `BoxCollider` 是当前简化命中范围。正式手部抚摸应细分头部、背部等触碰区域，处理双手选择、防误触和触碰退出。

## 建议接入顺序

1. 在独立版本分支将渲染管线迁移到 URP。运行时材质工厂已有 URP Lit 查找分支，但仍需配置 URP Asset、Renderer、阴影、颜色空间和构建 Shader 保留；已有查找分支不能代替管线迁移。移除构建器对 Standard 的强制保留要求。
2. 对照官方示例安装经过兼容性确认的 Android XR: OpenXR、OpenXR Plugin、XR Hands、XR Interaction Toolkit；按需添加 Android XR Extensions。版本以接入当天官方兼容矩阵和项目验证结果为准。
3. 使用 XR Origin 与追踪相机替代 `CafeBootstrap` 中的固定相机和 `CafeOrbitInput`；保持单个 AudioListener，禁用手机拖动镜头。
4. 把 `CafeHud` 从 Screen Space Overlay 改为 World Space Canvas，添加 XR UI 射线支持，重新验证文字大小、面板距离与手部可达范围。
5. 先让完整猫咖在头显中显示并可通过射线选择；再加入捏合和指尖接触，调用上面的共用动作接口。
6. 增加真实环境模式：启用透视、平面或场景理解、合法放置与重定位；完整猫咖和真实环境养猫应作为清楚的场景模式处理。加入空间尺度、锚点丢失、权限拒绝和追踪丢失状态。
7. 真机检查双目画面、性能、阴影、交互范围、失焦暂停与恢复；按目标设备调整 API level、图形 API 和 Manifest。

## 可选 AI 猫咪

EmBARDiment 提供 Android / Android XR 的 AI 能力工具包，可作为后续语音和模型适配来源。当前工程没有 AI 请求和密钥。正式集成前需确定本地还是云端推理、语音授权、响应延迟和失败回退，再把允许的语义动作映射到 `CafeSession.Interact`。

## 官方资料

- [Android XR Unity Samples](https://github.com/android/xr-unity-samples)：参考实际 XR Origin、输入、透视和手势配置。
- [Android XR Extensions for Unity](https://github.com/android/android-xr-unity-package)：按功能文档接入扩展。
- [EmBARDiment](https://github.com/google/embardiment)：后续 AI 和语音集成参考。

本工程的 Unity 版本基于本次查阅到的官方示例 `ProjectVersion.txt`：6000.3.14f1。没有直接安装官方示例中的整套 XR 依赖，以保留普通 Android 第一版的独立性。
