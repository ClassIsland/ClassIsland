#!/usr/bin/env bash

set -euo pipefail

if [[ "$#" -ne 3 ]]; then
  echo "Usage: $0 <ipa-path> <application-id> <runtime-identifier>" >&2
  exit 64
fi

readonly ipa_path="$1"
readonly application_id="$2"
readonly runtime_identifier="$3"

if [[ ! -f "$ipa_path" ]]; then
  echo "::error::Unsigned IPA was not produced at $ipa_path"
  exit 1
fi

verify_root="${RUNNER_TEMP:-${TMPDIR:-/tmp}}"
verify_directory="$(mktemp -d "$verify_root/classisland-verify-ipa.XXXXXX")"
trap 'rm -rf "$verify_directory"' EXIT

/usr/bin/ditto -x -k "$ipa_path" "$verify_directory"
shopt -s nullglob
app_bundles=("$verify_directory"/Payload/*.app)
if [[ "${#app_bundles[@]}" -ne 1 ]]; then
  echo "::error::The IPA must contain exactly one app bundle"
  exit 1
fi

app_bundle="${app_bundles[0]}"
extension_bundle="$app_bundle/PlugIns/ClassIslandLiveActivityExtension.appex"
bridge_bundle="$app_bundle/Frameworks/ClassIslandLiveActivityBridge.framework"
bridge_binary="$bridge_bundle/ClassIslandLiveActivityBridge"
miniaudio_binary="$app_bundle/Frameworks/miniaudio.framework/miniaudio"
miniaudio_resolver_alias="$app_bundle/runtimes/$runtime_identifier/native/miniaudio.framework/miniaudio"
privacy_manifest="$app_bundle/PrivacyInfo.xcprivacy"
debug_artifacts="$(find "$app_bundle" -type f \( -name '*.pdb' -o -name 'MonoTouchDebugConfiguration.txt' -o -name 'libxamarin-dotnet-debug*' \) -print)"
if [[ -n "$debug_artifacts" ]]; then
  echo "::error::The Release IPA contains debug artifacts:"
  echo "$debug_artifacts"
  exit 1
fi
if [[ ! -d "$extension_bundle" ]]; then
  echo "::error::The Live Activity extension is missing from the IPA"
  exit 1
fi
if [[ ! -f "$bridge_binary" ]]; then
  echo "::error::The Live Activity bridge framework is missing from the IPA"
  exit 1
fi
if compgen -G "$extension_bundle/*.debug.dylib" > /dev/null || [[ -e "$extension_bundle/__preview.dylib" ]]; then
  echo "::error::The Live Activity extension contains Xcode preview/debug loader binaries that are unsafe for external re-signing"
  exit 1
fi
if [[ ! -f "$miniaudio_binary" ]]; then
  echo "::error::The SoundFlow miniaudio framework is missing from the IPA"
  exit 1
fi
if [[ ! -x "$miniaudio_binary" ]]; then
  echo "::error::The embedded SoundFlow miniaudio binary is not executable"
  exit 1
fi
if [[ ! -L "$miniaudio_resolver_alias" || ! -e "$miniaudio_resolver_alias" ]]; then
  echo "::error::The SoundFlow iOS native resolver path is missing from the IPA"
  exit 1
fi
if [[ "$(readlink "$miniaudio_resolver_alias")" != "../../../../Frameworks/miniaudio.framework/miniaudio" ]]; then
  echo "::error::The SoundFlow iOS native resolver alias has an unexpected target"
  exit 1
fi
if [[ ! -f "$privacy_manifest" ]]; then
  echo "::error::PrivacyInfo.xcprivacy is missing from the app bundle"
  exit 1
fi
if ! /usr/bin/plutil -lint "$privacy_manifest" > /dev/null; then
  echo "::error::PrivacyInfo.xcprivacy is not a valid property list"
  exit 1
fi

assert_privacy_reason() {
  local expected_api_type="$1"
  local expected_reason="$2"
  local declaration_index=0
  local actual_api_type
  while actual_api_type="$(/usr/libexec/PlistBuddy -c "Print :NSPrivacyAccessedAPITypes:$declaration_index:NSPrivacyAccessedAPIType" "$privacy_manifest" 2>/dev/null)"; do
    if [[ "$actual_api_type" == "$expected_api_type" ]]; then
      local reason_index=0
      local actual_reason
      while actual_reason="$(/usr/libexec/PlistBuddy -c "Print :NSPrivacyAccessedAPITypes:$declaration_index:NSPrivacyAccessedAPITypeReasons:$reason_index" "$privacy_manifest" 2>/dev/null)"; do
        if [[ "$actual_reason" == "$expected_reason" ]]; then
          return
        fi
        ((reason_index += 1))
      done
      echo "::error::PrivacyInfo.xcprivacy does not declare reason $expected_reason for $expected_api_type"
      exit 1
    fi
    ((declaration_index += 1))
  done
  echo "::error::PrivacyInfo.xcprivacy does not declare $expected_api_type"
  exit 1
}

assert_privacy_reason "NSPrivacyAccessedAPICategoryUserDefaults" "CA92.1"
assert_privacy_reason "NSPrivacyAccessedAPICategoryFileTimestamp" "C617.1"
assert_privacy_reason "NSPrivacyAccessedAPICategorySystemBootTime" "35F9.1"

assert_privacy_collected_type() {
  local expected_type="$1"
  local declaration_index=0
  local actual_type
  while actual_type="$(/usr/libexec/PlistBuddy -c "Print :NSPrivacyCollectedDataTypes:$declaration_index:NSPrivacyCollectedDataType" "$privacy_manifest" 2>/dev/null)"; do
    if [[ "$actual_type" == "$expected_type" ]]; then
      return
    fi
    ((declaration_index += 1))
  done
  echo "::error::PrivacyInfo.xcprivacy does not declare collected data type $expected_type"
  exit 1
}

if [[ "$(/usr/libexec/PlistBuddy -c 'Print :NSPrivacyTracking' "$privacy_manifest")" != "false" ]]; then
  echo "::error::PrivacyInfo.xcprivacy must declare NSPrivacyTracking=false"
  exit 1
fi
assert_privacy_collected_type "NSPrivacyCollectedDataTypeCrashData"
assert_privacy_collected_type "NSPrivacyCollectedDataTypePerformanceData"
assert_privacy_collected_type "NSPrivacyCollectedDataTypeOtherDiagnosticData"
assert_privacy_collected_type "NSPrivacyCollectedDataTypeProductInteraction"

app_bundle_id="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleIdentifier' "$app_bundle/Info.plist")"
app_display_name="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleDisplayName' "$app_bundle/Info.plist")"
extension_bundle_id="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleIdentifier' "$extension_bundle/Info.plist")"
if [[ "$app_bundle_id" != "$application_id" ]]; then
  echo "::error::The app bundle ID is $app_bundle_id, expected $application_id"
  exit 1
fi
if [[ "$extension_bundle_id" != "$application_id.LiveActivityExtension" ]]; then
  echo "::error::The extension bundle ID is $extension_bundle_id, expected $application_id.LiveActivityExtension"
  exit 1
fi
if [[ "$app_display_name" != "ClassIsland" ]]; then
  echo "::error::The app display name is $app_display_name, expected ClassIsland"
  exit 1
fi
if [[ "$(/usr/libexec/PlistBuddy -c 'Print :UIFileSharingEnabled' "$app_bundle/Info.plist")" != "true" ]]; then
  echo "::error::The app does not expose its Documents directory to the Files app"
  exit 1
fi
if [[ "$(/usr/libexec/PlistBuddy -c 'Print :LSSupportsOpeningDocumentsInPlace' "$app_bundle/Info.plist")" != "true" ]]; then
  echo "::error::The app does not support opening Documents content in place"
  exit 1
fi
if [[ "$(/usr/libexec/PlistBuddy -c 'Print :NSSupportsLiveActivities' "$app_bundle/Info.plist")" != "true" ]]; then
  echo "::error::The app does not declare Live Activity support"
  exit 1
fi
if [[ "$(/usr/libexec/PlistBuddy -c 'Print :NSSupportsLiveActivities' "$extension_bundle/Info.plist")" != "true" ]]; then
  echo "::error::The extension does not declare Live Activity support"
  exit 1
fi
if [[ "$(/usr/libexec/PlistBuddy -c 'Print :NSExtension:NSExtensionPointIdentifier' "$extension_bundle/Info.plist")" != "com.apple.widgetkit-extension" ]]; then
  echo "::error::The Live Activity extension does not use the WidgetKit extension point"
  exit 1
fi

assert_unsigned_bundle() {
  local bundle_path="$1"
  local bundle_name="$2"
  if [[ -e "$bundle_path/embedded.mobileprovision" || -d "$bundle_path/_CodeSignature" ]]; then
    echo "::error::$bundle_name unexpectedly contains signing data"
    exit 1
  fi
}

assert_unsigned_bundle "$app_bundle" "The main app"
assert_unsigned_bundle "$extension_bundle" "The Live Activity extension"
assert_unsigned_bundle "$bridge_bundle" "The Live Activity bridge"
assert_unsigned_bundle "$app_bundle/Frameworks/miniaudio.framework" "The SoundFlow miniaudio framework"

app_executable="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleExecutable' "$app_bundle/Info.plist")"
extension_executable="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleExecutable' "$extension_bundle/Info.plist")"
app_binary="$app_bundle/$app_executable"
extension_binary="$extension_bundle/$extension_executable"

expected_bridge_install_name="@rpath/ClassIslandLiveActivityBridge.framework/ClassIslandLiveActivityBridge"
bridge_install_name="$(/usr/bin/otool -D "$bridge_binary" | awk 'NR > 1 && !found { value = $0; found = 1 } END { gsub(/^[[:space:]]+|[[:space:]]+$/, "", value); print value }')"
if [[ "$bridge_install_name" != "$expected_bridge_install_name" ]]; then
  echo "::error::The Live Activity bridge install name is $bridge_install_name, expected $expected_bridge_install_name"
  exit 1
fi

expected_miniaudio_install_name="@rpath/miniaudio.framework/miniaudio"
miniaudio_install_name="$(/usr/bin/otool -D "$miniaudio_binary" | awk 'NR > 1 && !found { value = $0; found = 1 } END { gsub(/^[[:space:]]+|[[:space:]]+$/, "", value); print value }')"
if [[ "$miniaudio_install_name" != "$expected_miniaudio_install_name" ]]; then
  echo "::error::The SoundFlow miniaudio install name is $miniaudio_install_name, expected $expected_miniaudio_install_name"
  exit 1
fi

if ! /usr/bin/otool -l "$app_binary" | awk -v expected="$expected_miniaudio_install_name" '
  $1 == "cmd" { load_command = $2 }
  $1 == "name" && $2 == expected && load_command == "LC_LOAD_DYLIB" { found = 1 }
  END { exit found ? 0 : 1 }
'; then
  echo "::error::The main app does not load the embedded SoundFlow miniaudio framework"
  exit 1
fi

for symbol in ma_context_init sf_allocate_context; do
  if ! /usr/bin/nm -gUj "$miniaudio_binary" | awk -v expected="_$symbol" '
    $0 == expected { found = 1 }
    END { exit found ? 0 : 1 }
  '; then
    echo "::error::The SoundFlow miniaudio framework does not export $symbol"
    exit 1
  fi
done

for symbol in ci_live_activity_get_availability ci_live_activity_publish_json ci_live_activity_end ci_live_activity_cancel; do
  if ! /usr/bin/nm -gUj "$bridge_binary" | awk -v expected="_$symbol" '
    $0 == expected { found = 1 }
    END { exit found ? 0 : 1 }
  '; then
    echo "::error::The Live Activity bridge does not export $symbol"
    exit 1
  fi
done

if ! /usr/bin/otool -l "$app_binary" | awk '
  $1 == "cmd" { in_rpath = ($2 == "LC_RPATH") }
  in_rpath && $1 == "path" && $2 == "@executable_path/Frameworks" { found = 1 }
  END { exit found ? 0 : 1 }
'; then
  echo "::error::The app cannot resolve embedded frameworks through @rpath"
  exit 1
fi

assert_arm64() {
  local binary_path="$1"
  local binary_name="$2"
  if ! /usr/bin/lipo -archs "$binary_path" | tr ' ' '\n' | grep -qx arm64; then
    echo "::error::$binary_name does not contain the arm64 architecture"
    exit 1
  fi
}

assert_arm64 "$app_binary" "The main app"
assert_arm64 "$extension_binary" "The Live Activity extension"
assert_arm64 "$bridge_binary" "The Live Activity bridge"
assert_arm64 "$miniaudio_binary" "The SoundFlow miniaudio framework"

assert_minimum_ios() {
  local binary_path="$1"
  local binary_name="$2"
  local expected_version="$3"
  local minimum_version
  minimum_version="$(/usr/bin/xcrun vtool -show-build "$binary_path" | awk '$1 == "minos" { print $2; exit }')"
  if [[ "$minimum_version" != "$expected_version" && "$minimum_version" != "$expected_version.0" ]]; then
    echo "::error::$binary_name has minimum iOS $minimum_version, expected iOS $expected_version"
    exit 1
  fi
}

assert_minimum_ios "$app_binary" "The main app" "15.0"
assert_minimum_ios "$bridge_binary" "The Live Activity bridge" "15.0"
assert_minimum_ios "$extension_binary" "The Live Activity extension" "16.1"

while IFS= read -r dependency; do
  case "$dependency" in
    @rpath/*)
      relative_dependency="${dependency#@rpath/}"
      if [[ ! -e "$app_bundle/Frameworks/$relative_dependency" ]]; then
        echo "::error::The bridge dependency $dependency is not bundled"
        exit 1
      fi
      ;;
  esac
done < <(/usr/bin/otool -L "$bridge_binary" | awk 'NR > 1 { print $1 }')

if /usr/bin/otool -l "$app_binary" | awk '
  $1 == "cmd" { load_command = $2 }
  $1 == "name" && $2 ~ /ActivityKit\.framework\/ActivityKit/ && load_command != "LC_LOAD_WEAK_DYLIB" { found = 1 }
  END { exit found ? 0 : 1 }
'; then
  echo "::error::The main app strongly links ActivityKit"
  exit 1
fi

if ! /usr/bin/otool -l "$bridge_binary" | awk '
  $1 == "cmd" { load_command = $2 }
  $1 == "name" && $2 ~ /ActivityKit\.framework\/ActivityKit/ && load_command == "LC_LOAD_WEAK_DYLIB" { found = 1 }
  END { exit found ? 0 : 1 }
'; then
  echo "::error::The Live Activity bridge does not weak-link ActivityKit"
  exit 1
fi

ipa_directory="$(cd "$(dirname "$ipa_path")" && pwd)"
ipa_basename="$(basename "$ipa_path")"
(
  cd "$ipa_directory"
  shasum -a 256 "$ipa_basename" > "$ipa_basename.sha256"
)
