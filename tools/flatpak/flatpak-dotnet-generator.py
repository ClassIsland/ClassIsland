#!/usr/bin/env python3

__license__ = "MIT"

import argparse
import base64
import binascii
import concurrent.futures
import json
import subprocess
import tempfile
from pathlib import Path


def main() -> None:
    # Bump this to latest freedesktop runtime version.
    freedesktop_default = "24.08"
    # Bump this to an LTS dotnet version.
    dotnet_default = "8"

    parser = argparse.ArgumentParser()
    parser.add_argument("output", help="The output JSON sources file")
    parser.add_argument("project", nargs="+", help="The project file(s)")
    parser.add_argument(
        "--runtime",
        "-r",
        nargs="+",
        default=[None],
        help="The target runtime(s) to restore packages for",
    )
    parser.add_argument(
        "--freedesktop",
        "-f",
        help="The target version of the freedesktop sdk to use",
        default=freedesktop_default,
    )
    parser.add_argument(
        "--dotnet",
        "-d",
        help="The target version of dotnet to use",
        default=dotnet_default,
    )
    parser.add_argument(
        "--destdir",
        help="The directory the generated sources file will save sources to",
        default="nuget-sources",
    )
    parser.add_argument(
        "--only-arches", help="Limit the source to this Flatpak arch", default=None
    )
    parser.add_argument(
        "--dotnet-args",
        "-a",
        nargs=argparse.REMAINDER,
        help="Additional arguments to pass to the dotnet command",
    )
    args = parser.parse_args()

    sources = []
    with tempfile.TemporaryDirectory(dir=Path()) as tmp:

        def restore_project(project: str, runtime: str | None) -> None:
            subprocess.run(
                [
                    "flatpak",
                    "run",
                    "--env=DOTNET_CLI_TELEMETRY_OPTOUT=true",
                    "--env=DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true",
                    "--command=sh",
                    f"--runtime=org.freedesktop.Sdk//{args.freedesktop}",
                    "--share=network",
                    "--filesystem=host",
                    f"org.freedesktop.Sdk.Extension.dotnet{args.dotnet}//{args.freedesktop}",
                    "-c",
                    f'PATH="${{PATH}}:/usr/lib/sdk/dotnet{args.dotnet}/bin" LD_LIBRARY_PATH="$LD_LIBRARY_PATH:/usr/lib/sdk/dotnet{args.dotnet}/lib" exec dotnet restore "$@"',
                    "--",
                    "--packages",
                    tmp,
                    project,
                ]
                + (["-r", runtime] if runtime else [])
                + (args.dotnet_args or []),
                check=False,
            )

        with concurrent.futures.ThreadPoolExecutor() as executor:
            futures = []
            for project in args.project:
                if args.runtime:
                    for runtime in args.runtime:
                        futures.append(
                            executor.submit(restore_project, project, runtime)
                        )
                else:
                    futures.append(executor.submit(restore_project, project, None))
            concurrent.futures.wait(futures)

        for path in Path(tmp).glob("**/*.nupkg.sha512"):
            name = path.parent.parent.name
            version = path.parent.name
            filename = "{}.{}.nupkg".format(name, version)
            url = "https://api.nuget.org/v3-flatcontainer/{}/{}/{}".format(
                name, version, filename
            )

            with path.open() as fp:
                sha512 = binascii.hexlify(base64.b64decode(fp.read())).decode("ascii")

            data = {
                "type": "file",
                "url": url,
                "sha512": sha512,
                "dest": args.destdir,
                "dest-filename": filename,
            }

            if args.only_arches is not None:
                data["only-arches"] = [args.only_arches]

            sources.append(data)

    with open(args.output, "w", encoding="utf-8") as fp:
        json.dump(sorted(sources, key=lambda n: n.get("dest-filename")), fp, indent=4)


if __name__ == "__main__":
    main()
