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
$iosAssemblyInfoText = Read-RepositoryFile "ClassIsland.iOS/AssemblyInfo.cs"
$privacyManifestText = Read-RepositoryFile "ClassIsland.iOS/PrivacyInfo.xcprivacy"
$privacyManifest = [xml]$privacyManifestText
$nativeProjectText = Read-RepositoryFile "ClassIsland.iOS.Native/ClassIsland.iOS.Native.xcodeproj/project.pbxproj"
$infoPlistText = Read-RepositoryFile "ClassIsland.iOS/Info.plist"
$infoPlist = [xml]$infoPlistText
$wrapperWorkflowText = Read-RepositoryFile ".github/workflows/build_ios.yml"
$workerWorkflowText = Read-RepositoryFile ".github/workflows/_build_ios_reusable.yml"
$releaseWorkflowText = Read-RepositoryFile ".github/workflows/build_release.yml"
$ipaNormalizationText = Read-RepositoryFile "tools/ci/normalize-ios-ipa.sh"
$ipaVerificationText = Read-RepositoryFile "tools/ci/verify-ios-ipa.sh"
$coverageVerificationText = Read-RepositoryFile "tools/ci/verify-cobertura-coverage.ps1"
$coverageRunsettingsText = Read-RepositoryFile "ClassIsland.Platforms.Abstractions.Tests/coverlet.runsettings"
$artifactInitializationText = Read-RepositoryFile "tools/release-gen/init-artifacts.ps1"
$nukeBuildText = Read-RepositoryFile "build/Build.DesktopApp.cs"
$nukeSchema = Read-RepositoryFile ".nuke/build.schema.json" | ConvertFrom-Json
$liveActivityServiceText = Read-RepositoryFile "ClassIsland.iOS/Services/LiveActivities/IosLiveActivityService.cs"
$liveActivityBridgeText = Read-RepositoryFile "ClassIsland.iOS.Native/Bridge/ClassIslandLiveActivityBridge.swift"
$appDelegateText = Read-RepositoryFile "ClassIsland.iOS/AppDelegate.cs"
$iosSystemEventsServiceText = Read-RepositoryFile "ClassIsland.iOS/Services/Platform/IosSystemEventsService.cs"
$iosSoundFlowBootstrapText = Read-RepositoryFile "ClassIsland.iOS/Services/Platform/IosSoundFlowNativeBootstrap.cs"
$audioServiceText = Read-RepositoryFile "ClassIsland/Services/AudioService.cs"
$iosNotificationCoordinatorText = Read-RepositoryFile "ClassIsland.iOS/Services/Notifications/IosLessonsNotificationCoordinator.cs"
$iosNotificationScheduleFactoryText = Read-RepositoryFile "ClassIsland.iOS/Services/Notifications/IosLessonNotificationScheduleFactory.cs"
$iosNotificationSchedulerText = Read-RepositoryFile "ClassIsland.iOS/Services/Notifications/IosLessonNotificationScheduler.cs"
$iosNotificationQueueConsumerText = Read-RepositoryFile "ClassIsland.iOS/Services/Notifications/IosNotificationQueueConsumer.cs"
$iosNotificationScheduleSelectorText = Read-RepositoryFile "ClassIsland.Platforms.Abstractions/Services/IosLessonNotificationScheduleSelector.cs"
$iosNotificationCapacityPolicyText = Read-RepositoryFile "ClassIsland.Platforms.Abstractions/Services/IosNotificationCapacityPolicy.cs"
$iosNotificationMutationGateText = Read-RepositoryFile "ClassIsland.Platforms.Abstractions/Services/IosNotificationMutationGate.cs"
$iosNotificationSynchronizationPolicyText = Read-RepositoryFile "ClassIsland.Platforms.Abstractions/Services/IosNotificationSynchronizationPolicy.cs"
$iosNotificationSynchronizationExecutionPolicyText = Read-RepositoryFile "ClassIsland.Platforms.Abstractions/Services/IosNotificationSynchronizationExecutionPolicy.cs"
$liveActivityCoordinatorText = Read-RepositoryFile "ClassIsland.iOS/Services/LiveActivities/LessonsLiveActivityCoordinator.cs"
$notificationTimelineText = Read-RepositoryFile "ClassIsland.Platforms.Abstractions/Services/LessonPreparationNotificationTimeline.cs"
$storageItemMaterializerText = Read-RepositoryFile "ClassIsland.Platforms.Abstractions/Services/StorageItemMaterializer.cs"
$importedFileReferenceText = Read-RepositoryFile "ClassIsland.Core/Helpers/ImportedFileReference.cs"
$portableImportedFileReferenceText = Read-RepositoryFile "ClassIsland.Platforms.Abstractions/Services/PortableImportedFileReference.cs"
$safeArchivePathText = Read-RepositoryFile "ClassIsland.Platforms.Abstractions/Services/SafeArchivePath.cs"
$trustedFileSystemPathPolicyText = Read-RepositoryFile "ClassIsland.Platforms.Abstractions/Services/TrustedFileSystemPathPolicy.cs"
$persistentImportedFileServiceText = Read-RepositoryFile "ClassIsland.Core/Helpers/PersistentImportedFileService.cs"
$iosPlatformFilePickerServiceText = Read-RepositoryFile "ClassIsland.iOS/Services/Platform/IosPlatformFilePickerService.cs"
$fileSystemDataTransactionText = Read-RepositoryFile "ClassIsland/Services/FileSystemDataTransaction.cs"
$safeArchiveExtractorText = Read-RepositoryFile "ClassIsland/Services/SafeArchiveExtractor.cs"
$dataTransferPageText = Read-RepositoryFile "ClassIsland/Views/DataTransferPage.axaml.cs"
$storageSettingsPageText = Read-RepositoryFile "ClassIsland/Views/SettingPages/StorageSettingsPage.axaml.cs"
$fileBrowserButtonText = Read-RepositoryFile "ClassIsland/Controls/FileBrowserButton.cs"
$safeChildDirectoryPathText = Read-RepositoryFile "ClassIsland.Platforms.Abstractions/Services/SafeChildDirectoryPath.cs"
$appText = Read-RepositoryFile "ClassIsland/App.axaml.cs"
$appServicesText = Read-RepositoryFile "ClassIsland/App.Services.xaml.cs"
$pluginLoadContextText = Read-RepositoryFile "ClassIsland/PluginLoadContext.cs"
$pluginServiceText = Read-RepositoryFile "ClassIsland/Services/PluginService.cs"
$pluginMarketServiceText = Read-RepositoryFile "ClassIsland/Services/PluginMarketService.cs"
$xamlThemeServiceText = Read-RepositoryFile "ClassIsland/Services/XamlThemeService.cs"
$managementConnectionText = Read-RepositoryFile "ClassIsland/Services/Management/ManagementServerConnection.cs"
$recoverBackupPageText = Read-RepositoryFile "ClassIsland/Views/RecoveryPages/RecoverBackupPage.axaml.cs"
$pluginsSettingsPageText = Read-RepositoryFile "ClassIsland/Views/SettingPages/PluginsSettingsPage.axaml.cs"
$mobileViewHostText = Read-RepositoryFile "ClassIsland/Controls/UI/MobileViewHost.axaml.cs"
$settingsWindowText = Read-RepositoryFile "ClassIsland/Views/SettingsWindowNew.axaml.cs"
$welcomeWindowText = Read-RepositoryFile "ClassIsland/Views/WelcomeWindow.axaml.cs"
$finishWelcomePageText = Read-RepositoryFile "ClassIsland/Views/WelcomePages/FinishPage.axaml.cs"

