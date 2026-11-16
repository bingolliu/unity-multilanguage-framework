# 多语言统一版本设计演示

一个 Unity 演示项目，用来展示「多语言统一版本」的设计思路：所有语言共享同一套场景、预制体、代码和业务数据，语言只是文本与资源的一个维度，而不是一条独立的分支或一份独立的工程。

## 设计思想

### 1. 版本只有一份

多语言不作为分支、仓库副本或独立出包渠道存在。场景、预制体、脚本、业务流程始终保持单份，语言差异被收敛到两个地方：

- 文本与字体：一份可编辑的语言表。
- 视觉资源：按语言目录组织的配对资源。

这样同一份工程只有一条版本线，避免多语言版本各自演进造成的合版、对齐和回归成本。

### 2. 语言是资源路径上的一个维度

需要随语言变化的资源（贴图、材质、字体、图集）放在约定目录下：

```
<资源根>/<资源类别>/<Language>/<文件名>
```

除语言目录段之外，相对路径与文件名保持一致。切换语言时只替换路径中的语言段，因此不需要维护「语言 → 资源」的显式映射表，新增语言或新增资源都只是往目录里放文件。

场景和预制体本身不按语言复制：一个预制体只保留一份，内部引用仍指向某个语言目录；切换语言时替换的是它引用的叶子资源。

### 3. 文本与字体是数据，不是代码

语言表是普通的 ScriptableObject 资产，可在 Inspector 中直接编辑，不依赖外部插件或导表工具。每条文案以 key 标识，七种语言各占一列；字体映射也放在同一份资产中，分别是 UGUI 字体与 NGUI 字体。

### 4. 编辑器与运行时共用同一套语言状态

编辑器里预览的语言、运行时使用的语言都通过同一个入口读取和切换。编辑器改动会立即反映到已打开的场景与预制体，方便在保存前逐项目视检查；运行时通过事件通知所有本地化组件刷新。

### 5. 缺失即保持，替换不越界

目标语言缺少某个配对资源时，不会清空引用或回退到硬编码资源，而是保留当前引用；所有替换只发生在声明的资源根目录内，不会误改演示范围之外的资产。

## 方案分层

| 层次 | 负责内容 | 演示位置 |
| --- | --- | --- |
| 文本层 | 按 key 与语言取文案，缺失时使用组件上的兜底文本 | `LocalizationCatalog`、`LocalizedText` |
| 字体层 | 每种语言对应一组 UGUI / NGUI 字体 | `LocalizationCatalog` 的字体映射 |
| 资源层 | 贴图、材质、图集按语言目录配对 | `Assets/LocalizationDemo/Resources/` |
| 编辑器层 | 语言切换入口、路径解析、导入后与切语言时的引用对齐 | `Assets/LocalizationDemo/Editor/` |

## 目录结构

```
Assets/
├─ Addons/NGUI/                     NGUI 运行库
└─ LocalizationDemo/
   ├─ Scenes/LocalizationDemo.unity 演示场景（UGUI / NGUI / 模型 / 特效）
   ├─ Runtime/
   │  ├─ DemoLanguage.cs            七种语言枚举：CN / TC / VN / EN / KR / TH / RU
   │  ├─ LocalizationCatalog.cs     语言表与字体映射的数据结构
   │  ├─ LocalizationRuntime.cs     语言状态、持久化、切换事件
   │  └─ LocalizedText.cs           支持 UGUI Text 与 NGUI UILabel 的本地化组件
   ├─ Editor/
   │  ├─ LocalizationReferenceDemoWindow.cs    语言切换窗口
   │  ├─ LanguageAssetPathResolver.cs          语言目录路径解析
   │  └─ LanguageAwareImportProcessor.cs       资源导入后的引用刷新
   └─ Resources/
      ├─ Localization/
      │  ├─ LocalizationCatalog.asset          语言表（文案 + 字体）
      │  ├─ <Language>/                         按语言配对的 UI 贴图
      │  └─ Fonts/<Language>/                  按语言配对的字体
      ├─ Models/
      │  ├─ Materials/<Language>/              按语言配对的模型材质
      │  └─ Textures/<Language>/               按语言配对的模型贴图
      └─ Effects/
         ├─ Materials/<Language>/               按语言配对的特效材质
         └─ Textures/<Language>/                按语言配对的特效贴图
```

