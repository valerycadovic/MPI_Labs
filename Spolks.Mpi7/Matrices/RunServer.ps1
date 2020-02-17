$hosts = 0
do {
    $inputValid = [int]::TryParse((Read-Host 'Enter count of hosts'), [ref]$hosts)
    if (-not $inputValid -or $hosts -lt 0) {
        Write-Host 'count of hosts should be positive integer'
    }
} while (-not $inputValid -or $hosts -lt 0)

$ipAddresses = New-Object Collections.Generic.List[string]

$ref = $null
$subnetIp = ''
do {
    $subnetIp = Read-Host 'Enter subnet ip address'
    if ([ipaddress]::TryParse($subnetIp, [ref]$ref)) {
        break
    }
} while (true)

$mask = ''
do {
    $mask = Read-Host 'Enter subnet mask'
    if ([ipaddress]::TryParse($mask, [ref]$ref)) {
        break
    }
} while (true)

for ($i = 0; $i -lt $hosts; $i++) {
    $address = Read-Host 'Enter ip address of host $i'

    if (-not [ipaddress]::TryParse($address, [ref]$ref)) {
        $i--
        continue
    }

    $ipAddresses.Add($address)
}

$ips = [string]::Join(' ', $ipAddresses)

msmpilaunchsvc.exe
smpd -d 0 -p 8677

$matrixARows = Read-Host 'Enter matrix A rows count'
$matrixAColumns = Read-Host 'Enter matrix A columns and matrix B rows count'
$matrixBColumns = Read-Host 'Enter matrix B columns count'

mpiexec.exe -p 8677 -hosts $ipAddresses.Count $ips -env MPICH_NETMASK '$subnetIp/$mask' -env MPICH_ND_ZCOPY_THRESHOLD -1 -env MPICH_DISABLE_ND 1 -env MPICH_DISABLE_SOCK 0 -affinity .\bin\Debug\netcoreapp3.0\Matrices.exe $matrixARows $matrixAColumns $matrixBColumns

Read-Host -Prompt "Press Enter to continue"