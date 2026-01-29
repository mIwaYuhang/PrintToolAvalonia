@echo off
REM 电商打单工具发布脚本

echo ========================================
echo 电商打单工具 - 发布构建
echo ========================================
echo.

REM 清理旧的发布文件
if exist "bin\Release\net10.0\win-x64\publish" (
    echo 清理旧的发布文件...
    rmdir /s /q "bin\Release\net10.0\win-x64\publish"
)

echo 开始发布构建...
echo.

REM 执行发布
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:PublishReadyToRun=true /p:PublishTrimmed=true

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================
    echo 发布成功！
    echo ========================================
    echo.
    echo 发布文件位置：
    echo bin\Release\net10.0\win-x64\publish\
    echo.
    
    REM 显示文件大小
    if exist "bin\Release\net10.0\win-x64\publish\电商打单工具.exe" (
        echo 可执行文件：电商打单工具.exe
        dir "bin\Release\net10.0\win-x64\publish\电商打单工具.exe" | findstr "电商打单工具.exe"
    )
    
    echo.
    echo 按任意键打开发布目录...
    pause > nul
    explorer "bin\Release\net10.0\win-x64\publish"
) else (
    echo.
    echo ========================================
    echo 发布失败！
    echo ========================================
    echo.
    echo 请检查错误信息并重试。
    pause
)
