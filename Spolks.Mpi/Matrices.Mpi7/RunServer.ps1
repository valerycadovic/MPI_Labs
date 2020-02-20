$hosts = 0
do {
    $inputValid = [int]::TryParse((Read-Host 'Enter count of hosts'), [ref]$hosts)
    if (-not $inputValid -or $hosts -lt 0) {
        Write-Host 'count of hosts should be positive integer'
    }
} while (-not $inputValid -or $hosts -lt 0)

$ipAddresses = New-Object Collections.Generic.List[string]

$ref = $null
for ($i = 0; $i -lt $hosts; $i++) {
    $address = Read-Host 'Enter ip address of host' $i
    if (-not [ipaddress]::TryParse($address, [ref]$ref)) {
        $i--
        continue
    }

    $ipAddresses.Add($address)
}

$ips = [string]::Join(' ', $ipAddresses)

smpd -d 0 -p 8677

$matrixARows = Read-Host 'Enter matrix A rows count'
$matrixAColumns = Read-Host 'Enter matrix A columns and matrix B rows count'
$matrixBColumns = Read-Host 'Enter matrix B columns count'

mpiexec.exe -p 8677 -hosts $ipAddresses.Count $ipAddresses -env .\bin\Debug\netcoreapp3.0\Matrices.Mpi7exe $matrixARows $matrixAColumns $matrixBColumns

Read-Host -Prompt "Press Enter to continue"