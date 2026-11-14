# MuPlayer

Windows 免安装单文件音乐播放器。双击 `MuPlayer.exe` 即可运行，无需安装任何运行时或依赖（使用 Windows 10/11 自带的 .NET Framework）。

## 功能

- **自动关联系统"音乐"文件夹**：启动时扫描该文件夹（含子目录）生成播放列表，支持 OneDrive 重定向后的音乐目录
- **播放控制**：点击列表即播放，播放/暂停、上一首/下一首
- **三种播放模式**：顺序播放（列表循环）、单曲循环、随机播放，按钮循环切换
- **音量控制**：滑条调节 + 一键静音
- **进度控制**：进度条点击/拖拽跳转
- **刷新列表**：音乐文件夹内容变化后一键重新扫描
- **深色自绘 UI**：无边框圆角窗口、随歌曲变色的渐变封面、播放中均衡器动画、悬停高亮歌单
- **低内存占用**：播放中工作集约 6~9MB，暂停/最小化后约 4~8MB

## 使用

| 操作 | 方式 |
|---|---|
| 播放某首歌 | 单击列表中的歌曲 |
| 播放 / 暂停 | 底部圆形按钮 |
| 上一首 / 下一首 | 圆形按钮两侧箭头 |
| 切换播放模式 | 模式按钮（顺序 → 单曲循环 → 随机） |
| 调节音量 / 静音 | 右下角滑条 / 喇叭图标 |
| 跳转进度 | 点击或拖拽进度条 |
| 移动窗口 | 拖拽顶部标题栏 |
| 调整窗口大小 | 拖拽窗口边缘 |

## 支持格式

- mp3 / wav / wma 等：走系统轻量 MCI 通道
- m4a / aac / flac 等 MCI 不支持的格式：自动回退到 WPF 解码（按需懒加载）

## 从源码构建

要求：Windows + .NET Framework 4.x（Win10/11 自带），无需 Visual Studio。

```bat
build.bat
```

或手动编译：

```bat
%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe -nologo -target:winexe -optimize+ -codepage:65001 -win32manifest:app.manifest -out:MuPlayer.exe -r:System.dll -r:System.Drawing.dll -r:System.Windows.Forms.dll -r:"%WINDIR%\Microsoft.NET\assembly\GAC_64\PresentationCore\v4.0_4.0.0.0__31bf3856ad364e35\PresentationCore.dll" -r:"%WINDIR%\Microsoft.NET\assembly\GAC_MSIL\WindowsBase\v4.0_4.0.0.0__31bf3856ad364e35\WindowsBase.dll" Program.cs
```

## 项目结构

| 文件 | 说明 |
|---|---|
| `MuPlayer.exe` | 编译产物，单文件免安装运行 |
| `Program.cs` | 全部源码（UI 自绘 + 播放后端 + 播放逻辑） |
| `app.manifest` | DPI 感知清单 |
| `build.bat` | 一键编译脚本 |

## 实现要点

- **单文件**：WinForms 自绘界面 + winmm MCI 播放后端，编译为单个 winexe（约 17KB）
- **双后端**：MCI 优先；打开失败时懒加载 WPF `MediaPlayer` 兜底，平时不加载重型解码栈
- **无闪烁歌单**：跳过原生 ListBox 背景擦除、逐项自绘；均衡器动画放在独立双缓冲覆盖层
- **DPI 适配**：按真实 DPI 统一缩放布局与绘制坐标，高分屏不糊不裁切
- **内存优化**：GDI 对象全缓存、空闲停表、秒数不变不重绘；播放 10s 后及每 60s、暂停 20s 后、最小化时做 GC + 工作集回收