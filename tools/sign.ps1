$ErrorActionPreference = "Stop"

Install-Module -Force -AcceptLicense -Name SignPath
# The user must be a submitter for the given signing policy!
Submit-SigningRequest `
  -InputArtifactPath ./out/ClassIsland/ClassIsland.exe `
  -ApiToken $env:SIGNPATH_TOKEN  `
  -OrganizationId "74962648-db7a-4a10-bfdd-0637542e39df" `
  -ProjectSlug "ClassIsland" `
  -SigningPolicySlug "test-signing" `
  -OutputArtifactPath "./out/ClassIsland/ClassIsland_signed.exe" `
  -WaitForCompletion

mv -Force "./out/ClassIsland/ClassIsland_signed.exe" "./out/ClassIsland/ClassIsland.exe"