$supportedVersion = $iosProject.SelectSingleNode("/Project/PropertyGroup/SupportedOSPlatformVersion").InnerText
Assert-True ($supportedVersion -eq "15.0") "The iOS app minimum supported version must be 15.0."
Assert-True ($iosAssemblyInfoText.Contains('[assembly: SupportedOSPlatform("ios15.0")]')) "The iOS entry assembly must declare its iOS 15.0 platform floor."

$privacyBundleResource = $iosProject.SelectSingleNode('/Project/ItemGroup/BundleResource[@Include="PrivacyInfo.xcprivacy"]')
Assert-True ($null -ne $privacyBundleResource) "The iOS project must bundle PrivacyInfo.xcprivacy."
Assert-True ($privacyBundleResource.LogicalName -eq "PrivacyInfo.xcprivacy") "The privacy manifest must retain its bundle-root logical name."
$privacyApiTypes = $privacyManifest.SelectSingleNode('/plist/dict/key[.="NSPrivacyAccessedAPITypes"]/following-sibling::array[1]')
Assert-True ($null -ne $privacyApiTypes) "PrivacyInfo.xcprivacy must declare required-reason API usage."
$privacyTracking = $privacyManifest.SelectSingleNode('/plist/dict/key[.="NSPrivacyTracking"]/following-sibling::*[1]')
Assert-True ($privacyTracking.Name -eq "false") "PrivacyInfo.xcprivacy must declare that ClassIsland does not track users."
$privacyCollectedData = $privacyManifest.SelectSingleNode('/plist/dict/key[.="NSPrivacyCollectedDataTypes"]/following-sibling::array[1]')
Assert-True ($null -ne $privacyCollectedData) "PrivacyInfo.xcprivacy must declare optional Sentry diagnostics."
$privacyCollectedTypes = @($privacyCollectedData.dict | ForEach-Object {
    $_.SelectSingleNode('key[.="NSPrivacyCollectedDataType"]/following-sibling::string[1]').InnerText
})
foreach ($collectedType in @(
    "NSPrivacyCollectedDataTypeCrashData",
    "NSPrivacyCollectedDataTypePerformanceData",
    "NSPrivacyCollectedDataTypeOtherDiagnosticData",
    "NSPrivacyCollectedDataTypeProductInteraction")) {
    Assert-True ($privacyCollectedTypes -contains $collectedType) "PrivacyInfo.xcprivacy is missing $collectedType."
}
$privacyReasons = @{}
foreach ($declaration in @($privacyApiTypes.dict)) {
    $apiType = $declaration.SelectSingleNode('key[.="NSPrivacyAccessedAPIType"]/following-sibling::string[1]')
    $reasonNodes = $declaration.SelectNodes('key[.="NSPrivacyAccessedAPITypeReasons"]/following-sibling::array[1]/string')
    if ($null -ne $apiType) {
        $privacyReasons[$apiType.InnerText] = @($reasonNodes | ForEach-Object { $_.InnerText })
    }
}
Assert-True ($privacyReasons.ContainsKey("NSPrivacyAccessedAPICategoryUserDefaults")) "The privacy manifest must declare UserDefaults access."
Assert-True ($privacyReasons["NSPrivacyAccessedAPICategoryUserDefaults"] -contains "CA92.1") "The privacy manifest must declare UserDefaults reason CA92.1."
Assert-True ($privacyReasons.ContainsKey("NSPrivacyAccessedAPICategoryFileTimestamp")) "The privacy manifest must declare file timestamp access."
Assert-True ($privacyReasons["NSPrivacyAccessedAPICategoryFileTimestamp"] -contains "C617.1") "The privacy manifest must declare file timestamp reason C617.1."
Assert-True ($privacyReasons.ContainsKey("NSPrivacyAccessedAPICategorySystemBootTime")) "The privacy manifest must declare system boot time access used by Stopwatch."
Assert-True ($privacyReasons["NSPrivacyAccessedAPICategorySystemBootTime"] -contains "35F9.1") "The privacy manifest must declare elapsed-time reason 35F9.1."
$localNetworkUsageDescription = $infoPlist.SelectSingleNode('/plist/dict/key[.="NSLocalNetworkUsageDescription"]/following-sibling::string[1]')
Assert-True (-not [string]::IsNullOrWhiteSpace($localNetworkUsageDescription.InnerText)) "The iOS app must explain local-network access used by management servers."

$eventLogGate = [regex]::Match($appServicesText, '(?s)if\s*\(\s*(?:System\.)?OperatingSystem\.IsWindows\(\)\s*\)\s*\{(?<body>.*?)\}')
Assert-True ($eventLogGate.Success) "EventLogLoggerProvider must be guarded by OperatingSystem.IsWindows()."
Assert-True ($eventLogGate.Groups["body"].Value.Contains("builder.AddFilter<EventLogLoggerProvider>")) "The Windows logging gate must contain the EventLog filter."
Assert-True (-not $pluginLoadContextText.Contains('[SupportedOSPlatform("macos")]')) "The path-only Mac plugin resolver must not carry an inaccurate macOS platform annotation."

