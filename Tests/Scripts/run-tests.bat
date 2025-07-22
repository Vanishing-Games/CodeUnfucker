@echo off
setlocal enabledelayedexpansion

REM CodeUnfucker 测试运行脚本 (Windows)
REM 支持构建、运行测试、生成覆盖率报告

echo ==========================================
echo CodeUnfucker 单元测试运行脚本 (Windows)
echo ==========================================

REM 默认参数
set RUN_COVERAGE=false
set VERBOSE=false
set CLEAN=false

REM 解析命令行参数
:parse_args
if "%~1"=="" goto :end_parse
if "%~1"=="--coverage" (
    set RUN_COVERAGE=true
    shift
    goto :parse_args
)
if "%~1"=="--verbose" (
    set VERBOSE=true
    shift
    goto :parse_args
)
if "%~1"=="--clean" (
    set CLEAN=true
    shift
    goto :parse_args
)
if "%~1"=="--help" (
    echo 用法: %0 [选项]
    echo 选项:
    echo   --coverage    生成代码覆盖率报告
    echo   --verbose     显示详细输出
    echo   --clean       清理构建输出
    echo   --help        显示此帮助信息
    exit /b 0
)
echo 未知选项: %~1
echo 使用 --help 查看可用选项
exit /b 1

:end_parse

REM 清理构建输出
if "%CLEAN%"=="true" (
    echo 🧹 清理构建输出...
    dotnet clean
    if exist "TestResults" rmdir /s /q "TestResults"
    if exist "coverage" rmdir /s /q "coverage"
    echo ✅ 清理完成
)

REM 检查 .NET 是否已安装
where dotnet >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ 错误: 未找到 dotnet 命令
    echo 请安装 .NET 9.0 SDK
    exit /b 1
)

echo 📋 检查 .NET 版本...
dotnet --version

REM 恢复依赖
echo 📦 恢复 NuGet 包...
dotnet restore
if %errorlevel% neq 0 (
    echo ❌ 依赖恢复失败
    exit /b 1
)

REM 构建项目
echo 🔨 构建项目...
if "%VERBOSE%"=="true" (
    dotnet build --no-restore --verbosity normal
) else (
    dotnet build --no-restore --verbosity quiet
)

if %errorlevel% neq 0 (
    echo ❌ 构建失败
    exit /b 1
)

echo ✅ 构建成功

REM 运行测试
echo 🧪 运行单元测试...

if "%RUN_COVERAGE%"=="true" (
    echo 📊 启用代码覆盖率收集...
    
    REM 检查报告生成工具
    where reportgenerator >nul 2>&1
    if %errorlevel% neq 0 (
        echo 📥 安装 ReportGenerator...
        dotnet tool install -g dotnet-reportgenerator-globaltool
    )
    
    REM 运行测试并收集覆盖率
    dotnet test ^
        --no-build ^
        --collect:"XPlat Code Coverage" ^
        --results-directory:"./TestResults" ^
        --logger:"console;verbosity=normal" ^
        -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura
    
    set TEST_EXIT_CODE=%errorlevel%
    
    if !TEST_EXIT_CODE! equ 0 (
        REM 生成覆盖率报告
        echo 📊 生成覆盖率报告...
        reportgenerator ^
            "-reports:./TestResults/**/coverage.cobertura.xml" ^
            "-targetdir:./coverage" ^
            "-reporttypes:Html;TextSummary"
        
        echo 📊 覆盖率报告生成完成: ./coverage/index.html
        
        REM 显示覆盖率摘要
        if exist "./coverage/Summary.txt" (
            echo.
            echo 📈 覆盖率摘要:
            type "./coverage/Summary.txt"
        )
    )
) else (
    REM 简单运行测试
    if "%VERBOSE%"=="true" (
        dotnet test --no-build --logger:"console;verbosity=normal"
    ) else (
        dotnet test --no-build --logger:"console;verbosity=minimal"
    )
    
    set TEST_EXIT_CODE=%errorlevel%
)

if %TEST_EXIT_CODE% equ 0 (
    echo ✅ 所有测试通过!
    
    REM 如果有覆盖率报告，提示查看
    if "%RUN_COVERAGE%"=="true" (
        if exist "./coverage/index.html" (
            echo 💡 提示: 在浏览器中打开 ./coverage/index.html 查看详细覆盖率报告
        )
    )
) else (
    echo ❌ 测试失败
    exit /b %TEST_EXIT_CODE%
)

echo ==========================================
echo 测试完成 🎉
echo ==========================================

endlocal 