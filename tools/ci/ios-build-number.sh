#!/usr/bin/env bash

validate_ios_build_number() {
  if [[ "$#" -ne 1 ]]; then
    echo "::error::validate_ios_build_number requires exactly one value" >&2
    return 64
  fi

  if [[ ! "$1" =~ ^[1-9][0-9]*$ ]]; then
    echo "::error::The expected iOS build number must be a positive integer" >&2
    return 64
  fi
}

assert_ios_bundle_build_number() {
  if [[ "$#" -ne 3 ]]; then
    echo "::error::assert_ios_bundle_build_number requires a bundle label, actual value, and expected value" >&2
    return 64
  fi

  local bundle_label="$1"
  local actual_build_number="$2"
  local expected_build_number="$3"

  validate_ios_build_number "$expected_build_number" || return $?
  if [[ "$actual_build_number" != "$expected_build_number" ]]; then
    echo "::error::The $bundle_label build number is $actual_build_number, expected $expected_build_number" >&2
    return 1
  fi
}
