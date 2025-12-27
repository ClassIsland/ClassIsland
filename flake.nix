{
  inputs.nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
  inputs.flake-utils.url = "github:numtide/flake-utils";

  outputs =
    {
      self,
      nixpkgs,
      flake-utils,
    }:
    # TODO: darwin support
    flake-utils.lib.eachSystem [ "x86_64-linux" "aarch64-linux" ] (
      system:
      let
        pkgs = import nixpkgs { inherit system; };
        projectFile = "./ClassIsland.Desktop/ClassIsland.Desktop.csproj";
        dotnet-sdk =
          with pkgs.dotnetCorePackages;
          pkgs.dotnetCorePackages.combinePackages [
            sdk_9_0
            sdk_8_0
            sdk_6_0
          ];
        dotnet-runtime = pkgs.dotnetCorePackages.runtime_8_0;
        version = "2.0.0.0";
        classisland = pkgs.buildDotnetModule {
          inherit
            projectFile
            dotnet-sdk
            dotnet-runtime
            version
            ;
          pname = "classisland";
          src = ./.;
          # nix build .#default.passthru.fetch-deps && ./result deps.json
          # 生成完后可能需要手动修改
          nugetDeps = ./deps.json;
          doCheck = true;
          dotnetBuildFlags = [
            "--property:NIX=true"
          ];
          runtimeDeps = with pkgs; [
            xorg.libX11
            xorg.libICE
            xorg.libSM
            xorg.libXfixes
            fontconfig
          ];
          executables = [ "ClassIsland.Desktop" ];
          nativeBuildInputs = with pkgs; [
            git
            stdenv.cc.cc.lib
            autoPatchelfHook
          ];
          postInstall = ''
            echo deb > $out/lib/classisland/PackageType
          '';
          packNupkg = false;
        };
      in
      {
        packages.default = classisland;

        apps.default = {
          type = "app";
          program = "${classisland}/bin/ClassIsland.Desktop";
        };

        devShells.default = pkgs.mkShell {
          packages = with pkgs; [
            nuget-to-json
            dotnet-sdk
            xorg.libX11
            xorg.libICE
            xorg.libSM
            xorg.libXfixes
            fontconfig
          ];
        };
      }
    );
}
