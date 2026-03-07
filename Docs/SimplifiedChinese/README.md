
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT) [![Discord](https://img.shields.io/badge/Discord-Join-5865F2?logo=discord&logoColor=white)](https://discord.gg/H3TACT3Buh) [![GitHub Release](https://img.shields.io/github/v/release/MeowServer/HintServiceMeow)](https://github.com/MeowServer/HintServiceMeow/releases)

## 简介
**HintServiceMeow (HSM)** 是一个用于 SCP: Secret Laboratory 的框架，允许插件在玩家屏幕的指定位置显示文字。

---

## 安装

请按照以下步骤安装该插件：

1. 进入 [发行页面](https://github.com/MeowServer/HintServiceMeow/releases)，下载最新的 `HintServiceMeow.dll` 文件，并将其粘贴到插件文件夹中。
2. 如果您使用的是 **LabAPI**（默认 API），请将 `Harmony.dll` 放入 **dependencies** 文件夹。
3. 重启服务器。
4. 根据需要调整配置。
5. 再次重启服务器以应用配置更改。

---

## 文档

以下是一些有用的资源，帮助您快速上手：

- [开始使用](/Docs/SimplifiedChinese/GettingStarted.md)
- [核心功能](/Docs/SimplifiedChinese/CoreFeatures.md)
- [更新日志](/Docs/SimplifiedChinese/CHANGELOG.md)

---

## 常见问题

### 1. 为什么插件安装后没有工作？
- 请确保 **HintServiceMeow** 已正确安装。
- 检查是否有与 **HintServiceMeow** 冲突的插件。
- 查看插件激活时是否有错误发生。

### 2. 为什么提示文字相互重叠？
- 当多个插件将提示文字放在同一位置时，可能会出现这种情况。您可以在每个插件的配置文件中调整 UI 位置。
- 如果某个插件不允许通过配置文件更改位置，请联系该插件的作者寻求帮助。

---

## 贡献者

感谢所有为 HintServiceMeow 做出贡献的人！
您的 pull request、错误报告和建议帮助这个项目持续运转。

- [@Someone](https://github.com/Someone-193) - 添加代码风格检查。
- [XLittleLeft](https://github.com/XLittleLeft) - 添加 LabAPI 支持。
- [Firething](https://github.com/Firething) - 添加葡萄牙语翻译。
