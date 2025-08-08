{
  inputs = {
    nixpkgs.url = "github:nixos/nixpkgs/nixpkgs-unstable";
  };
  outputs =
    { self, nixpkgs }:
    {
      packages.x86_64-linux.classisland =
        let
          pkgs = import nixpkgs { system = "x86_64-linux"; };
          classisland = pkgs.callPackage ./classisland.nix { };
        in
        classisland;
      packages.x86_64-linux.default = self.packages.x86_64-linux.classisland;
      # apps.x86_64-linux.classisland = self.packages.x86_64-linux.classisland
      devShells.x86_64-linux.default =
        let
          pkgs = import nixpkgs { system = "x86_64-linux"; };
        in
        pkgs.mkShell {
          packages = with pkgs; [
            nuget-to-json
            dotnetCorePackages.sdk_8_0-bin
            xorg.libX11
            nix-output-monitor
          ];
        };
    };
}
