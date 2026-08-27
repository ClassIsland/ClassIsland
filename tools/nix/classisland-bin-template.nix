{
  pname,
  version,
  x86_64-linux-hash,
  aarch64-linux-hash,
}:{
  lib,
  stdenv,
  fetchurl,
  autoPatchelfHook,
  makeShellWrapper,
  dpkg,
  fontconfig,
  hicolor-icon-theme,
  lttng-ust_2_12,
  libx11,
  libice,
  libsm,
  libxfixes,
  icu,
  openssl,
  alsa-lib,
}:
stdenv.mkDerivation rec {
  inherit pname version;
  src =
    {
      x86_64-linux = fetchurl {
        url = "https://github.com/ClassIsland/ClassIsland/releases/download/${version}/ClassIsland_app_linux_x64_selfContained_deb.deb";
        hash = x86_64-linux-hash;
      };
      aarch64-linux = fetchurl {
        url = "https://github.com/ClassIsland/ClassIsland/releases/download/${version}/ClassIsland_app_linux_arm64_selfContained_deb.deb";
        hash = aarch64-linux-hash;
      };
    }
    .${stdenv.hostPlatform.system} or (throw "Unsupported system: ${stdenv.hostPlatform.system}");
  nativeBuildInputs = [
    autoPatchelfHook
    makeShellWrapper
    dpkg
  ];
  buildInputs = [
    fontconfig
    hicolor-icon-theme
    stdenv.cc.cc.lib
    lttng-ust_2_12
  ];
  installPhase = ''
    runHook preInstall
    mkdir -p $out/bin
    cp -r opt/apps $out/opt
    cp -r usr/share $out/share
    printf "deb" > "$out/opt/cn.classisland.app/PackageType"
    substituteInPlace $out/share/applications/cn.classisland.app.desktop \
      --replace-fail "/opt/apps/cn.classisland.app/files/bin/ClassIsland.Desktop" $pname
    makeShellWrapper $out/opt/cn.classisland.app/files/bin/ClassIsland.Desktop $out/bin/$pname \
      --set ClassIsland_PackageRoot "$out/opt/cn.classisland.app" \
      --prefix LD_LIBRARY_PATH : "${
        lib.makeLibraryPath [
          icu
          libx11
          libice
          libsm
          libxfixes
          openssl
          alsa-lib
        ]
      }"
    runHook postInstall
  '';
}
