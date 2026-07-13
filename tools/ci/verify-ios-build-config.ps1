param(
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
)

$ErrorActionPreference = "Stop"

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Read-RepositoryFile {
    param([string]$RelativePath)

    $path = Join-Path $RepositoryRoot $RelativePath
    Assert-True (Test-Path -LiteralPath $path -PathType Leaf) "Missing repository file: $RelativePath"
    return Get-Content -LiteralPath $path -Raw
}

$iosProjectText = Read-RepositoryFile "ClassIsland.iOS/ClassIsland.iOS.csproj"
$iosProject = [xml]$iosProjectText
$nativeProjectText = Read-RepositoryFile "ClassIsland.iOS.Native/ClassIsland.iOS.Native.xcodeproj/project.pbxproj"
$infoPlistText = Read-RepositoryFile "ClassIsland.iOS/Info.plist"
$infoPlist = [xml]$infoPlistText
$wrapperWorkflowText = Read-RepositoryFile ".github/workflows/build_ios.yml"
$workerWorkflowText = Read-RepositoryFile ".github/workflows/_build_ios_reusable.yml"
$releaseWorkflowText = Read-RepositoryFile ".github/workflows/build_release.yml"
$ipaVerificationText = Read-RepositoryFile "tools/ci/verify-ios-ipa.sh"
$coverageVerificationText = Read-RepositoryFile "tools/ci/verify-cobertura-coverage.ps1"
$nukeBuildText = Read-RepositoryFile "build/Build.App.cs"
$nukeSchema = Read-RepositoryFile ".nuke/build.schema.json" | ConvertFrom-Json
$liveActivityServiceText = Read-RepositoryFile "ClassIsland.iOS/Services/LiveActivities/IosLiveActivityService.cs"

$supportedVersion = $iosProject.SelectSingleNode("/Project/PropertyGroup/SupportedOSPlatformVersion").InnerText
Assert-True ($supportedVersion -eq "15.0") "The iOS app minimum supported version must be 15.0."

$runtimeIdentifier = $iosProject.SelectSingleNode("/Project/PropertyGroup/RuntimeIdentifier").InnerText
Assert-True ($runtimeIdentifier -eq "ios-arm64") "The iOS project must default to the ios-arm64 device RID."
$runtimeValidationTarget = $iosProject.SelectSingleNode('/Project/Target[@Name="ValidateIosDeviceRuntime"]')
Assert-True ($null -ne $runtimeValidationTarget) "The iOS project must explicitly reject Simulator RIDs."
$runtimeValidationConditions = @($runtimeValidationTarget.Error) | ForEach-Object { $_.Condition }
Assert-True ($runtimeValidationConditions -contains "'`$(RuntimeIdentifier)' != 'ios-arm64'") "Single-RID validation must allow only ios-arm64."
Assert-True (($runtimeValidationConditions -join "`n").Contains("Copy('`$(RuntimeIdentifiers)').Contains('iossimulator-')")) "Multi-RID validation must reject iossimulator-* entries."

$releaseSymbolGroup = @($iosProject.Project.PropertyGroup) |
    Where-Object {
        $_.Condition -eq "'`$(Configuration)' == 'Release'" -and
        $_.DebugType -eq "none" -and
        $_.DebugSymbols -eq "false"
    } |
    Select-Object -First 1
Assert-True ($null -ne $releaseSymbolGroup) "The iOS Release project must disable debug symbols after shared props imports."

$derivedData = $iosProject.SelectSingleNode("/Project/PropertyGroup/ClassIslandLiveActivityDerivedData").InnerText
Assert-True ($derivedData -like '*$(MSBuildProjectDirectory)/obj/*') "Xcode DerivedData must be stored under ClassIsland.iOS/obj."
Assert-True (-not $iosProjectText.Contains('$(IntermediateOutputPath)xcode-live-activity-extension')) "DerivedData must not depend on an early IntermediateOutputPath evaluation."