$runtimeIdentifier = $iosProject.SelectSingleNode("/Project/PropertyGroup/RuntimeIdentifier").InnerText
Assert-True ($runtimeIdentifier -eq "ios-arm64") "The iOS project must default to the ios-arm64 device RID."
$runtimeValidationTarget = $iosProject.SelectSingleNode('/Project/Target[@Name="ValidateIosDeviceRuntime"]')
Assert-True ($null -ne $runtimeValidationTarget) "The iOS project must explicitly reject Simulator RIDs."
$runtimeValidationConditions = @($runtimeValidationTarget.Error) | ForEach-Object { $_.Condition }
Assert-True ($runtimeValidationConditions -contains "'`$(RuntimeIdentifier)' != 'ios-arm64'") "Single-RID validation must allow only ios-arm64."
Assert-True (($runtimeValidationConditions -join "`n").Contains("Copy('`$(RuntimeIdentifiers)').Contains('iossimulator-')")) "Multi-RID validation must reject iossimulator-* entries."
$abstractionsReference = $iosProject.SelectSingleNode('/Project/ItemGroup/ProjectReference[contains(@Include, "ClassIsland.Platforms.Abstractions")]')
Assert-True ($abstractionsReference.AdditionalProperties -eq "ClassIslandReferencedByMobile=true") "The direct abstractions reference must share the mobile project-graph properties."

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
Assert-True (-not [regex]::IsMatch($iosProjectText, '\$\(AppBundleDir\)(?!/)')) "AppBundleDir paths must include an explicit directory separator."

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
Assert-True ($liveActivityBridgeText.Contains('private let continuation: AsyncStream<Command>.Continuation')) "The Swift bridge must enqueue ActivityKit operations in a FIFO async stream."
Assert-True ($liveActivityBridgeText.Contains('bufferingPolicy: .bufferingNewest(Self.maximumBufferedCommands)')) "The Swift ActivityKit command stream must have a bounded newest-value buffer."
Assert-True ($liveActivityBridgeText.Contains('case let .dropped(droppedCommand):')) "A coalesced Swift command must explicitly complete its callback."
Assert-True ($liveActivityBridgeText.Contains('operation.cancel()')) "The Swift bridge must synchronously relinquish callback ownership when managed code cancels."
Assert-True ($liveActivityBridgeText.Contains('guard command.operation.shouldExecute else')) "Cancelled buffered Live Activity commands must not reach ActivityKit."
Assert-True ($liveActivityBridgeText.Contains('@_cdecl("ci_live_activity_cancel")')) "The Swift bridge must export its callback-ownership cancellation ABI."
Assert-True ($liveActivityServiceText.Contains('NativeOperationTimeout = TimeSpan.FromSeconds(15)')) "Managed Live Activity calls must have a bounded native timeout."
Assert-True ($liveActivityServiceText.Contains('EntryPoint = "ci_live_activity_cancel"')) "Managed cancellation must call the native callback-ownership protocol."
Assert-True ($liveActivityServiceText.Contains('CompleteAfterNativeCancellationAcknowledged()')) "Managed code must release its callback context only after native cancellation acknowledgement."
Assert-True ($liveActivityServiceText.Contains('registration.Unregister();')) "Managed callback cleanup must not synchronously wait on its own cancellation registration."
Assert-True ($liveActivityBridgeText.Contains('for await command in stream')) "The Swift bridge must process ActivityKit operations through one sequential consumer."
Assert-True ($liveActivityBridgeText.Contains('ClassIslandLiveActivityCommandQueue.shared.publish(')) "The Swift publish ABI must use the sequential command queue."
Assert-True ($liveActivityBridgeText.Contains('ClassIslandLiveActivityCommandQueue.shared.end(')) "The Swift end ABI must use the sequential command queue."
Assert-True (-not [regex]::IsMatch($appDelegateText, 'override\s+bool\s+OpenUrl')) "AvaloniaAppDelegate.OpenUrl is not virtual and must not be overridden."
Assert-True ($appDelegateText.Contains('app.TryGetFeature<IActivatableLifetime>()')) "The AppDelegate must subscribe to Avalonia protocol activation."
Assert-True ($appDelegateText.Contains('_activatableLifetime.Activated += OnActivated')) "The AppDelegate must handle protocol activation events."
Assert-True ($appDelegateText.Contains('_activatableLifetime.Activated -= OnActivated')) "The AppDelegate must unsubscribe from protocol activation events."
Assert-True ($appDelegateText.Contains('args is not ProtocolActivatedEventArgs')) "The AppDelegate must filter protocol activation events before URI navigation."
Assert-True ($appDelegateText.Contains('PlatformServices.SystemEventsService = _systemEventsService')) "The AppDelegate must install the iOS system-time event service."
Assert-True ($appDelegateText.Contains('IosSoundFlowNativeBootstrap.TryInitialize(')) "The AppDelegate must bootstrap SoundFlow before app services initialize."
$soundFlowBootstrapIndex = $appDelegateText.IndexOf('IosSoundFlowNativeBootstrap.TryInitialize(', [StringComparison]::Ordinal)
$appEntryIndex = $appDelegateText.IndexOf('Program.AppEntry(', [StringComparison]::Ordinal)
Assert-True ($soundFlowBootstrapIndex -ge 0 -and $soundFlowBootstrapIndex -lt $appEntryIndex) "SoundFlow must be bootstrapped before Program.AppEntry can resolve application services."
Assert-True (-not $iosSoundFlowBootstrapText.Contains('NativeLibrary.SetDllImportResolver(')) "The iOS bootstrap must not replace SoundFlow's assembly resolver."
Assert-True ($iosSoundFlowBootstrapText.Contains('RuntimeHelpers.RunClassConstructor(')) "The iOS bootstrap must explicitly run SoundFlow's Native type initializer for Mono AOT."
Assert-True ($iosSoundFlowBootstrapText.Contains('SoundFlow.Backends.MiniAudio.Native')) "The iOS bootstrap must preserve and initialize SoundFlow's internal Native type."
Assert-True ($iosSoundFlowBootstrapText.Contains('[DynamicDependency(')) "The iOS bootstrap must preserve SoundFlow's internal Native type during trimming."
Assert-True ($iosSoundFlowBootstrapText.Contains('NativeLibrary.Load(binaryPath)')) "The iOS bootstrap must preload miniaudio from its canonical framework path."
Assert-True ($iosSoundFlowBootstrapText.Contains('"miniaudio.framework"')) "The SoundFlow bootstrap must load the embedded iOS framework binary."
Assert-True ($iosSoundFlowBootstrapText.Contains('NSBundle.MainBundle.PrivateFrameworksPath')) "The SoundFlow bootstrap must use the installed app's Frameworks directory."
Assert-True ($audioServiceText.Contains('catch (Exception exception) when (OperatingSystem.IsIOS())')) "A native audio failure on iOS must not prevent the app from starting."
Assert-True ($audioServiceText.Contains('_audioEngine?.Dispose()')) "The optional iOS audio engine must be disposed safely."
$programText = Read-RepositoryFile "ClassIsland/Program.cs"
$settingsText = Read-RepositoryFile "ClassIsland/Models/Settings.cs"
Assert-True ($programText.Contains('sentryPreference == null && !PlatformHelper.IsAppleMobile')) "Sentry must default to opt-in on iOS/iPadOS."
Assert-True ($settingsText.Contains('preference == null && !PlatformHelper.IsAppleMobile')) "The privacy setting must reflect the iOS Sentry opt-in default."
Assert-True ($iosSystemEventsServiceText.Contains('UIApplication.SignificantTimeChangeNotification')) "The iOS system event service must observe significant time changes."
Assert-True ($iosSystemEventsServiceText.Contains('NSTimeZone.SystemTimeZoneDidChangeNotification')) "The iOS system event service must observe time-zone changes."
Assert-True ($iosSystemEventsServiceText.Contains('TimeZoneInfo.ClearCachedData()')) "The iOS system event service must invalidate cached time-zone data."
Assert-True ($iosNotificationCoordinatorText.Contains('UIApplication.DidEnterBackgroundNotification')) "The notification coordinator must refresh after entering the background."
Assert-True ($iosNotificationCoordinatorText.Contains('BeginBackgroundTask(')) "The final notification refresh must request bounded background execution time."
Assert-True ($iosNotificationCoordinatorText.Contains('private sealed class BackgroundTaskLease')) "The final notification refresh must own an idempotent background task lease."
Assert-True ($iosNotificationCoordinatorText.Contains('_expirationCancellation.Cancel()')) "The background task expiration handler must cancel pending notification work."
Assert-True ($iosNotificationCoordinatorText.Contains('application.EndBackgroundTask(identifier)')) "The notification coordinator must immediately release an expired background task lease."
Assert-True ($iosNotificationCoordinatorText.Contains('PlatformServices.SystemEventsService.TimeChanged += SystemEventsOnTimeChanged')) "The notification coordinator must reschedule after system time or time-zone changes."
Assert-True ($iosNotificationCoordinatorText.Contains('PlatformServices.SystemEventsService.TimeChanged -= SystemEventsOnTimeChanged')) "The notification coordinator must release its system-time event subscription."
Assert-True ($iosNotificationCoordinatorText.Contains('ScheduleFailureRetry();')) "Native notification scheduling failures must trigger a bounded retry."
Assert-True ($appDelegateText.Contains('private readonly LessonPreparationNotificationTimeline _lessonPreparationTimeline = new();')) "The AppDelegate must own one shared preparation-notification timeline."
Assert-True (([regex]::Matches($appDelegateText, '_lessonPreparationTimeline')).Count -eq 3) "The AppDelegate must inject the same preparation timeline into notification and Live Activity coordinators."
Assert-True ($iosNotificationScheduleFactoryText.Contains('lessonPreparationTimeline.PlanNotification(')) "The notification planner must register the effective preparation-notification time."
Assert-True ($iosNotificationScheduleFactoryText.Contains('lessonPreparationTimeline.GetCandidateNotificationTime(')) "Candidate selection and final scheduling must share one catch-up time policy."
Assert-True ($iosNotificationScheduleFactoryText.Contains('lessonPreparationTimeline.GetLiveActivityPublicationTime(')) "The Live Activity gate must read the registered preparation-notification time."
Assert-True ($liveActivityCoordinatorText.Contains('_notificationScheduleFactory?.GetUpcomingClassPreparationTime()')) "The Live Activity coordinator must align publication with the preparation notification."
Assert-True ($notificationTimelineText.Contains('private static readonly TimeSpan CatchUpDelay = TimeSpan.FromSeconds(2);')) "Preparation notification catch-up timing must remain centralized."
Assert-True ($notificationTimelineText.Contains('_preparationTimings[notificationIdentifier] = new PreparationTiming(')) "The notification planner must retain both scheduled and catch-up preparation times."
Assert-True ($notificationTimelineText.Contains('return _preparationTimings.TryGetValue(notificationIdentifier')) "Live Activity publication must read the notification planner's retained time."
Assert-True ($notificationTimelineText.Contains('timing.IsScheduled')) "Live Activity publication must wait until the native notification request is accepted."
Assert-True (-not $notificationTimelineText.Contains('timing.LessonStartAt == lessonStartAt')) "Live Activity lookup must use the stable notification identifier across clock remapping."
Assert-True ($notificationTimelineText.Contains('CeilingToWholeSecond(systemNow + CatchUpDelay)')) "Catch-up notification and Live Activity times must share the native trigger's whole-second precision."
Assert-True ($notificationTimelineText.Contains('existing.IsScheduled ||')) "An unconfirmed catch-up time must be replanned after its scheduling window expires."
Assert-True ($iosNotificationSchedulerText.Contains('lessonPreparationTimeline.ReconcileScheduledNotifications(')) "The native scheduler must reconcile preparation gates with current notification requests."
Assert-True ($notificationTimelineText.Contains('ShouldRemoveMissingTiming(')) "Preparation reconciliation must distinguish canceled requests from notifications that may already have fired."
Assert-True ($notificationTimelineText.Contains('timing.FireAt > systemNow + SchedulingSafetyMargin')) "Reconciliation must preserve accepted preparation times after the native request fires."
Assert-True ($iosNotificationSchedulerText.Contains('lessonPreparationTimeline.ConfirmNotificationScheduled(')) "The native scheduler must confirm accepted preparation requests before Live Activity publication."
Assert-True ($iosNotificationSchedulerText.Contains('lessonPreparationTimeline.RestoreNotificationScheduled(')) "The native scheduler must restore preparation timing from pending or delivered native notifications."
Assert-True ($iosNotificationSchedulerText.Contains('deliveredNotification.Date')) "Cross-process preparation recovery must use the native delivery time."
Assert-True ($iosNotificationSchedulerText.Contains('x.Value.ToUnixTimeSeconds()')) "Preparation history must persist the actual fire time, not only its identifier."
Assert-True ($iosNotificationSchedulerText.Contains('synchronizedRequests.Where(x =>') -and $iosNotificationSchedulerText.Contains('x.Identifier.EndsWith(".prepare", StringComparison.Ordinal)')) "Cross-process recovery must persist normally scheduled preparation notifications as well as catch-up requests."
Assert-True ($iosNotificationSchedulerText.Contains('PendingRequestMatches(')) "The iOS scheduler must avoid replacing unchanged native notification requests."
Assert-True ($iosNotificationSchedulerText.Contains('content.Sound != null) == request.PlaySound')) "Native notification diffing must include sound delivery settings."
Assert-True ($iosNotificationSchedulerText.Contains('Task<IosLessonNotificationSynchronizationResult> SynchronizeAsync(')) "The native scheduler must publish its confirmed snapshot and retry classification."
Assert-True ($iosNotificationSchedulerText.Contains('protectedImminentIdentifiers')) "Notification reconciliation must not cancel an imminent native delivery."
Assert-True ($iosNotificationSchedulerText.Contains('GetConfirmedIdentifiersAsync(')) "The scheduler must verify native pending or delivered requests after mutation."
Assert-True ($iosNotificationSchedulerText.Contains('RollbackAsync(')) "A failed native notification synchronization must restore the previous managed schedule."
Assert-True ($iosNotificationSchedulerText.Contains('IosLessonNotificationScheduleSelector.Select(')) "The scheduler must select requests within the available native capacity."
Assert-True ($iosNotificationScheduleSelectorText.Contains('FullyCoveredNearTermDayCount = 2')) "The native notification budget must fully prioritize the nearest two lesson days before long-range anchors."
Assert-True ($iosNotificationSchedulerText.Contains('MinimumUnixTimeSeconds')) "Persisted native notification times must be range-checked before conversion."
Assert-True ($iosNotificationCapacityPolicyText.Contains('MaximumPendingNotificationCount = 64')) "The notification capacity policy must model Apple's 64 pending-request limit."
Assert-True ($iosNotificationCapacityPolicyText.Contains('ReservedFallbackNotificationCount = 1')) "Scheduled lessons must reserve one slot for immediate fallback notifications."
Assert-True ($iosNotificationSynchronizationPolicyText.Contains('ObsoleteIdentifierToRemoveBeforeUpsert')) "A full native schedule must rotate verified managed slots without evicting unrelated notifications."
Assert-True ($iosNotificationSynchronizationExecutionPolicyText.Contains('ShouldDeferLargeMutation(')) "Large background notification mutations must be deferred to the foreground."
Assert-True ($iosNotificationSynchronizationExecutionPolicyText.Contains('hasTransientCapacityPressure')) "Stable native capacity reduction must not be treated as a short-lived synchronization failure."
Assert-True ($iosNotificationMutationGateText.Contains('private readonly SemaphoreSlim _semaphore = new(1, 1)')) "Native notification mutations must be serialized through one gate."
Assert-True ($iosNotificationCoordinatorText.Contains('private readonly IosNotificationMutationGate _notificationMutationGate = new();')) "The notification coordinator must own the shared native mutation gate."
Assert-True (([regex]::Matches($iosNotificationCoordinatorText, '_notificationMutationGate')).Count -ge 3) "The scheduler and immediate queue must share the same mutation gate."
Assert-True ($iosNotificationQueueConsumerText.Contains('UNNotificationRequest.FromIdentifier(')) "Non-lesson notification tickets must be converted to native iOS notifications."
Assert-True ($iosNotificationQueueConsumerText.Contains('AddNotificationRequestAsync(request)')) "Immediate iOS notifications must be submitted to UNUserNotificationCenter."
Assert-True ($iosNotificationQueueConsumerText.Contains('finally') -and $iosNotificationQueueConsumerText.Contains('await CompleteNotificationAsync(notification);')) "Every iOS notification ticket must be completed even when native delivery fails or the queue drains."
Assert-True ($iosNotificationQueueConsumerText.Contains('Channel.CreateBounded<QueuedNotification>')) "Immediate iOS notifications must use one bounded FIFO worker instead of unbounded fire-and-forget tasks."
Assert-True ($iosNotificationQueueConsumerText.Contains('SingleReader = true')) "The iOS notification queue must have a single ordered consumer."
Assert-True ($iosNotificationQueueConsumerText.Contains('ticket.CancellationToken.IsCancellationRequested')) "Immediate iOS delivery must skip canceled tickets."
Assert-True ($iosNotificationQueueConsumerText.Contains('_settingsService.Settings.AllowNotificationSound')) "Immediate iOS notifications must respect the global sound gate."
Assert-True ($iosNotificationQueueConsumerText.Contains('public void Dispose()')) "The iOS notification queue must stop and drain during coordinator disposal."
Assert-True ($iosNotificationQueueConsumerText.Contains('Dispatcher.UIThread.InvokeAsync(() => CompleteNotificationCore(notification))')) "NotificationHost ticket completion must return to the Avalonia UI thread."
Assert-True ($iosNotificationQueueConsumerText.Contains('private sealed record QueuedNotification(')) "Avalonia notification content must be materialized before the background native worker reads it."
Assert-True ($iosNotificationQueueConsumerText.Contains('_notificationHostService.PullNotificationRequests()')) "The iOS notification consumer must pull the next host batch after becoming idle."
Assert-True ($iosNotificationQueueConsumerText.Contains('Volatile.Read(ref _queuedNotificationCount)')) "The iOS notification consumer must report its actual queued batch size."
Assert-True ($iosNotificationQueueConsumerText.Contains('MaximumFallbackCapacityWait')) "Immediate fallback delivery must wait only for a bounded native-capacity window."
Assert-True ($iosNotificationQueueConsumerText.Contains('GetFallbackSubmissionDecision(')) "Immediate fallback delivery must not rely on iOS evicting an existing pending request."
Assert-True ($iosNotificationQueueConsumerText.Contains('ChainedHeadRequest ?? x.Request')) "One chained reminder must be collapsed into one native fallback notification."
Assert-True ($iosNotificationQueueConsumerText.Contains('CapacityExhaustionProbeInterval')) "Capacity exhaustion must be shared across consecutive host batches instead of waiting per ticket."
Assert-True ($coverageRunsettingsText.Contains('ClassIsland.iOS.Services.Notifications.*')) "Coverage enforcement must include the new notification scheduling policies."
Assert-True ($coverageRunsettingsText.Contains('IosFallbackNotificationPayloadPolicy')) "Coverage enforcement must include immediate fallback payload shaping."
Assert-True ($coverageRunsettingsText.Contains('PortableImportedFileReference')) "Coverage enforcement must include portable imported-file references."
Assert-True ($coverageRunsettingsText.Contains('SafeArchivePath')) "Coverage enforcement must include cross-platform archive path rules."
Assert-True ($coverageRunsettingsText.Contains('TrustedFileSystemPathPolicy')) "Coverage enforcement must include trusted-root filesystem traversal rules."
Assert-True ($storageItemMaterializerText.Contains('DefaultMaximumFileCount = 4096')) "Materialized storage selections must retain the 4096-file limit."
Assert-True ($storageItemMaterializerText.Contains('DefaultMaximumFileLength = 256L * 1024 * 1024')) "Materialized storage selections must retain the 256 MiB per-file limit."
Assert-True ($storageItemMaterializerText.Contains('DefaultMaximumTotalLength = 1024L * 1024 * 1024')) "Materialized storage selections must retain the 1 GiB aggregate limit."
Assert-True ($storageItemMaterializerText.Contains('DisposeItems(files)') -and $storageItemMaterializerText.Contains('DisposeItems(folders)')) "Materialized iOS storage items must release their security-scoped resources."
Assert-True ($storageItemMaterializerText.Contains('SafeArchivePath.SanitizeFileNameSegment(')) "Materialized persistent filenames must remain round-trip safe for ClassIsland data archives."
Assert-True ($portableImportedFileReferenceText.Contains('internal const string Prefix = "_classisland-imported:"')) "Persistent imported-file references must be independent of the sandbox container UUID."
Assert-True ($portableImportedFileReferenceText.Contains('TryGetAppleDocumentsIndex(')) "Legacy absolute-path migration must require an Apple application-container Documents path."
Assert-True ($portableImportedFileReferenceText.Contains('Documents/ClassIsland/ImportedFiles')) "Persistent references must recognize the current shared Documents layout."
Assert-True ($portableImportedFileReferenceText.Contains('Documents/ClassIsland/Data/ImportedFiles')) "Persistent references must migrate the legacy iOS imported-files layout."
Assert-True ($importedFileReferenceText.Contains('PlatformHelper.IsAppleMobile')) "Legacy absolute imported-file paths must not be rewritten on desktop platforms."
Assert-True ($persistentImportedFileServiceText.Contains('CommonDirectories.AppImportedFilesFolderPath')) "Persistent selections must be copied into the Files-visible imported-files directory."
Assert-True ($iosPlatformFilePickerServiceText.Contains('CommonDirectories.AppTempFolderPath')) "One-shot iOS file selections must use temporary materialization."
Assert-True ($iosPlatformFilePickerServiceText.Contains('PersistentImportedFileService.ImportAsync(files)')) "Long-lived iOS file selections must opt into persistent materialization."
Assert-True ($fileBrowserButtonText.Contains('PersistSelectionProperty')) "File-backed settings must explicitly request persistent iOS references."
Assert-True ($fileSystemDataTransactionText.Contains('ResolveInsideRoot(')) "Data transactions must resolve every relative operation under its intended root."
Assert-True ($fileSystemDataTransactionText.Contains('RejectReparsePoint(')) "Data transactions must reject symbolic-link or reparse-point traversal."
Assert-True ($fileSystemDataTransactionText.Contains('TrustedFileSystemPathPolicy') -and $fileSystemDataTransactionText.Contains('.GetControlledComponents(path, trustedRoot)')) "Reparse-point checks must stop at an application-controlled boundary instead of rejecting Darwin system links."
Assert-True ($trustedFileSystemPathPolicyText.Contains('Path.GetRelativePath(fullRoot, fullPath)')) "Trusted filesystem traversal must reject paths outside its application-controlled root."
Assert-True ($safeArchivePathText.Contains('IsWindowsReservedName(')) "Archive paths must reject Windows device names across every source platform."
Assert-True ($safeArchivePathText.Contains('character <')) "Archive paths must reject control characters."
Assert-True ($safeArchiveExtractorText.Contains('IsSymbolicLink(entry)')) "Staged imports must reject symbolic links in archives."
Assert-True ($safeArchiveExtractorText.Contains('EnsureInsideRoot(')) "Staged imports must reject archive paths escaping the staging root."
Assert-True ($dataTransferPageText.Contains('ZipArchiveSafety.ValidateForClassIslandDataExtraction(archive)') -and $dataTransferPageText.Contains('SafeArchiveExtractor.ExtractSelected(')) "CSES import must validate its dedicated round-trip budget before staged extraction."
Assert-True ($dataTransferPageText.Contains('FileSystemDataTransaction.Execute(')) "CSES import must replace live data through a rollback transaction."
Assert-True ($recoverBackupPageText.Contains('SafeArchiveExtractor.ExtractSelected(') -and $recoverBackupPageText.Contains('FileSystemDataTransaction.Execute(')) "Backup recovery must use staged extraction and rollback transactions."
Assert-True ($recoverBackupPageText.Contains('if (!Directory.Exists(stagedImportedFiles))')) "Legacy backups without ImportedFiles must preserve the current shared imported files."
Assert-True ($storageSettingsPageText.Contains('RecheckAndDeleteImportedItems(') -and $storageSettingsPageText.Contains('UninspectableSource')) "Imported-file cleanup must recheck references and fail closed before deletion."
Assert-True ($safeChildDirectoryPathText.Contains("childDirectoryName.IndexOfAny(['/', '\\'])")) "Plugin directory identifiers must reject both path separator styles."
Assert-True ($safeChildDirectoryPathText.Contains('Path.GetRelativePath(rootPath, targetPath)')) "Plugin directory resolution must verify containment under the plugin root."
Assert-True ($pluginServiceText.Contains('SafeChildDirectoryPath.Resolve(')) "Plugin installation must resolve untrusted manifest IDs through the safe child-path helper."
Assert-True (-not $pluginMarketServiceText.Contains('ServicePointManager.ServerCertificateValidationCallback')) "Plugin downloads must not replace the process-wide TLS validation callback."
Assert-True ($pluginMarketServiceText.Contains('CustomHttpMessageHandlerFactory')) "The optional plugin mirror TLS override must be scoped to its own HTTP handler."
Assert-True ($pluginMarketServiceText.Contains('InstallPluginIndexArchive(') -and $pluginMarketServiceText.Contains('ZipArchiveSafety.ValidateForExtraction(archive)')) "Plugin indexes must be validated and transactionally staged before installation."
Assert-True ($xamlThemeServiceText.Contains('ReplaceThemeDirectory(') -and $xamlThemeServiceText.Contains('ZipArchiveSafety.ValidateForExtraction(pkg)')) "Theme packages must be validated and transactionally staged before installation."
Assert-True ($recoverBackupPageText.Contains('ZipArchiveSafety.ValidateForClassIslandDataExtraction(archive)')) "Backup ZIP recovery must enforce the dedicated ClassIsland data archive policy."
Assert-True ($pluginsSettingsPageText.Contains('ZipArchiveSafety.ValidateForExtraction(pkg)')) "Plugin previews must validate package resource limits before reading content."
Assert-True ($managementConnectionText.Contains('var hash = SHA256.HashData(ClientGuid.ToByteArray())')) "iOS management identity must have a stable privacy-safe fallback when MAC addresses are unavailable."
$hideViewMethod = [regex]::Match(
    $mobileViewHostText,
    '(?s)public\s+async\s+Task<bool>\s+HideView\(.*?(?=\n\s*private\s+async\s+Task\s+RunNavigationWithProgressAsync)')