资源层的语言目录约定对七种语言统一生效，演示中的 UI 贴图、模型贴图与材质、特效贴图与材质均已按 CN / TC / VN / EN / KR / TH / RU 配齐，示例资源取自原工程 Trunk_PC 对应语言的目录；目标语言缺少配对文件时仍按「缺失即保持」处理。`Atlases/<Language>/` 为可选的 NGUI 图集目录，代码已纳入配对范围，示例工程暂未放置图集文件。

## 运行演示

1. 使用 Unity 2022.3.62f2 打开项目。
2. 打开 `Assets/LocalizationDemo/Scenes/LocalizationDemo.unity`，场景中已预置 UGUI、NGUI、模型与特效对象，无需手动搭建。
3. 通过菜单 `Demo > Localization Language` 打开切换窗口，选择目标语言。
4. 切换后，当前已打开场景中的以下引用会立即对齐：
   - UGUI `Image` 的 Sprite、UGUI `Text` 的字体；
   - NGUI `UITexture` 的贴图、`UILabel` 的 TrueType 字体、`UIAtlas` 的替换图集；
   - `Renderer` 上按语言配对的材质。
5. 演示目录下的预制体、材质和 NGUI 图集也会同步刷新，重新打开演示场景时会按当前语言再次对齐。
6. 保存场景与相关资产，即可把这些引用固化为当前语言的构建内容。

## 数据与资源约定

### 语言表

`Assets/LocalizationDemo/Resources/Localization/LocalizationCatalog.asset` 包含三部分：

- `defaultLanguage`：没有存档或存档语言非法时使用的默认语言。
- `strings`：`key` 加七种语言文本的列表，可配合 `LocalizedText.fallbackText` 使用。
- `fonts`：每种语言对应的 `uiFont`（UGUI）与 `nguiFont`（NGUI）。

运行时通过 `Resources.Load` 读取该资产，因此无需额外配置路径；也可以在菜单 `Assets > Create > Localization Demo > Localization Catalog` 新建语言表。

### 本地化组件

`LocalizedText` 可挂在 UGUI `Text` 或 NGUI `UILabel` 上，核心字段：

| 字段 | 说明 |
| --- | --- |
| `key` | 语言表中的文案键 |
| `fallbackText` | 语言表缺失该 key 时显示的内容 |
| `applyLanguageFont` | 是否同时应用当前语言的字体 |
| `catalog` | 可选的指定语言表，留空时使用运行时加载的默认表 |

组件在启用时订阅语言切换事件，在编辑器中同样生效（`ExecuteAlways` + `OnValidate`），改语言后无需进入播放模式即可看到文本与字体变化。

### 资源配对

- 语言资源根目录：`Localization/`、`Localization/Fonts/`、`Models/Textures/`、`Effects/Textures/`、`Atlases/`。
- 材质根目录：`Models/Materials/`、`Effects/Materials/`。
- 图集根目录：`Atlases/`。

配对规则是「把路径中的 `/CN/` 换成 `/目标语言/`，且目标文件必须存在」。因此：

- 文件名与相对目录必须保持一致；
- 目标不存在时保留原引用，不做删除；
- 只有位于上述根目录内的资产会参与替换。

## 切换语言时发生了什么

### 编辑器内切换

`LocalizationReferenceDemoWindow` 是唯一的切换入口，它依次完成：

1. 把当前语言写入 `ProjectSettings/LocalizationLanguage`，作为编辑器侧的持久状态。
2. 设置编辑预览语言，触发 `LocalizedText` 刷新文本与字体。
3. 遍历已打开场景中的 UGUI、NGUI、`Renderer` 与图集组件，按语言目录替换配对引用，并记录 `Undo`、标记场景为已修改。
4. 把演示资源目录中的预制体、材质和图集加入待处理队列，由 `AssetPostprocessor` 逐个刷新。

### 资源导入时

`LanguageAwareImportProcessor` 在资源导入后自动执行：