$soundFlowTarget = $iosProject.SelectSingleNode('/Project/Target[@Name="CreateSoundFlowIosResolverAlias"]')
Assert-True ($null -ne $soundFlowTarget) "The production build is missing the SoundFlow resolver target."
Assert-True ($soundFlowTarget.AfterTargets -eq "_CreateAppBundle") "The SoundFlow resolver must run after the app bundle is created."
Assert-True ($soundFlowTarget.BeforeTargets -eq "CoreCodesign") "The SoundFlow resolver must run before app signing."
Assert-True ($soundFlowTarget.Exec.Command.Contains("../../../../Frameworks/miniaudio.framework/miniaudio")) "The SoundFlow resolver target path is incorrect."

$swiftRuntimeTarget = $iosProject.SelectSingleNode('/Project/Target[@Name="EmbedIosSwiftRuntimeLibraries"]')
Assert-True ($null -ne $swiftRuntimeTarget) "Swift runtime embedding must be part of the production app-bundle build."
Assert-True ($swiftRuntimeTarget.AfterTargets -eq "_CreateAppBundle") "Swift runtime embedding must run after the app bundle is created."
Assert-True ($swiftRuntimeTarget.BeforeTargets -eq "CoreCodesign") "Swift runtime embedding must run before app signing."
$swiftRuntimeSignArguments = $swiftRuntimeTarget.SelectSingleNode("PropertyGroup/_IosSwiftRuntimeSignArguments")
Assert-True ($null -ne $swiftRuntimeSignArguments) "Signed builds must configure swift-stdlib-tool signing arguments."
Assert-True ($swiftRuntimeSignArguments.Condition -eq "'`$(EnableCodeSigning)' == 'true'") "Swift runtime signing arguments must be enabled only for signed builds."
Assert-True ($swiftRuntimeSignArguments.InnerText -eq '--sign "$(CodesignKey)"') "Swift runtime libraries must be signed with CodesignKey."
Assert-True ($swiftRuntimeTarget.Exec.Command.Contains("swift-stdlib-tool --copy `$(_IosSwiftRuntimeSignArguments) --platform iphoneos")) "The Swift runtime target must invoke swift-stdlib-tool with conditional signing arguments."
Assert-True (-not $swiftRuntimeTarget.Exec.Command.Contains("--sign")) "Unsigned builds must not pass a literal --sign argument to swift-stdlib-tool."
$swiftRuntimeErrorConditions = @($swiftRuntimeTarget.Error) | ForEach-Object { $_.Condition }
Assert-True ($swiftRuntimeErrorConditions -contains "'`$(EnableCodeSigning)' == 'true' And '`$(CodesignKey)' == ''") "Signed Swift runtime embedding must require CodesignKey."

$derivedDataCleanTarget = $iosProject.SelectSingleNode('/Project/Target[@Name="CleanClassIslandLiveActivityDerivedData"]')
Assert-True ($null -ne $derivedDataCleanTarget) "The iOS Clean target must remove Xcode DerivedData."

$ios15Targets = ([regex]::Matches($nativeProjectText, "IPHONEOS_DEPLOYMENT_TARGET = 15\.0;")).Count
$ios161Targets = ([regex]::Matches($nativeProjectText, "IPHONEOS_DEPLOYMENT_TARGET = 16\.1;")).Count
Assert-True ($ios15Targets -eq 4) "The Xcode project and bridge Debug/Release targets must use iOS 15.0."
Assert-True ($ios161Targets -eq 2) "The Live Activity extension Debug/Release targets must remain on iOS 16.1."
Assert-True (-not $nativeProjectText.Contains("IPHONEOS_DEPLOYMENT_TARGET = 13.0;")) "The Xcode project still contains an iOS 13.0 target unsupported by Xcode 26."
$bridgePlatforms = ([regex]::Matches($nativeProjectText, 'SUPPORTED_PLATFORMS = "iphoneos iphonesimulator";')).Count
$deviceOnlyPlatforms = ([regex]::Matches($nativeProjectText, 'SUPPORTED_PLATFORMS = iphoneos;')).Count
Assert-True ($bridgePlatforms -eq 2) "The Swift bridge must provide device and Simulator slices for the SDK-generated XCFramework."
Assert-True ($deviceOnlyPlatforms -eq 2) "The Live Activity extension must remain device-only."

