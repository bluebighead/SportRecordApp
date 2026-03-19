# SportRecordApp

一个使用 .NET MAUI 开发的运动记录应用程序。

## 功能特性

- 创建和管理运动项目
- 每日打卡记录
- 查看打卡详情
- 设置页面支持自定义配置
- 支持"每天只可打卡一次"限制（默认开启）

## 技术栈

- **框架**: .NET MAUI
- **语言**: C#
- **目标平台**: Android、iOS、MacCatalyst、Windows
- **设计模式**: MVVM

## 项目结构

```
SportRecordApp/
├── Models/              # 数据模型
├── Pages/               # 页面文件
├── Platforms/           # 平台特定代码
├── Resources/           # 资源文件
│   ├── AppIcon/         # 应用图标
│   ├── Fonts/           # 字体
│   ├── Images/          # 图片
│   ├── Raw/             # 原始资源
│   ├── Splash/          # 启动画面
│   └── Styles/          # 样式文件
└── Services/            # 服务类
    ├── DataService.cs   # 数据服务
    └── SettingsService.cs # 设置服务
```

## 构建和运行

### 前置要求

- .NET 10.0 SDK 或更高版本
- Visual Studio 2022 或 Visual Studio Code
- 对于 Android 开发：Android SDK
- 对于 iOS 开发：Xcode（仅 macOS）

### 构建项目

```bash
dotnet build
```

### 运行 Android 版本

```bash
dotnet build -t:Run -f net10.0-android
```

### 运行 Windows 版本

```bash
dotnet build -t:Run -f net10.0-windows10.0.19041.0
```

## 许可证

MIT License
