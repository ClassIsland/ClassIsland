#!/usr/bin/env bash

set -eo pipefail

COLOR_CYAN="\033[36m"
COLOR_GREEN="\033[32m"
COLOR_RED="\033[31m"
COLOR_DARKGRAY="\033[90m"
COLOR_RESET="\033[0m"

function echo_info() {
  echo -e "${COLOR_CYAN}== $1 ==${COLOR_RESET}"
}

function echo_success() {
  echo -e "${COLOR_GREEN}$1${COLOR_RESET}"
}

function echo_error() {
  echo -e "${COLOR_RED}$1${COLOR_RESET}"
}

function echo_debug() {
  echo -e "${COLOR_DARKGRAY}$1${COLOR_RESET}"
}

# 初始化路径
PROJECT_DIR="$(cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd)"
PROJECT_ROOT=$(cd "$PROJECT_DIR/../.." && pwd)
MAIN_PROJECT="ClassIsland.Desktop/ClassIsland.Desktop.csproj"

cd "$PROJECT_ROOT"

echo_info "[1/4] 检查 Homebrew 安装情况"

if ! command -v brew &>/dev/null; then
  echo_error "未安装 Homebrew，正在安装..."
  NONINTERACTIVE=1 /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
else
  echo_success "已安装 $(brew --version | head -n1)"
fi

echo_info "[2/4] 检查 .NET SDK 安装情况"

if ! command -v dotnet &>/dev/null; then
  echo_error ".NET SDK 未安装，正在使用 Homebrew 安装 .NET 8.0..."
  brew install dotnet-sdk@8
fi

INSTALLED_DOTNET_VERSIONS=$(dotnet --list-sdks 2>/dev/null)
echo_debug "已安装的 .NET SDK 版本："
echo_debug "$INSTALLED_DOTNET_VERSIONS"

if ! echo "$INSTALLED_DOTNET_VERSIONS" | grep -q "^8\.0"; then
  echo_error "未检测到 .NET 8.0 SDK，正在通过 Homebrew 安装 .NET 8.0 SDK..."
  brew install dotnet-sdk@8
fi

echo_info "[3/4] 检查 Xcode 与 Command Line Tools"

if [ -d "/Applications/Xcode.app" ]; then
  XCODE_VER=$(/usr/bin/xcodebuild -version | head -n1)
  echo_success "已安装 $XCODE_VER"
else
  echo_error "Xcode 未安装，请从 App Store 安装 Xcode"
fi

if ! xcode-select -p &>/dev/null; then
  echo_error "Xcode Command Line Tools 未安装，正在安装..."
  xcode-select --install
else
  echo_success "Xcode Command Line Tools 路径为：$(xcode-select -p)"
fi

echo_info "[4/4] 安装 .NET macOS 工作负载"

sudo dotnet workload install macos

echo_success "🎉 全部环境准备完成，可以开始构建和运行项目啦！Happy Coding～"
