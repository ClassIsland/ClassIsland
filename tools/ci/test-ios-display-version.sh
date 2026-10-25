#!/usr/bin/env bash

set -euo pipefail

script_directory="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$script_directory/ios-display-version.sh"

assert_status() {
  local expected_status="$1"
  shift

  local actual_status=0
  "$@" >/dev/null 2>&1 || actual_status=$?
  if [[ "$actual_status" -ne "$expected_status" ]]; then
    echo "Expected status $expected_status, got $actual_status: $*" >&2
    exit 1
  fi
}

assert_output() {
  local expected_output="$1"
  shift

  local actual_output
  if ! actual_output="$("$@")"; then
    echo "Command failed while expecting output $expected_output: $*" >&2
    exit 1
  fi
  if [[ "$actual_output" != "$expected_output" ]]; then
    echo "Expected output $expected_output, got $actual_output: $*" >&2
    exit 1
  fi
}

assert_output 2.1.0 resolve_ios_display_version 2.1.0
assert_output 2.1.0 resolve_ios_display_version 2.1.0.1
assert_output 2.1.0 resolve_ios_display_version v2.1.0.1
assert_output 2.1.0 resolve_ios_display_version ios-v2.1.0.1
assert_output 0.0.52 resolve_ios_display_version 0.0.52

assert_status 0 validate_ios_display_version 2.1.0
assert_status 64 validate_ios_display_version
assert_status 64 validate_ios_display_version 2.1.0 extra
assert_status 64 validate_ios_display_version 2.1
assert_status 64 validate_ios_display_version 2.1.0.1

assert_status 64 resolve_ios_display_version
assert_status 64 resolve_ios_display_version 2.1.0 extra
assert_status 64 resolve_ios_display_version ""
assert_status 64 resolve_ios_display_version 2.1
assert_status 64 resolve_ios_display_version 2.1.0.1.2
assert_status 64 resolve_ios_display_version 2.1.x
assert_status 64 resolve_ios_display_version ios-vv2.1.0
assert_status 64 resolve_ios_display_version 2.1.0-beta

assert_status 0 assert_ios_bundle_display_version app 2.1.0 2.1.0
assert_status 1 assert_ios_bundle_display_version app 2.1.1 2.1.0
assert_status 1 assert_ios_bundle_display_version "Live Activity extension" 2.1.1 2.1.0
assert_status 64 assert_ios_bundle_display_version
assert_status 64 assert_ios_bundle_display_version app 2.1.0 2.1.0 extra

assert_status 64 bash "$script_directory/verify-ios-ipa.sh" missing.ipa cn.classisland.ios ios-arm64 invalid 1

echo "iOS display-version validation tests passed."
