#!/usr/bin/env bash

set -euo pipefail

script_directory="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$script_directory/ios-build-number.sh"

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

assert_status 0 validate_ios_build_number 1
assert_status 0 validate_ios_build_number 3201
assert_status 64 validate_ios_build_number
assert_status 64 validate_ios_build_number 1 2
assert_status 64 validate_ios_build_number ""
assert_status 64 validate_ios_build_number 0
assert_status 64 validate_ios_build_number -1
assert_status 64 validate_ios_build_number 1.2
assert_status 64 validate_ios_build_number invalid

assert_status 64 assert_ios_bundle_build_number
assert_status 64 assert_ios_bundle_build_number app 3201 3201 extra
assert_status 0 assert_ios_bundle_build_number app 3201 3201
assert_status 1 assert_ios_bundle_build_number app 3202 3201
assert_status 1 assert_ios_bundle_build_number "Live Activity extension" 3202 3201

assert_status 64 bash "$script_directory/verify-ios-ipa.sh"
assert_status 64 bash "$script_directory/verify-ios-ipa.sh" missing.ipa cn.classisland.ios ios-arm64
assert_status 64 bash "$script_directory/verify-ios-ipa.sh" missing.ipa cn.classisland.ios ios-arm64 2.1.0 1 extra
assert_status 64 bash "$script_directory/verify-ios-ipa.sh" missing.ipa cn.classisland.ios ios-arm64 2.1.0 invalid
assert_status 1 bash "$script_directory/verify-ios-ipa.sh" missing.ipa cn.classisland.ios ios-arm64 2.1.0 1

echo "iOS build-number validation tests passed."