$plistKeys = @($infoPlist.plist.dict.key)
Assert-True (-not ($plistKeys -contains "CFBundleDisplayName")) "Info.plist must not hard-code CFBundleDisplayName; ApplicationTitle must generate it."
Assert-True ($liveActivityServiceText.Contains('[SupportedOSPlatform("ios15.0")]')) "The managed Live Activity bridge must declare the iOS 15.0 platform floor."

Assert-True ($wrapperWorkflowText.Contains("name: Build iOS")) "The observable iOS workflow must keep the Build iOS name."
Assert-True ($wrapperWorkflowText.Contains("uses: ./.github/workflows/_build_ios_reusable.yml")) "The Build iOS workflow must call the reusable iOS worker."
Assert-True ($wrapperWorkflowText.Contains('checkout_ref: ${{ github.sha }}')) "The Build iOS wrapper must build the triggering commit."
Assert-True ($wrapperWorkflowText.Contains("developer_preview: true")) "The standalone Build iOS workflow must enable DeveloperPreview."
Assert-True ($wrapperWorkflowText.Contains("group: ios-`${{ github.workflow }}-`${{ github.ref }}")) "The Build iOS wrapper must retain its concurrency group."
Assert-True ($wrapperWorkflowText.Contains(".github/workflows/_build_ios_reusable.yml")) "Changes to the reusable iOS worker must trigger Build iOS."
Assert-True ($wrapperWorkflowText.Contains("tools/ci/verify-ios-ipa.sh")) "Changes to IPA verification must trigger Build iOS."
Assert-True ($wrapperWorkflowText.Contains("tools/ci/verify-cobertura-coverage.ps1")) "Changes to coverage verification must trigger Build iOS."
Assert-True (-not $wrapperWorkflowText.Contains("runs-on:")) "The Build iOS wrapper must not duplicate the macOS worker implementation."

Assert-True ($workerWorkflowText.Contains("workflow_call:")) "The shared iOS worker must be a reusable workflow."
foreach ($inputName in @("checkout_ref", "app_version", "build_number", "brand_type", "developer_preview", "retention_days")) {
    Assert-True ($workerWorkflowText.Contains("${inputName}:")) "The reusable iOS worker is missing the $inputName input."
}
Assert-True ($workerWorkflowText.Contains('ref: ${{ inputs.checkout_ref }}')) "The reusable iOS worker must checkout the requested ref."
Assert-True ($workerWorkflowText.Contains("NUGET_AUTH_TOKEN: `${{ github.token }}")) "The reusable iOS worker must use its scoped GITHUB_TOKEN for package restore."
Assert-True ($workerWorkflowText.Contains("packages: read")) "The reusable iOS worker must request read-only package access."
Assert-True ($workerWorkflowText.Contains("IOS_CONFIGURATION: Release")) "The unsigned IPA must use a Release app build."
Assert-True ($workerWorkflowText.Contains("./build.sh PublishApp")) "The iOS worker must build through the shared NUKE PublishApp target."
Assert-True ($workerWorkflowText.Contains("DeveloperPreview: `${{ inputs.developer_preview && 'true' || 'false' }}")) "The NUKE iOS build must receive the reusable DeveloperPreview input."
Assert-True ($workerWorkflowText.Contains("enableCodeSigning: 'false'")) "The reusable iOS worker must produce an unsigned IPA."
Assert-True (-not $workerWorkflowText.Contains("Add SoundFlow iOS resolver alias")) "The SoundFlow resolver fix must not exist only in CI shell code."
Assert-True (-not $workerWorkflowText.Contains("Embed Swift runtime libraries")) "Swift runtime embedding must not exist only in CI shell code."
Assert-True ($workerWorkflowText.Contains("coverlet.runsettings")) "The iOS worker must apply coverlet.runsettings."
Assert-True ($workerWorkflowText.Contains("XPlat Code Coverage")) "The iOS worker must enable the coverlet collector."
Assert-True ($workerWorkflowText.Contains("verify-cobertura-coverage.ps1")) "The iOS worker must enforce the coverage threshold."
Assert-True ($workerWorkflowText.Contains("-MinimumLineRate 0.8")) "The iOS worker must enforce at least 80% line coverage."
Assert-True ($workerWorkflowText.Contains("bash ./tools/ci/verify-ios-ipa.sh")) "The iOS worker must run the shared IPA verification script."