Assert-True ($hideViewMethod.Success) "MobileViewHost.HideView could not be validated."
Assert-True (([regex]::Matches($hideViewMethod.Value, 'ViewDeactivating\(')).Count -eq 1) "MobileViewHost.HideView must not invoke ViewDeactivating before a normal navigation pop."
$rootPageBranchIndex = $hideViewMethod.Value.IndexOf('if (NavigationPage.Pages?.Count() <= 1)', [StringComparison]::Ordinal)
$viewDeactivatingIndex = $hideViewMethod.Value.IndexOf('view.ViewDeactivating(', [StringComparison]::Ordinal)
$rootPageElse = [regex]::Match($hideViewMethod.Value, '\r?\n\s{8}else\s*\{')
$popAsyncIndex = $hideViewMethod.Value.IndexOf('NavigationPage.PopAsync()', [StringComparison]::Ordinal)
Assert-True ($rootPageBranchIndex -ge 0 -and $rootPageElse.Success) "MobileViewHost.HideView must retain distinct root-page and navigation-pop branches."
Assert-True ($viewDeactivatingIndex -gt $rootPageBranchIndex -and $viewDeactivatingIndex -lt $rootPageElse.Index) "Only the root-page branch may explicitly invoke ViewDeactivating."
Assert-True ($popAsyncIndex -gt $rootPageElse.Index) "Normal mobile navigation must rely on the navigation pop lifecycle callback."
$removeViewIndex = $hideViewMethod.Value.IndexOf('ActivatedViews.Remove(view)', [StringComparison]::Ordinal)
$hideHostIndex = $hideViewMethod.Value.IndexOf('Hide();', [StringComparison]::Ordinal)
Assert-True ($removeViewIndex -gt $viewDeactivatingIndex -and $removeViewIndex -lt $hideHostIndex) "The root view must be deactivated before a synchronous native hide callback can destroy the host."
$windowRuleHandler = [regex]::Match(
    $settingsWindowText,
    '(?s)private\s+void\s+MenuItemDebugWindowRule_OnClick\(.*?(?=\n\s*private\s+(?:async\s+)?(?:void|Task(?:<[^>]+>)?)\s+)')
