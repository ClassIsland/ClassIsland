#!/bin/bash

set -e

if [[ "$(uname)" != "Darwin" ]]; then
  exit 1
fi

if ! command -v brew &>/dev/null; then
  /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
else
  echo "Homebrew 已安装"
fi

# 检查 .NET 8 SDK
if ! dotnet --list-sdks | grep -q "8.0"; then
  brew install --cask dotnet-sdk
  export PATH="/usr/local/share/dotnet:$PATH"
else
  echo ".NET 8.0 SDK 已安装"
fi

# 检查 Command Line Tools
if ! xcode-select -p &>/dev/null; then
  xcode-select --install
  echo "安装完成后请重新运行脚本"
  exit 1
else
  echo "Command Line Tools 已安装"
fi

# 初始化子模块
echo "初始化 Git 子模块..."
git submodule update --init --recursive

# 安装 macOS 工作负载
echo "安装 .NET macOS 工作负载..."
sudo dotnet workload install macos

# 编译运行
echo "编译并运行项目"
dotnet run --project ClassIsland.Desktop/ClassIsland.Desktop.csproj
