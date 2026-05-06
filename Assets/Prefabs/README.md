# 预制体创建指南

在 Unity 编辑器中创建以下预制体：

## MainMenuPanel.prefab
- 创建 UI > Panel，命名为 "MainMenuPanel"。
- 添加 MainMenuPanel 脚本组件。
- 在 Panel 下添加两个 UI > Button：
  - 第一个：命名为 "startButton"，文本 "开始游戏"。
  - 第二个：命名为 "quitButton"，文本 "退出游戏"。
- 将 Panel 拖到 Prefabs 文件夹创建预制体。

## GamePanel.prefab
- 创建 UI > Panel，命名为 "GamePanel"。
- 添加 GamePanel 脚本组件。
- 在 Panel 下添加：
  - UI > Text：命名为 "scoreText"，显示分数。
  - UI > Button：命名为 "pauseButton"，文本 "暂停"。
- 将 Panel 拖到 Prefabs 文件夹创建预制体。

注意：脚本会自动通过名字查找 UI 元素，无需手动赋值。

路径：Assets/Resources/Prefabs/