Assert-True ($coverageVerificationText.Contains('GetAttribute("line-rate")')) "Coverage verification must read the Cobertura root line rate."
Assert-True ($coverageVerificationText.Contains('$lineRate -lt $MinimumLineRate')) "Coverage verification must fail below the requested threshold."

Assert-True ($releaseWorkflowText.Contains("build_ios_unsigned:")) "The release workflow must expose the unsigned iOS build job."
Assert-True ($releaseWorkflowText.Contains("if: `${{ github.event_name == 'workflow_dispatch' }}")) "The release workflow must build iOS only for an explicit dispatch."
Assert-True ($releaseWorkflowText.Contains('checkout_ref: ${{ inputs.release_tag }}')) "The release iOS build must checkout the requested release tag."
Assert-True ($releaseWorkflowText.Contains("developer_preview: false")) "The release iOS build must disable DeveloperPreview."

Assert-True ($ipaVerificationText.Contains("<ipa-path> <application-id> <runtime-identifier>")) "IPA verification must document its three required arguments."
Assert-True ($ipaVerificationText.Contains("MonoTouchDebugConfiguration.txt")) "IPA verification must reject MonoTouch debug configuration."
Assert-True ($ipaVerificationText.Contains("libxamarin-dotnet-debug")) "IPA verification must reject the remote-debug runtime."
Assert-True ($ipaVerificationText.Contains('assert_minimum_ios "$app_binary" "The main app" "15.0"')) "IPA verification must assert iOS 15.0 for the main app."
Assert-True ($ipaVerificationText.Contains('assert_minimum_ios "$bridge_binary" "The Live Activity bridge" "15.0"')) "IPA verification must assert iOS 15.0 for the bridge."
Assert-True ($ipaVerificationText.Contains("CFBundleDisplayName")) "IPA verification must validate the final display name."
Assert-True ($ipaVerificationText.Contains("assert_unsigned_bundle")) "IPA verification must reject signed app, extension, and bridge bundles."

Assert-True ($nukeBuildText.Contains('SetProperty("EnableCodeSigning", enableCodeSigning)')) "NUKE iOS publish must explicitly support signed and unsigned modes."
Assert-True ($nukeBuildText.Contains('SetProperty("ArchiveOnBuild", enableCodeSigning)')) "Unsigned NUKE publish must skip xcarchive creation while still building the IPA."
Assert-True ($nukeBuildText.Contains('SetProperty("BuildIpa", true)')) "NUKE iOS publish must keep IPA packaging enabled."
$iosPublishProperties = [regex]::Match($nukeBuildText, '(?s)DotNetPublish\(settings =>.*?SetProject\(IosAppEntryProject\).*?if \(!EnableCodeSigning\)')
Assert-True ($iosPublishProperties.Success) "The NUKE iOS publish block could not be validated."
Assert-True ($iosPublishProperties.Value.Contains('SetProperty("PublishBuilding", true)')) "NUKE iOS publish must exclude non-publish fallback secrets."
Assert-True ($iosPublishProperties.Value.Contains('SetProperty("PublishPlatform", OsName)')) "NUKE iOS publish must not inherit the macOS runner platform."
Assert-True ($iosPublishProperties.Value.Contains('SetProperty("ClassIsland_PlatformTarget", Arch)')) "NUKE iOS publish must define the release architecture."
Assert-True ($nukeBuildText.Contains('SetProperty("DebugType", "none")')) "NUKE iOS Release publish must disable PDB generation for every project reference."
Assert-True ($nukeBuildText.Contains('SetProperty("DebugSymbols", false)')) "NUKE iOS Release publish must disable debug symbols for every project reference."
$enableCodeSigningSchema = @($nukeSchema.allOf) |
    ForEach-Object { $_.properties.EnableCodeSigning } |
    Where-Object { $null -ne $_ } |
    Select-Object -First 1
Assert-True ($enableCodeSigningSchema.type -eq "boolean") "The NUKE schema must expose EnableCodeSigning as a boolean parameter."

Write-Output "iOS build configuration verification passed."
