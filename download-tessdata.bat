@echo off
echo ========================================
echo Tesseract 训练数据下载工具
echo ========================================
echo.
echo 需要下载的文件:
echo   1. chi_sim.traineddata (简体中文, 约17MB)
echo   2. eng.traineddata (英文+数字, 约4MB)
echo.
echo 总大小: 约21MB
echo.
pause
echo.

REM 创建 tessdata 目录
if not exist "tessdata" mkdir tessdata

echo [1/2] 下载简体中文训练数据 (chi_sim.traineddata)...
curl -L -o tessdata\chi_sim.traineddata https://github.com/tesseract-ocr/tessdata/raw/main/chi_sim.traineddata
if %errorlevel% neq 0 (
    echo 下载失败！请检查网络连接
    pause
    exit /b 1
)

echo.
echo [2/2] 下载英文训练数据 (eng.traineddata)...
curl -L -o tessdata\eng.traineddata https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata
if %errorlevel% neq 0 (
    echo 下载失败！请检查网络连接
    pause
    exit /b 1
)

echo.
echo ========================================
echo 下载完成！
echo ========================================
echo 训练数据文件已保存到 tessdata 目录:
echo   - tessdata\chi_sim.traineddata
echo   - tessdata\eng.traineddata
echo.
echo 现在可以重新运行程序，OCR功能将正常工作
echo ========================================
pause