Assert-True ($windowRuleHandler.Success) "The window-rule debug handler could not be validated."
Assert-True ($windowRuleHandler.Value.Contains('if (PlatformHelper.IsAppleMobile)')) "Apple-only window-rule gating must not hide the Android implementation."
Assert-True (-not $windowRuleHandler.Value.Contains('PlatformHelper.IsMobile')) "The window-rule handler must not reject Android as an unsupported platform."
$pluginsSettingsRegistration = [regex]::Match(
    $appServicesText,
    '(?s)if\s*\(\s*!PlatformHelper\.IsAppleMobile\s*\)\s*\{\s*services\.AddSettingsPage<PluginsSettingsPage>\(\);\s*\}')
Assert-True ($pluginsSettingsRegistration.Success) "The plugins settings page must be hidden only on iOS/iPadOS."
Assert-True ($appServicesText.Contains('System.OperatingSystem.IsWindows() || System.OperatingSystem.IsMacOS() || System.OperatingSystem.IsLinux()')) "Bundled desktop tutorials must not be registered on mobile platforms."
Assert-True ($welcomeWindowText.Contains('if (!isOnboarding || PlatformHelper.IsAppleMobile)')) "The iOS onboarding flow must omit the desktop system-integration page."
Assert-True ($finishWelcomePageText.Contains('DesktopTrayTutorial.IsVisible = false') -and
             $finishWelcomePageText.Contains('DesktopProfileTutorial.IsVisible = false') -and
             $finishWelcomePageText.Contains('Carousel.SelectedIndex = 2')) "The iOS onboarding finish page must skip desktop-only tray tutorials."
