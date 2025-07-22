#!/bin/bash

# CodeUnfucker 测试运行脚本
# 支持构建、运行测试、生成覆盖率报告

set -e

echo "=========================================="
echo "CodeUnfucker 单元测试运行脚本"
echo "=========================================="

# 默认参数
RUN_COVERAGE=false
VERBOSE=false
CLEAN=false

# 解析命令行参数
while [[ $# -gt 0 ]]; do
    case $1 in
        --coverage)
            RUN_COVERAGE=true
            shift
            ;;
        --verbose)
            VERBOSE=true
            shift
            ;;
        --clean)
            CLEAN=true
            shift
            ;;
        --help)
            echo "用法: $0 [选项]"
            echo "选项:"
            echo "  --coverage    生成代码覆盖率报告"
            echo "  --verbose     显示详细输出"
            echo "  --clean       清理构建输出"
            echo "  --help        显示此帮助信息"
            exit 0
            ;;
        *)
            echo "未知选项: $1"
            echo "使用 --help 查看可用选项"
            exit 1
            ;;
    esac
done

# 清理构建输出
if [ "$CLEAN" = true ]; then
    echo "🧹 清理构建输出..."
    dotnet clean
    if [ -d "TestResults" ]; then
        rm -rf TestResults
    fi
    if [ -d "coverage" ]; then
        rm -rf coverage
    fi
    echo "✅ 清理完成"
fi

# 检查 .NET 是否已安装
if ! command -v dotnet &> /dev/null; then
    echo "❌ 错误: 未找到 dotnet 命令"
    echo "请安装 .NET 9.0 SDK"
    exit 1
fi

echo "📋 检查 .NET 版本..."
dotnet --version

# 恢复依赖
echo "📦 恢复 NuGet 包..."
dotnet restore

# 构建项目
echo "🔨 构建项目..."
if [ "$VERBOSE" = true ]; then
    dotnet build --no-restore --verbosity normal
else
    dotnet build --no-restore --verbosity quiet
fi

if [ $? -ne 0 ]; then
    echo "❌ 构建失败"
    exit 1
fi

echo "✅ 构建成功"

# 运行测试
echo "🧪 运行单元测试..."

if [ "$RUN_COVERAGE" = true ]; then
    echo "📊 启用代码覆盖率收集..."
    
    # 安装报告生成工具
    if ! command -v reportgenerator &> /dev/null; then
        echo "📥 安装 ReportGenerator..."
        dotnet tool install -g dotnet-reportgenerator-globaltool
    fi
    
    # 运行测试并收集覆盖率
    dotnet test \
        --no-build \
        --collect:"XPlat Code Coverage" \
        --results-directory:"./TestResults" \
        --logger:"console;verbosity=normal" \
        --settings:coverlet.runsettings \
        -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura
    
    # 生成覆盖率报告
    echo "📊 生成覆盖率报告..."
    reportgenerator \
        -reports:"./TestResults/**/coverage.cobertura.xml" \
        -targetdir:"./coverage" \
        -reporttypes:"Html;TextSummary"
    
    echo "📊 覆盖率报告生成完成: ./coverage/index.html"
    
    # 显示覆盖率摘要
    if [ -f "./coverage/Summary.txt" ]; then
        echo ""
        echo "📈 覆盖率摘要:"
        cat "./coverage/Summary.txt"
    fi
else
    # 简单运行测试
    if [ "$VERBOSE" = true ]; then
        dotnet test --no-build --logger:"console;verbosity=normal"
    else
        dotnet test --no-build --logger:"console;verbosity=minimal"
    fi
fi

TEST_EXIT_CODE=$?

if [ $TEST_EXIT_CODE -eq 0 ]; then
    echo "✅ 所有测试通过!"
    
    # 如果有覆盖率报告，提示查看
    if [ "$RUN_COVERAGE" = true ] && [ -f "./coverage/index.html" ]; then
        echo "💡 提示: 在浏览器中打开 ./coverage/index.html 查看详细覆盖率报告"
    fi
else
    echo "❌ 测试失败"
    exit $TEST_EXIT_CODE
fi

echo "=========================================="
echo "测试完成 🎉"
echo "==========================================" 