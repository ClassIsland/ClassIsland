#!/usr/bin/env bash

set -eo pipefail

COLOR_CYAN="\033[36m"
COLOR_GREEN="\033[32m"
COLOR_YELLOW="\033[33m"
COLOR_RED="\033[31m"
COLOR_DARKGRAY="\033[90m"
COLOR_RESET="\033[0m"

function echo_info() {
  echo -e "${COLOR_CYAN}== $1 ==${COLOR_RESET}"
}

function echo_success() {
  echo -e "${COLOR_GREEN}$1${COLOR_RESET}"
}

function echo_warning() {
  echo -e "${COLOR_YELLOW}$1${COLOR_RESET}"
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
DOTNET_INSTALL_URL="https://dot.net/v1/dotnet-install.sh"
DOTNET_INSTALL_DIR="${DOTNET_INSTALL_DIR:-$HOME/.dotnet}"
DOTNET_INSTALL_FILE="${TMPDIR:-/tmp}/dotnet-install.sh"
DOTNET_CHANNELS=("8.0" "9.0")

cd "$PROJECT_ROOT"

function version_ge() {
  local current="$1"
  local required="$2"
  local current_major current_minor required_major required_minor
  IFS=. read -r current_major current_minor _ <<< "$current"
  IFS=. read -r required_major required_minor _ <<< "$required"
  current_minor=${current_minor:-0}
  required_minor=${required_minor:-0}

  if (( current_major > required_major )); then
    return 0
  fi

  if (( current_major == required_major && current_minor >= required_minor )); then
    return 0
  fi

  return 1
}

function ensure_dotnet_on_path() {
  if [ -x "$DOTNET_INSTALL_DIR/dotnet" ]; then
    export DOTNET_ROOT="$DOTNET_INSTALL_DIR"
    export PATH="$DOTNET_INSTALL_DIR:$PATH"
  fi
}

function ensure_dotnet_install_script() {
  if [ ! -f "$DOTNET_INSTALL_FILE" ]; then
    echo_debug "正在下载 dotnet-install.sh 到 $DOTNET_INSTALL_FILE"
    curl -fsSL "$DOTNET_INSTALL_URL" -o "$DOTNET_INSTALL_FILE"
    chmod +x "$DOTNET_INSTALL_FILE"
  fi
}

function installed_dotnet_sdks() {
  if command -v dotnet &>/dev/null; then
    dotnet --list-sdks 2>/dev/null || true
  fi
}

function has_dotnet_sdk_channel() {
  local channel="$1"
  installed_dotnet_sdks | grep -Eq "^${channel//./\\.}\\."
}

function install_dotnet_sdk_channel() {
  local channel="$1"
  ensure_dotnet_install_script
  mkdir -p "$DOTNET_INSTALL_DIR"
  "$DOTNET_INSTALL_FILE" --channel "$channel" --install-dir "$DOTNET_INSTALL_DIR" --no-path
  ensure_dotnet_on_path
}

echo_info "[1/6] 检查 macOS 系统版本"

MACOS_VERSION=$(sw_vers -productVersion)
echo_debug "当前 macOS 版本：$MACOS_VERSION"

if ! version_ge "$MACOS_VERSION" "12.0"; then
  echo_error "当前系统版本为 macOS $MACOS_VERSION，最低要求为 macOS 12 Monterey。"
  exit 1
fi

if version_ge "$MACOS_VERSION" "14.0"; then
  echo_success "系统版本满足推荐要求：macOS 14 Sonoma 或更高版本。"
else
  echo_warning "系统版本满足最低要求；推荐升级到 macOS 14 Sonoma 或更高版本。"
fi

echo_info "[2/6] 检查 .NET 8.0 SDK 与 .NET 9.0 SDK"

ensure_dotnet_on_path

if ! command -v dotnet &>/dev/null; then
  echo_warning "未检测到 dotnet 命令，将安装 .NET SDK 到 $DOTNET_INSTALL_DIR"
fi

MISSING_DOTNET_CHANNELS=()
for channel in "${DOTNET_CHANNELS[@]}"; do
  if has_dotnet_sdk_channel "$channel"; then
    echo_success "已安装 .NET $channel SDK"
  else
    MISSING_DOTNET_CHANNELS+=("$channel")
  fi
done

if [ "${#MISSING_DOTNET_CHANNELS[@]}" -gt 0 ]; then
  echo_warning "缺少 .NET SDK：${MISSING_DOTNET_CHANNELS[*]}，正在安装 .NET 8.0 与 .NET 9.0 到 $DOTNET_INSTALL_DIR"
  for channel in "${DOTNET_CHANNELS[@]}"; do
    install_dotnet_sdk_channel "$channel"
  done
fi

for channel in "${DOTNET_CHANNELS[@]}"; do
  if has_dotnet_sdk_channel "$channel"; then
    echo_success ".NET $channel SDK"
  else
    echo_error ".NET $channel SDK 安装后仍未检测到。"
    exit 1
  fi
done

INSTALLED_DOTNET_VERSIONS=$(installed_dotnet_sdks)
echo_debug "已安装的 .NET SDK 版本："
echo_debug "$INSTALLED_DOTNET_VERSIONS"

if ! command -v dotnet &>/dev/null; then
  echo_error "dotnet 命令不可用，请检查 $DOTNET_INSTALL_DIR 是否可执行。"
  exit 1
fi

echo_info "[3/6] 检查 Xcode 或 Xcode-beta"

if [ -d "/Applications/Xcode.app" ] || [ -d "/Applications/Xcode-beta.app" ]; then
  if xcodebuild -version &>/dev/null; then
    XCODE_VER=$(xcodebuild -version | head -n1)
    echo_success "已安装 $XCODE_VER"
  else
    echo_success "已检测到 Xcode/Xcode-beta 应用。"
  fi
else
  echo_error "未检测到 Xcode 或 Xcode-beta，请从 App Store 或 Apple Developer 下载并安装。"
  exit 1
fi

echo_info "[4/6] 检查 Command Line Tools"

if ! xcode-select -p &>/dev/null; then
  echo_warning "Xcode Command Line Tools 未安装，正在启动安装程序..."
  xcode-select --install
  echo_error "Command Line Tools 安装需要在弹出的系统窗口中完成；安装完成后请重新运行此脚本。"
  exit 1
else
  echo_success "Xcode Command Line Tools 路径为：$(xcode-select -p)"
fi

echo_info "[5/6] 检查 .NET macOS 工作负载"

if dotnet workload list | grep -Eq '^[[:space:]]*macos[[:space:]]'; then
  echo_success "已安装 .NET macOS 工作负载"
else
  echo_warning "未检测到 .NET macOS 工作负载，正在自动安装..."
  dotnet workload install macos
fi

echo_info "[6/6] dotnet restore"

dotnet restore

echo_success "全部环境准备完成，dotnet restore 已完成。"
