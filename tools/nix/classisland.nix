{
  dotnetCorePackages,
  buildDotnetModule,
  libx11,
  libice,
  libsm,
  libxfixes,
  fontconfig,
  git,
  stdenv,
  autoPatchelfHook,
  makeDesktopItem,
}:
let
  desktopItem = makeDesktopItem {
    type = "Application";
    name = "cn.classisland.app";
    desktopName = "ClassIsland";
    icon = "cn.classisland.app";
    exec = "classisland";
    terminal = false;
    startupNotify = true;
    comment = "功能强大、可定制、跨平台的大屏课表显示工具。";
    categories = [
      "Education"
      "Office"
    ];
  };

  desktopUrlItem = makeDesktopItem {
    type = "Application";
    name = "cn.classisland.app-url-handler";
    desktopName = "ClassIsland - URL Handler";
    icon = "cn.classisland.app";
    exec = "classisland --uri %U";
    terminal = false;
    startupNotify = true;
    comment = "功能强大、可定制、跨平台的大屏课表显示工具。";
    noDisplay = true;
    mimeTypes = [ "x-scheme-handler/classisland" ];
    categories = [
      "Education"
      "Office"
    ];
  };
in
buildDotnetModule {
  pname = "classisland";
  version = "2.0.2.0";
  projectFile = "./ClassIsland.Desktop/ClassIsland.Desktop.csproj";
  dotnet-sdk =
    with dotnetCorePackages;
    (combinePackages [
      sdk_9_0
      sdk_8_0
      sdk_6_0
    ]);
  dotnet-runtime = dotnetCorePackages.runtime_8_0;
  src = ../../.;
  # nix build .#classisland.passthru.fetch-deps && ./result ./tools/nix/deps.json
  # 生成完后可能需要手动修改
  nugetDeps = ./deps.json;
  doCheck = true;
  dotnetBuildFlags = [
    "--property:NIX=true"
  ];
  runtimeDeps = [
    libx11
    libice
    libsm
    libxfixes
    fontconfig
  ];
  executables = [ "ClassIsland.Desktop" ];
  nativeBuildInputs = [
    git
    stdenv.cc.cc.lib
    autoPatchelfHook
  ];
  postInstall = ''
    mkdir -p $out/share/applications
    cp ${desktopItem}/share/applications/cn.classisland.app.desktop $out/share/applications/cn.classisland.app.desktop 
    cp ${desktopUrlItem}/share/applications/cn.classisland.app-url-handler.desktop $out/share/applications/cn.classisland.app-url-handler.desktop 
    mkdir -p $out/share/icons/hicolor/scalable/apps/
    cp ClassIsland.Desktop/Assets/AppLogo.svg $out/share/icons/hicolor/scalable/apps/cn.classisland.app.svg
    printf deb > $out/lib/classisland/PackageType
  '';
  postFixup = ''
    mv $out/bin/ClassIsland.Desktop $out/bin/classisland
  '';
  packNupkg = false;
}
