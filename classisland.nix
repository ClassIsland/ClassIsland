{
  buildDotnetModule,
  dotnetCorePackages,
  git,
  xorg,
  lib,
  fontconfig,
}:

buildDotnetModule {
  pname = "classisland";
  version = "1.7.105.0";
  src = ./.;
  projectFile = "ClassIsland.Desktop/ClassIsland.Desktop.csproj";
  nugetDeps = ./deps.json;
  dotnet-sdk = dotnetCorePackages.sdk_8_0-bin;
  dotnet-runtime = dotnetCorePackages.runtime_8_0-bin;
  dotnetBuildFlags = [
    "-p:NIX=true"
  ];
  executables = [ "ClassIsland.Desktop" ];
  nativeBuildInputs = [ git ];
  makeWrapperArgs = [
    "--prefix LD_LIBRARY_PATH : ${
      lib.makeLibraryPath [
        xorg.libX11
        xorg.libICE
        xorg.libSM
        xorg.libXfixes
        fontconfig
      ]
    }"
  ];
  postInstall = ''
    echo deb > $out/lib/classisland/PackageType
  '';
  packNupkg = false;
}
