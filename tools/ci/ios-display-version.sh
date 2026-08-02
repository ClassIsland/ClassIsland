#!/usr/bin/env bash

resolve_ios_display_version() {
  if [[ "$#" -ne 1 ]]; then
    echo "::error::resolve_ios_display_version requires exactly one value" >&2
    return 64
  fi

  local version_source="$1"
  case "$version_source" in
    ios-v*) version_source="${version_source#ios-v}" ;;
    v*) version_source="${version_source#v}" ;;
  esac

  if [[ ! "$version_source" =~ ^([0-9]+\.[0-9]+\.[0-9]+)(\.[0-9]+)?$ ]]; then
    echo "::error::The iOS version source must contain three or four numeric components" >&2
    return 64
  fi

  printf '%s\n' "${BASH_REMATCH[1]}"
}

validate_ios_display_version() {
  if [[ "$#" -ne 1 ]]; then
    echo "::error::validate_ios_display_version requires exactly one value" >&2
    return 64
  fi

  if [[ ! "$1" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
    echo "::error::The expected iOS display version must contain exactly three numeric components" >&2
    return 64
  fi
}

assert_ios_bundle_display_version() {
  if [[ "$#" -ne 3 ]]; then
    echo "::error::assert_ios_bundle_display_version requires a bundle label, actual value, and expected value" >&2
    return 64
  fi

  local bundle_label="$1"
  local actual_display_version="$2"
  local expected_display_version="$3"

  validate_ios_display_version "$expected_display_version" || return $?
  if [[ "$actual_display_version" != "$expected_display_version" ]]; then
    echo "::error::The $bundle_label display version is $actual_display_version, expected $expected_display_version" >&2
    return 1
  fi
}
