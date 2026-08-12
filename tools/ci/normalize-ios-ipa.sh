#!/usr/bin/env bash

set -euo pipefail

if [[ $# -ne 1 ]]; then
  echo "Usage: $0 <ipa-path>" >&2
  exit 2
fi

ipa_path="$1"
if [[ ! -f "$ipa_path" ]]; then
  echo "::error::The IPA to normalize does not exist: $ipa_path"
  exit 1
fi

normalization_root="$(mktemp -d "${RUNNER_TEMP:-${TMPDIR:-/tmp}}/classisland-normalize-ipa.XXXXXX")"
repacked_ipa="$normalization_root/normalized.ipa"
trap 'rm -rf "$normalization_root"' EXIT

/usr/bin/ditto -x -k "$ipa_path" "$normalization_root/extracted"

shopt -s nullglob
app_bundles=("$normalization_root"/extracted/Payload/*.app)
if [[ ${#app_bundles[@]} -ne 1 ]]; then
  echo "::error::The IPA must contain exactly one app bundle"
  exit 1
fi
app_bundle="${app_bundles[0]}"

while IFS= read -r -d '' bundle; do
  if /usr/bin/codesign --display "$bundle" >/dev/null 2>&1; then
    /usr/bin/codesign --remove-signature "$bundle"
  fi
done < <(
  /usr/bin/find "$app_bundle" -depth -type d \
    \( -name '*.app' -o -name '*.appex' -o -name '*.framework' \
       -o -name '*.xpc' -o -name '*.bundle' \) -print0
)

while IFS= read -r -d '' binary; do
  if /usr/bin/file -b "$binary" | /usr/bin/grep -q '^Mach-O' &&
     /usr/bin/codesign --display "$binary" >/dev/null 2>&1; then
    /usr/bin/codesign --remove-signature "$binary"
  fi
done < <(/usr/bin/find "$app_bundle" -type f -print0)

while IFS= read -r -d '' signature_directory; do
  rm -rf "$signature_directory"
done < <(/usr/bin/find "$app_bundle" -depth -type d -name '_CodeSignature' -print0)

while IFS= read -r -d '' provisioning_profile; do
  rm -f "$provisioning_profile"
done < <(/usr/bin/find "$app_bundle" -type f -name 'embedded.mobileprovision' -print0)

/usr/bin/xattr -cr "$app_bundle"
(
  cd "$normalization_root/extracted"
  /usr/bin/ditto -c -k --keepParent Payload "$repacked_ipa"
)

mv -f "$repacked_ipa" "$ipa_path"
