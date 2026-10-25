#!/usr/bin/env bash

# 校验 ldid 签名后的 TrollStore IPA：主可执行文件必须带完整 entitlements，
# 且 app bundle 内所有 Mach-O 二进制都已签名。
#
# 用法：
#   tools/ci/verify-ios-trollstore-ipa.sh <signed-ipa> <expected-application-id>
#
# 环境变量：
#   LDID  用于读取签名的 ldid 可执行文件路径，默认使用 PATH 中的 ldid。

set -euo pipefail

if [[ "$#" -ne 2 ]]; then
  echo "Usage: $0 <signed-ipa> <expected-application-id>" >&2
  exit 64
fi

readonly ipa_path="$1"
readonly expected_application_id="$2"
readonly ldid_path="${LDID:-ldid}"

if [[ ! -f "$ipa_path" ]]; then
  echo "::error::Signed TrollStore IPA was not produced at $ipa_path"
  exit 1
fi
if ! command -v "$ldid_path" >/dev/null 2>&1; then
  echo "::error::ldid is required to verify TrollStore entitlements but was not found (tried: $ldid_path)."
  exit 1
fi
if ! command -v python3 >/dev/null 2>&1; then
  echo "::error::python3 is required to verify TrollStore entitlements."
  exit 1
fi

verify_root="${RUNNER_TEMP:-${TMPDIR:-/tmp}}"
verify_directory="$(mktemp -d "$verify_root/classisland-verify-trollstore.XXXXXX")"
trap 'rm -rf "$verify_directory"' EXIT

/usr/bin/ditto -x -k "$ipa_path" "$verify_directory"
shopt -s nullglob
app_bundles=("$verify_directory"/Payload/*.app)
if [[ "${#app_bundles[@]}" -ne 1 ]]; then
  echo "::error::The signed IPA must contain exactly one app bundle"
  exit 1
fi

app_bundle="${app_bundles[0]}"
app_executable="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleExecutable' "$app_bundle/Info.plist" 2>/dev/null || true)"
if [[ -z "$app_executable" ]]; then
  echo "::error::The signed IPA does not declare CFBundleExecutable"
  exit 1
fi
app_binary="$app_bundle/$app_executable"
if [[ ! -f "$app_binary" ]]; then
  echo "::error::The signed IPA is missing its main executable at $app_binary"
  exit 1
fi

if ! /usr/bin/file "$app_binary" | grep -q 'Mach-O'; then
  echo "::error::The app executable is not a Mach-O binary: $app_binary"
  exit 1
fi

entitlements_file="$verify_directory/entitlements.plist"
if ! "$ldid_path" -e "$app_binary" > "$entitlements_file" 2>/dev/null; then
  echo "::error::The app executable has no readable code signature; was the IPA signed with ldid?"
  exit 1
fi
if [[ ! -s "$entitlements_file" ]]; then
  echo "::error::The app executable has an empty entitlements blob; ldid signing was incomplete."
  exit 1
fi

python3 - "$entitlements_file" "$expected_application_id" "$app_bundle" <<'PYTHON'
import plistlib
import sys

entitlements_path, expected_application_id, app_bundle = sys.argv[1:4]
required_true = [
    "platform-application",
    "com.apple.private.security.no-sandbox",
    "com.apple.private.security.storage.AppDataContainers",
    "com.apple.private.security.storage.AppBundles",
    "com.apple.springboard.accessibility-window-hosting",
    "com.apple.private.persona-mgmt",
    "com.apple.developer.kernel.increased-memory-limit",
    "com.apple.QuartzCore.displayable-context",
    "com.apple.QuartzCore.secure-mode",
    "com.apple.backboard.client",
    "com.apple.springboard-ui.client",
    "com.apple.springboard.appbackgroundstyle",
    "com.apple.private.kernel.jetsam",
    "com.apple.private.memorystatus",
]
banned = [
    "com.apple.private.cs.debugger",
    "dynamic-codesigning",
    "com.apple.private.skip-library-validation",
]

errors = []
with open(entitlements_path, "rb") as handle:
    entitlements = plistlib.load(handle)
if not isinstance(entitlements, dict):
    errors.append("the embedded entitlements blob is not a dictionary")

for key in required_true:
    if entitlements.get(key) is not True:
        errors.append(f"missing required TrollStore entitlement: {key}")

for key in banned:
    if key in entitlements:
        errors.append(f"the signed app contains an entitlement banned on iOS 15/A12+: {key}")

iokit_classes = entitlements.get("com.apple.security.iokit-user-client-class")
if not isinstance(iokit_classes, list) or not iokit_classes:
    errors.append("missing com.apple.security.iokit-user-client-class used by the root HUD process")

with open(f"{app_bundle}/Info.plist", "rb") as handle:
    info = plistlib.load(handle)
if info.get("CFBundleIdentifier") != expected_application_id:
    errors.append(
        "unexpected bundle identifier: "
        f"{info.get('CFBundleIdentifier')!r} (expected {expected_application_id!r})"
    )

if errors:
    for error in errors:
        print(f"::error::{error}")
    sys.exit(1)

print(
    f"Verified TrollStore entitlements for {expected_application_id}: "
    f"{len(required_true)} required keys present, banned keys absent."
)
PYTHON

# 所有 Mach-O 二进制都必须已签名（主程序使用 TrollStore entitlements，其余用空 entitlements）。
mach_o_binaries="$verify_directory/mach-o-binaries.txt"
find "$app_bundle" -type f -exec /usr/bin/file {} + 2>/dev/null \
  | grep 'Mach-O' \
  | sed -e 's/ (for architecture [^)]*):.*$//' -e 's/: *Mach-O.*$//' \
  | sort -u > "$mach_o_binaries" || true
unsigned_count=0
while IFS= read -r candidate; do
  if [[ -z "$candidate" ]]; then
    continue
  fi
  if ! "$ldid_path" -e "$candidate" >/dev/null 2>&1; then
    echo "::error::Mach-O binary inside the signed IPA has no code signature: ${candidate#"$app_bundle"/}"
    unsigned_count=$((unsigned_count + 1))
  fi
done < "$mach_o_binaries"
if [[ "$unsigned_count" -ne 0 ]]; then
  exit 1
fi

echo "Verified ldid signatures for all Mach-O binaries inside $app_bundle."