Assert-True ($appText.Contains('if (Equals(result, true))')) "Manual-termination resources must resume only after the user explicitly cancels the iOS close request."

Assert-True ($wrapperWorkflowText.Contains("name: Build iOS")) "The observable iOS workflow must keep the Build iOS name."
Assert-True ($wrapperWorkflowText.Contains("uses: ./.github/workflows/_build_ios_reusable.yml")) "The Build iOS workflow must call the reusable iOS worker."
Assert-True ($wrapperWorkflowText.Contains('checkout_ref: ${{ github.sha }}')) "The Build iOS wrapper must build the triggering commit."
Assert-True ($wrapperWorkflowText.Contains("developer_preview: true")) "The standalone Build iOS workflow must enable DeveloperPreview."
Assert-True ($wrapperWorkflowText.Contains("github.event.pull_request.head.repo.full_name == github.repository")) "Authenticated iOS builds must not execute fork pull-request code."
Assert-True ($wrapperWorkflowText.Contains("group: ios-`${{ github.workflow }}-`${{ github.ref }}")) "The Build iOS wrapper must retain its concurrency group."
Assert-True ($wrapperWorkflowText.Contains(".github/workflows/_build_ios_reusable.yml")) "Changes to the reusable iOS worker must trigger Build iOS."
Assert-True ($wrapperWorkflowText.Contains(".github/workflows/build_release.yml")) "Changes to iOS release artifact wiring must trigger Build iOS."
Assert-True ($wrapperWorkflowText.Contains("tools/ci/verify-ios-ipa.sh")) "Changes to IPA verification must trigger Build iOS."
Assert-True ($wrapperWorkflowText.Contains("tools/ci/normalize-ios-ipa.sh")) "Changes to IPA normalization must trigger Build iOS."
Assert-True ($wrapperWorkflowText.Contains("tools/ci/verify-cobertura-coverage.ps1")) "Changes to coverage verification must trigger Build iOS."
Assert-True (-not $wrapperWorkflowText.Contains("runs-on:")) "The Build iOS wrapper must not duplicate the macOS worker implementation."

