# SportRecordApp

一个使用 .NET MAUI 开发的运动记录应用程序。

## 功能特性

### 核心功能
- 创建和管理运动项目
- 每日打卡记录
- 查看打卡详情
- 设置页面支持自定义配置
- 支持"每天只可打卡一次"限制（默认开启）

### 新增功能

#### 日历提醒
- 在项目详情页菜单中添加"打开日历提醒"选项
- 自动获取手机日历读写权限
- 根据目标打卡天数自动在手机日历中添加提醒日程
- 支持全天提醒设置

#### 打卡动画效果
- 打卡成功时显示缩放动画
- 目标完成时显示庆祝动画（缩放+旋转+渐变组合）
- 提升用户交互体验

#### 打卡音效
- 打卡成功播放提示音效
- 重复打卡播放错误提示音效
- 支持自定义音效文件

#### 桌面小组件
- 提供三种尺寸小组件：小、中、大
- 小组件显示项目名称和今日打卡状态
- 点击小组件直接跳转到应用

#### 自动数据备份
- 打开应用时自动恢复数据
- 数据变更时自动备份
- 无需手动操作

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
│   └── Android/
│       ├── CalendarHelper.cs      # 日历操作帮助类
│       ├── SoundHelper.cs         # 音效播放帮助类
│       ├── CheckInWidgetProvider.cs        # 中等尺寸小组件
│       ├── CheckInWidgetSmallProvider.cs   # 小尺寸小组件
│       ├── CheckInWidgetLargeProvider.cs   # 大尺寸小组件
│       └── Resources/
│           ├── layout/            # 小组件布局
│           ├── drawable/          # 小组件样式
│           ├── raw/               # 音效文件
│           └── xml/               # 小组件配置
├── Resources/           # 资源文件
│   ├── AppIcon/         # 应用图标
│   ├── Fonts/           # 字体
│   ├── Images/          # 图片
│   ├── Raw/             # 原始资源（音效文件）
│   ├── Splash/          # 启动画面
│   └── Styles/          # 样式文件
└── Services/            # 服务类
    ├── DataService.cs        # 数据服务
    ├── SettingsService.cs    # 设置服务
    ├── AnimationService.cs   # 动画服务
    ├── CalendarService.cs    # 日历服务
    └── SoundService.cs       # 音效服务
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

## 权限说明

### Android 权限
- `READ_CALENDAR` - 读取日历事件
- `WRITE_CALENDAR` - 写入日历事件

## 自定义音效

应用支持自定义打卡音效，替换以下文件即可：
- `Resources/Raw/checkin_sound.mp3` - 打卡成功音效
- `Resources/Raw/error_sound.mp3` - 重复打卡提示音效

## 许可证

MIT License
