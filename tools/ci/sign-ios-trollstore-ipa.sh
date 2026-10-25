#!/usr/bin/env bash

# 为 TrollStore 发行版预置 ldid 签名的 IPA 后处理脚本。
#
# 用法：
#   tools/ci/sign-ios-trollstore-ipa.sh <ipa-path> <entitlements-plist>
#
# 环境变量：
#   LDID  ldid 可执行文件的路径或命令名，默认使用 PATH 中的 ldid。
#
# 处理流程：
#   1. 解包 IPA，定位 Payload 下唯一的 .app；
#   2. 用传入的 entitlements 为主可执行文件执行 ldid -S；
#   3. PlugIns 下每个 .appex 的宿主可执行文件使用 appex 自带的 Entitlements.plist，
#      没有时使用空 entitlements；
#   4. 其余嵌套 Mach-O 二进制（Frameworks、dylib 等）同样用空 entitlements 签名，
#      避免未签名嵌套二进制在 TrollStore 安装后加载失败；
#   5. 重新打包为 <原名>-TrollStore-signed.ipa，并生成同名 .sha256 文件。
#
# 只有主可执行文件携带 TrollStore 的私有 entitlements；不要把它们写入 Extension
# 或嵌套 framework。脚本仅支持 macOS（需要 ditto 与 PlistBuddy），并且不修改输入 IPA。

set -euo pipefail

usage() {
  cat >&2 <<EOF
Usage: $0 <ipa-path> <entitlements-plist>

  <ipa-path>            unsigned ClassIsland iOS IPA
  <entitlements-plist>  entitlements for the main executable
                        (TrollStore builds use ClassIsland.iOS/Entitlements.TrollStore.plist)
  LDID                  optional ldid executable, defaults to ldid on PATH
EOF
}

die() {
  echo "error: $*" >&2
  exit 1
}

if [[ "$#" -ne 2 ]]; then
  echo "error: expected 2 arguments, got $#." >&2
  usage
  exit 64
fi

ipa_path="$1"
entitlements_path="$2"
ldid_path="${LDID:-ldid}"

if [[ ! -f "$ipa_path" ]]; then
  die "IPA not found: $ipa_path"
fi
if [[ ! -f "$entitlements_path" ]]; then
  die "entitlements plist not found: $entitlements_path"
fi

if ! command -v "$ldid_path" >/dev/null 2>&1; then
  echo "error: ldid is not available (tried: $ldid_path)." >&2
  echo "Install it with 'brew install ldid' or build https://github.com/ProcursusTeam/ldid," >&2
  echo "or point the LDID environment variable at an existing ldid binary." >&2
  exit 1
fi

for required_command in ditto file; do
  if ! command -v "$required_command" >/dev/null 2>&1; then
    die "$required_command is required but was not found; run this script on macOS with Xcode command line tools installed."
  fi
done

plist_buddy="/usr/libexec/PlistBuddy"
if [[ ! -x "$plist_buddy" ]]; then
  die "$plist_buddy is required but was not found; run this script on macOS with Xcode command line tools installed."
fi

if command -v plutil >/dev/null 2>&1 && ! plutil -lint "$entitlements_path" >/dev/null; then
  die "entitlements plist is not a valid property list: $entitlements_path"
fi

work_root="$(mktemp -d "${TMPDIR:-/tmp}/classisland-trollstore-sign.XXXXXX")"
trap 'rm -rf "$work_root"' EXIT

extracted_root="$work_root/extracted"
ditto -x -k "$ipa_path" "$extracted_root"

