param(
    [Parameter(Mandatory = $true)]
    [string]$ResultsDirectory,

    [ValidateRange(0.0, 1.0)]
    [double]$MinimumLineRate = 0.8
)

$ErrorActionPreference = "Stop"

$reports = @(
    Get-ChildItem -LiteralPath $ResultsDirectory -Recurse -File -Filter "coverage.cobertura.xml"
)
if ($reports.Count -ne 1) {
    throw "Expected exactly one Cobertura report under '$ResultsDirectory', found $($reports.Count)."
}

[xml]$coverage = Get-Content -LiteralPath $reports[0].FullName -Raw
$lineRateText = $coverage.coverage.GetAttribute("line-rate")
if ([string]::IsNullOrWhiteSpace($lineRateText)) {
    throw "Cobertura report '$($reports[0].FullName)' does not contain a root line-rate."
}

$lineRate = [double]::Parse(
    $lineRateText,
    [Globalization.CultureInfo]::InvariantCulture)
$percentage = $lineRate * 100
$minimumPercentage = $MinimumLineRate * 100
Write-Output (
    "Platform abstraction line coverage: {0:N2}% (minimum {1:N2}%)." -f
    $percentage,
    $minimumPercentage)

if ($lineRate -lt $MinimumLineRate) {
    throw (
        "Platform abstraction line coverage {0:N2}% is below the required {1:N2}%." -f
        $percentage,
        $minimumPercentage)
}
