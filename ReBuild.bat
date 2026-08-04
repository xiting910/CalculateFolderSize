@echo off
chcp 65001 >nul
title Clean and Build .NET Project

echo ========================================================
echo                .NET 项目清理和构建脚本
echo ========================================================

echo.
echo [1/6] 正在删除 bin 文件夹...
if exist "bin" (
    echo 删除: bin
    rmdir /s /q "bin"
)
for /f "delims=" %%i in ('dir /s /b /a:d "bin" 2^>nul') do (
    if exist "%%i" (
        echo 删除: %%i
        rmdir /s /q "%%i"
    )
)

echo.
echo [2/6] 正在删除 obj 文件夹...
if exist "obj" (
    echo 删除: obj
    rmdir /s /q "obj"
)
for /f "delims=" %%i in ('dir /s /b /a:d "obj" 2^>nul') do (
    if exist "%%i" (
        echo 删除: %%i
        rmdir /s /q "%%i"
    )
)

echo.
echo [3/6] 正在删除 publish 文件夹...
if exist "publish" (
    echo 删除: publish
    rmdir /s /q "publish"
)
for /f "delims=" %%i in ('dir /s /b /a:d "publish" 2^>nul') do (
    if exist "%%i" (
        echo 删除: %%i
        rmdir /s /q "%%i"
    )
)

echo.
echo [4/6] 正在执行 dotnet restore...
dotnet restore
if %errorlevel% neq 0 goto :error

echo.
echo [5/6] 正在执行 dotnet build...
dotnet build --no-restore
if %errorlevel% neq 0 goto :error

echo.
echo [6/6] 正在发布到根目录 publish 文件夹...
echo 发布 CLI...
dotnet publish srcs\CalculateFolderSize.Cli\CalculateFolderSize.Cli.csproj -c Release -r win-x64 --no-restore -o publish\Cli
if %errorlevel% neq 0 goto :error

echo 发布 UI.Desktop...
dotnet publish srcs\CalculateFolderSize.UI.Desktop\CalculateFolderSize.UI.Desktop.csproj -c Release -r win-x64 --no-restore -o publish\UI.Desktop
if %errorlevel% neq 0 goto :error

echo 发布 Android...
dotnet publish srcs\CalculateFolderSize.UI.Android\CalculateFolderSize.UI.Android.csproj -c Release --no-restore -o publish\Android
if %errorlevel% neq 0 goto :error

echo.
echo ========================================================
echo                    全部操作成功完成！
echo ========================================================
goto :end

:error
echo.
echo ========================================================
echo              出现错误，错误代码: %errorlevel%
echo ========================================================

:end
echo.
pause
