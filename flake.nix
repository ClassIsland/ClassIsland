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
        classisland = pkgs.callPackage ./classisland.nix { };
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
            dotnetCorePackages.sdk_8_0-bin
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