shopt -s nullglob
app_bundles=("$extracted_root"/Payload/*.app)
if [[ "${#app_bundles[@]}" -ne 1 ]]; then
  die "the IPA must contain exactly one app bundle under Payload/, found ${#app_bundles[@]}."
fi
app_bundle="${app_bundles[0]}"

read_plist_string() {
  "$plist_buddy" -c "Print :$2" "$1" 2>/dev/null || true
}

app_bundle_name="$(basename "$app_bundle")"
app_executable="$(read_plist_string "$app_bundle/Info.plist" CFBundleExecutable)"
if [[ -z "$app_executable" ]]; then
  die "Info.plist does not declare CFBundleExecutable: $app_bundle/Info.plist"
fi
app_binary="$app_bundle/$app_executable"
if [[ ! -f "$app_binary" ]]; then
  die "app executable not found: $app_binary"
fi

empty_entitlements="$work_root/empty-entitlements.plist"
cat > "$empty_entitlements" <<'PLIST'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict/>
</plist>
PLIST

signed_binaries="$work_root/signed-binaries.txt"
: > "$signed_binaries"
signed_count=0

sign_binary() {
  local binary_path="$1"
  local signing_entitlements="$2"
  echo "signing: ${binary_path#"$app_bundle"/}"
  "$ldid_path" -S"$signing_entitlements" "$binary_path"
  echo "$binary_path" >> "$signed_binaries"
  signed_count=$((signed_count + 1))
}

# 主可执行文件是唯一需要 TrollStore entitlements 的二进制。
sign_binary "$app_binary" "$entitlements_path"

appex_count=0
for appex in "$app_bundle"/PlugIns/*.appex; do
  appex_count=$((appex_count + 1))
  appex_name="$(basename "$appex")"
  appex_executable="$(read_plist_string "$appex/Info.plist" CFBundleExecutable)"
  if [[ -z "$appex_executable" ]]; then
    echo "warning: $appex_name does not declare CFBundleExecutable; leaving it unsigned." >&2
    continue
  fi

  appex_binary="$appex/$appex_executable"
  if [[ ! -f "$appex_binary" ]]; then
    echo "warning: $appex_name host executable is missing at $appex_binary; leaving it unsigned." >&2
    continue
  fi

  if [[ -f "$appex/Entitlements.plist" ]]; then
    appex_entitlements="$appex/Entitlements.plist"
  else
    appex_entitlements="$empty_entitlements"
  fi
  sign_binary "$appex_binary" "$appex_entitlements"
done

# 已签名的二进制在 signed-binaries.txt 中，剩下的 Mach-O（framework/dylib 等）用空 entitlements 签名。
# 用一次 file 批处理识别 Mach-O，避免对大量非二进制资源逐个启动 file 进程。
mach_o_binaries="$work_root/mach-o-binaries.txt"
find "$app_bundle" -type f -exec file {} + 2>/dev/null \
  | grep 'Mach-O' \
  | sed -e 's/ (for architecture [^)]*):.*$//' -e 's/: *Mach-O.*$//' \
  | sort -u > "$mach_o_binaries" || true

while IFS= read -r candidate; do
  if [[ -z "$candidate" ]]; then
    continue
  fi
  if grep -Fqx "$candidate" "$signed_binaries"; then
    continue
  fi

  sign_binary "$candidate" "$empty_entitlements"
done < "$mach_o_binaries"

output_directory="$(cd "$(dirname "$ipa_path")" && pwd)"
ipa_basename="$(basename "$ipa_path")"
ipa_stem="${ipa_basename%.ipa}"
signed_ipa="$output_directory/${ipa_stem}-TrollStore-signed.ipa"
repacked_ipa="$work_root/repacked.ipa"

(
  cd "$extracted_root"
  ditto -c -k --keepParent Payload "$repacked_ipa"
)
mv -f "$repacked_ipa" "$signed_ipa"

sha256_command=()
if command -v shasum >/dev/null 2>&1; then
  sha256_command=(shasum -a 256)
elif command -v sha256sum >/dev/null 2>&1; then
  sha256_command=(sha256sum)
else
  die "neither shasum nor sha256sum is available to compute the SHA-256 digest."
fi

(
  cd "$output_directory"
  "${sha256_command[@]}" "$(basename "$signed_ipa")" > "$(basename "$signed_ipa").sha256"
)
sha256_digest="$("${sha256_command[@]}" "$signed_ipa" | awk '{print $1}')"

echo "signed $signed_count binaries from $app_bundle_name (${appex_count} appex bundle(s))"
echo "signed IPA: $signed_ipa"
echo "SHA-256 file: $signed_ipa.sha256"
echo "SHA-256: $sha256_digest"
