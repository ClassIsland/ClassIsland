{
  buildDotnetModule,
  dotnetCorePackages,
  git,
  xorg,
  lib,
}:

buildDotnetModule {
  pname = "classisland";
  version = "1.7.104.0";
  src = ./.;
  projectFile = "ClassIsland.Desktop/ClassIsland.Desktop.csproj";
  nugetDeps = ./deps.json;
  dotnet-sdk = dotnetCorePackages.sdk_8_0-bin;
  dotnet-runtime = dotnetCorePackages.runtime_8_0-bin;
  executables = [ "ClassIsland.Desktop" ];
  nativeBuildInputs = [ git ];
  makeWrapperArgs = [
    "--prefix LD_LIBRARY_PATH : ${
      lib.makeLibraryPath [
        xorg.libX11
        xorg.libICE
        xorg.libSM
        xorg.libXfixes
      ]
    }"
  ];
  postInstall = ''
    echo deb > $out/lib/classisland/PackageType
  '';
  packNupkg = false;
}
