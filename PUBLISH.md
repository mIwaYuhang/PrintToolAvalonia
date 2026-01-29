# 发布指南

## 发布步骤

### 方法 1：使用发布脚本（推荐）

1. 双击运行 `publish.bat` 脚本
2. 等待构建完成
3. 发布文件将位于 `bin\Release\net10.0\win-x64\publish\` 目录

### 方法 2：手动发布

在项目目录下执行以下命令：

```bash
dotnet publish -c Release -r win-x64 --self-contained true
```

## 发布配置说明

当前发布配置包含以下优化：

- **单文件发布** (`PublishSingleFile`): 将所有依赖打包到单个 exe 文件
- **自包含部署** (`SelfContained`): 包含 .NET 运行时，无需用户安装 .NET
- **AOT 编译** (`PublishReadyToRun`): 提前编译以加快启动速度
- **程序集裁剪** (`PublishTrimmed`): 移除未使用的代码以减小体积
- **目标平台**: Windows x64

## 预期文件大小

发布后的 exe 文件大小应小于 50MB。

## 测试发布版本

发布完成后，请测试以下功能：

1. ✅ 应用程序能正常启动
2. ✅ 打印机列表能正常加载
3. ✅ 文件添加和验证功能正常
4. ✅ 打印功能正常（使用真实打印机或虚拟打印机测试）
5. ✅ 配置保存和加载正常
6. ✅ 环保码管理功能正常
7. ✅ 历史记录功能正常

## 故障排除

### 如果发布失败

1. 确保已安装 .NET 10 SDK
2. 检查是否有编译错误
3. 尝试先执行 `dotnet clean` 清理项目

### 如果文件过大

1. 检查是否正确启用了 `PublishTrimmed`
2. 考虑移除不必要的依赖包
3. 检查是否有大型资源文件被包含

### 如果运行时出错

1. 检查是否所有必要的程序集都被保留（TrimmerRootAssembly）
2. 在 Debug 模式下测试以获取详细错误信息
3. 检查日志文件（如果有）

## 创建安装程序（可选）

如需创建安装程序，可以使用：

- **Inno Setup**: 免费的 Windows 安装程序制作工具
- **WiX Toolset**: 基于 XML 的安装程序工具集
- **Advanced Installer**: 商业安装程序制作工具

## 版本信息

当前版本：1.0.0

修改版本号请编辑 `PrintToolAvalonia.csproj` 文件中的 `<Version>` 标签。