- 预制体：`Image.sprite`、`UITexture.mainTexture`、`Text.font`、`UILabel.trueTypeFont`、`Renderer` 材质、`UIAtlas.replacement`。
- 材质：遍历所有贴图属性，替换为同语言的配对贴图。
- NGUI 图集：替换 `spriteMaterial`。

它同样只在演示资源目录内工作，且只处理目标文件确实存在的引用。

### 运行时切换

```csharp
LocalizationRuntime.LanguageChanged += OnLanguageChanged;
LocalizationRuntime.SetLanguage(DemoLanguage.EN);
```

- 当前语言通过 `PlayerPrefs` 持久化，下次启动自动恢复。
- `LocalizationRuntime.SetLanguage` 校验语言合法性后触发事件，所有 `LocalizedText` 刷新文本与字体。
- 编辑器预览使用 `SetEditorPreviewLanguage` / `ClearEditorPreviewLanguage`，不会覆盖玩家的语言存档。

## 扩展方式

**新增文案**：在语言表中增加一条 `strings` 记录，填好 key 与各语言文本，再把 key 填到场景中 `LocalizedText` 上。

**新增语言字体**：在 `Fonts/<新语言>/` 放入字体文件，在语言表的 `fonts` 中登记该语言的 UGUI / NGUI 字体。

**新增配对资源**：按 `Category/<Language>/<共同文件名>` 放置文件，保证除语言段外的路径一致；随后重新导入或切换一次语言即可完成引用对齐。

**新增资源类别**：在两个编辑器处理入口（切换窗口与导入处理器）的根目录数组中补充该类别目录，其余逻辑无需改动。

**接入资源系统**：当前引用对齐发生在编辑器阶段；若需要包内多语言或按语言按需下载，可在同一路径约定之上替换资源解析实现（例如 Addressables / AssetBundle），语言状态与文本接口保持不变。

## 设计边界

- 演示聚焦「统一版本下的语言切换机制」，不绑定具体资源加载方案。
- 场景与预制体只保留一份，语言差异落在叶子资源上；不提供按语言整包复制的流程。
- 引用对齐只在声明的资源根目录内进行，目录外的资产不会被修改。
- 编辑器侧语言配置与运行时语言存档互相独立：前者用于编辑期预览，后者用于实际运行。
- 切换语言不会自动保存场景；确认引用正确后需手动保存。

## 关键文件

- [DemoLanguage.cs](Assets/LocalizationDemo/Runtime/DemoLanguage.cs)：语言枚举。
- [LocalizationCatalog.cs](Assets/LocalizationDemo/Runtime/LocalizationCatalog.cs)：语言表与字体映射结构。
- [LocalizationRuntime.cs](Assets/LocalizationDemo/Runtime/LocalizationRuntime.cs)：语言状态、持久化与切换事件。
- [LocalizedText.cs](Assets/LocalizationDemo/Runtime/LocalizedText.cs)：UGUI / NGUI 文本与字体本地化组件。
- [LocalizationReferenceDemoWindow.cs](Assets/LocalizationDemo/Editor/LocalizationReferenceDemoWindow.cs)：语言切换窗口与场景内引用对齐。
- [LanguageAssetPathResolver.cs](Assets/LocalizationDemo/Editor/LanguageAssetPathResolver.cs)：语言目录路径解析规则。
- [LanguageAwareImportProcessor.cs](Assets/LocalizationDemo/Editor/LanguageAwareImportProcessor.cs)：导入后与切换时的资源引用刷新。
## 仓库内容说明

- Unity 版本：2022.3.62f2（见 `ProjectSettings/ProjectVersion.txt`），依赖商业插件 NGUI 2023.08.01（`Assets/Addons/NGUI`）。
- 未纳入版本库：`Library/`、`Logs/`、`UserSettings/` 等 Unity 生成目录（见 `.gitignore`），以及 NGUI 自带的 `Assets/Addons/NGUI/Examples/` 示例资源（约 19MB，演示场景未引用）。需要 NGUI 示例场景时，请从原始插件包中取回该目录并重新导入。
- 首次打开工程时，请使用对应的 Unity 2022.3.62f2 版本，等待资源导入完成后再运行 `Assets/LocalizationDemo/Scenes` 下的演示场景。