Assert-True ($workerWorkflowText.Contains("workflow_call:")) "The shared iOS worker must be a reusable workflow."
foreach ($inputName in @("checkout_ref", "app_version", "build_number", "brand_type", "developer_preview", "artifact_name", "retention_days")) {
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
Assert-True ($workerWorkflowText.Contains('bash ./tools/ci/normalize-ios-ipa.sh "$IPA_PATH"')) "The iOS worker must normalize inherited signatures before verification."
Assert-True ($workerWorkflowText.Contains("bash ./tools/ci/verify-ios-ipa.sh")) "The iOS worker must run the shared IPA verification script."
Assert-True ($workerWorkflowText.Contains('name: ${{ inputs.artifact_name }}')) "The iOS worker must expose a caller-controlled artifact name."

Assert-True ($ipaNormalizationText.Contains("codesign --remove-signature")) "IPA normalization must remove inherited code signatures."
Assert-True ($ipaNormalizationText.Contains("embedded.mobileprovision")) "IPA normalization must remove inherited provisioning profiles."
Assert-True ($ipaNormalizationText.Contains("-name '*.xpc' -o -name '*.bundle'")) "IPA normalization must inspect nested XPC and resource bundles."
Assert-True ($ipaNormalizationText.Contains('mv -f "$repacked_ipa" "$ipa_path"')) "IPA normalization must replace the input only after repacking succeeds."

Assert-True ($coverageVerificationText.Contains('GetAttribute("line-rate")')) "Coverage verification must read the Cobertura root line rate."
Assert-True ($coverageVerificationText.Contains('$lineRate -lt $MinimumLineRate')) "Coverage verification must fail below the requested threshold."

Assert-True ($releaseWorkflowText.Contains("build_ios_unsigned:")) "The release workflow must expose the unsigned iOS build job."
Assert-True ($releaseWorkflowText.Contains("if: `${{ github.event_name == 'workflow_dispatch' }}")) "The release workflow must build iOS only for an explicit dispatch."
Assert-True ($releaseWorkflowText.Contains('checkout_ref: ${{ inputs.release_tag }}')) "The release iOS build must checkout the requested release tag."
Assert-True ($releaseWorkflowText.Contains("developer_preview: false")) "The release iOS build must disable DeveloperPreview."
Assert-True ($releaseWorkflowText.Contains("artifact_name: out_app_ios_arm64_selfContained_ipa")) "The release iOS artifact must use the release collector naming convention."
Assert-True ($releaseWorkflowText.Contains("needs: [ pack_app, build_nupkg, build_android, build_ios_unsigned ]")) "Publishing must wait for Android and unsigned iOS artifacts."
Assert-True ($releaseWorkflowText.Contains("sha256sum --check")) "Publishing must verify the downloaded iOS checksum."
Assert-True ($releaseWorkflowText.Contains("./out/*.ipa,./out/*.sha256")) "The release draft must include the unsigned IPA and checksum."

Assert-True ($artifactInitializationText.Contains('$payloadName')) "Release checksum regeneration must reference the renamed payload by basename."
Assert-True ($artifactInitializationText.Contains('Get-FileHash -LiteralPath $payloadPath -Algorithm SHA256')) "Release artifact normalization must regenerate SHA-256 sidecars."

Assert-True ($ipaVerificationText.Contains("<ipa-path> <application-id> <runtime-identifier>")) "IPA verification must document its three required arguments."
Assert-True ($ipaVerificationText.Contains("MonoTouchDebugConfiguration.txt")) "IPA verification must reject MonoTouch debug configuration."
Assert-True ($ipaVerificationText.Contains("libxamarin-dotnet-debug")) "IPA verification must reject the remote-debug runtime."
Assert-True ($ipaVerificationText.Contains('assert_minimum_ios "$app_binary" "The main app" "15.0"')) "IPA verification must assert iOS 15.0 for the main app."
Assert-True ($ipaVerificationText.Contains('assert_minimum_ios "$bridge_binary" "The Live Activity bridge" "15.0"')) "IPA verification must assert iOS 15.0 for the bridge."
Assert-True ($ipaVerificationText.Contains('expected_miniaudio_install_name="@rpath/miniaudio.framework/miniaudio"')) "IPA verification must validate the SoundFlow framework install name."
Assert-True ($ipaVerificationText.Contains('The main app does not load the embedded SoundFlow miniaudio framework')) "IPA verification must validate the main app's miniaudio load command."
Assert-True ($ipaVerificationText.Contains('The embedded SoundFlow miniaudio binary is not executable')) "IPA verification must validate miniaudio executable permissions."
Assert-True ($ipaVerificationText.Contains('load_command == "LC_LOAD_DYLIB"')) "IPA verification must require a strong miniaudio load command."
Assert-True ($ipaVerificationText.Contains('ma_context_init sf_allocate_context')) "IPA verification must validate representative SoundFlow native exports."
Assert-True ($ipaVerificationText.Contains("CFBundleDisplayName")) "IPA verification must validate the final display name."
Assert-True ($ipaVerificationText.Contains("assert_unsigned_bundle")) "IPA verification must reject signed app, extension, and bridge bundles."
Assert-True ($ipaVerificationText.Contains('ipa_basename="$(basename "$ipa_path")"')) "IPA verification must generate a portable basename-only checksum."
Assert-True ($ipaVerificationText.Contains('privacy_manifest="$app_bundle/PrivacyInfo.xcprivacy"')) "IPA verification must require the bundled privacy manifest."
Assert-True ($ipaVerificationText.Contains("NSPrivacyAccessedAPICategoryUserDefaults")) "IPA verification must validate UserDefaults privacy disclosure."
Assert-True ($ipaVerificationText.Contains("CA92.1")) "IPA verification must validate UserDefaults reason CA92.1."
Assert-True ($ipaVerificationText.Contains("NSPrivacyAccessedAPICategoryFileTimestamp")) "IPA verification must validate file timestamp privacy disclosure."
Assert-True ($ipaVerificationText.Contains("C617.1")) "IPA verification must validate file timestamp reason C617.1."
Assert-True ($ipaVerificationText.Contains("NSPrivacyAccessedAPICategorySystemBootTime")) "IPA verification must validate system boot time privacy disclosure."
Assert-True ($ipaVerificationText.Contains("35F9.1")) "IPA verification must validate elapsed-time reason 35F9.1."
Assert-True ($ipaVerificationText.Contains("NSPrivacyCollectedDataTypeCrashData")) "IPA verification must validate the optional Sentry crash-data disclosure."
Assert-True ($ipaVerificationText.Contains("NSPrivacyCollectedDataTypePerformanceData")) "IPA verification must validate the optional Sentry performance disclosure."
Assert-True ($ipaVerificationText.Contains("NSPrivacyCollectedDataTypeOtherDiagnosticData")) "IPA verification must validate the optional Sentry diagnostics disclosure."
Assert-True ($ipaVerificationText.Contains("NSPrivacyCollectedDataTypeProductInteraction")) "IPA verification must validate the optional Sentry interaction disclosure."

Assert-True ($nukeBuildText.Contains('SetProperty("EnableCodeSigning", enableCodeSigning)')) "NUKE iOS publish must explicitly support signed and unsigned modes."
Assert-True ($nukeBuildText.Contains('SetProperty("ArchiveOnBuild", enableCodeSigning)')) "Unsigned NUKE publish must skip xcarchive creation while still building the IPA."
Assert-True ($nukeBuildText.Contains('SetProperty("BuildIpa", true)')) "NUKE iOS publish must keep IPA packaging enabled."
$iosPublishProperties = [regex]::Match($nukeBuildText, '(?s)DotNetPublish\(settings =>.*?SetProject\(IosAppEntryProject\).*?if \(!EnableCodeSigning\)')
Assert-True ($iosPublishProperties.Success) "The NUKE iOS publish block could not be validated."
Assert-True ($iosPublishProperties.Value.Contains('SetProcessArgumentConfigurator(arguments => arguments.Add("-m:1"))')) "NUKE iOS publish must serialize duplicate Avalonia project-reference builds."
Assert-True ($iosPublishProperties.Value.Contains('SetProperty("PublishBuilding", true)')) "NUKE iOS publish must exclude non-publish fallback secrets."
Assert-True ($iosPublishProperties.Value.Contains('SetProperty("PublishPlatform", OsName)')) "NUKE iOS publish must not inherit the macOS runner platform."
Assert-True ($iosPublishProperties.Value.Contains('SetProperty("ClassIsland_PlatformTarget", Arch)')) "NUKE iOS publish must define the release architecture."
Assert-True ($iosPublishProperties.Value.Contains('SetProperty("GeneratePackageOnBuild", false)')) "NUKE iOS publish must not concurrently pack project references."
Assert-True ($iosPublishProperties.Value.Contains('SetProperty("WarningsAsErrors", "CA1416")')) "NUKE iOS publish must fail on unguarded platform compatibility warnings."
Assert-True ($nukeBuildText.Contains('SetProperty("DebugType", "none")')) "NUKE iOS Release publish must disable PDB generation for every project reference."
Assert-True ($nukeBuildText.Contains('SetProperty("DebugSymbols", false)')) "NUKE iOS Release publish must disable debug symbols for every project reference."
$enableCodeSigningSchema = @($nukeSchema.allOf) |
    ForEach-Object { $_.properties.EnableCodeSigning } |
    Where-Object { $null -ne $_ } |
    Select-Object -First 1
Assert-True ($enableCodeSigningSchema.type -eq "boolean") "The NUKE schema must expose EnableCodeSigning as a boolean parameter."

Write-Output "iOS build configuration verification passed."
