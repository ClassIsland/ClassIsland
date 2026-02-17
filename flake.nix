{
  inputs.nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
  inputs.flake-utils.url = "github:numtide/flake-utils";

  outputs =
    {
      nixpkgs,
      flake-utils,
      ...
    }:
    # TODO: darwin support
    flake-utils.lib.eachSystem [ "x86_64-linux" "aarch64-linux" ] (
      system:
      let
        pkgs = import nixpkgs { inherit system; };
      in
      {
        packages.classisland = pkgs.callPackage ./tools/nix/classisland.nix { };
        packages.classisland-bin = pkgs.callPackage ./tools/nix/classisland-bin.nix { };
        packages.default = pkgs.callPackage ./tools/nix/classisland-bin.nix { };
      }
    );
}
