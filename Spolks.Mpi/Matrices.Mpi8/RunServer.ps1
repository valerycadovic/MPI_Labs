﻿$hosts = 0
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
        Write-Host 'Wrong IP address format'
        $i--
        continue
    }

    $processes = 0
    do {
        $isValid = [int]::TryParse((Read-Host 'Enter count of processes of host' $i), [ref]$processes)
        if (-not $isValid -or $processes -lt 0) {
            Write-Host 'Count of processes should be a positive integer number'
        }
    } while (-not $isValid -or $processes -lt 0)

    $ipAddresses.Add($address) 
    $ipAddresses.Add($processes)
}

$ips = [string]::Join(' ', $ipAddresses)

$matrixARows = Read-Host 'Enter matrix A rows count'
$matrixAColumns = Read-Host 'Enter matrix A columns and matrix B rows count'
$matrixBColumns = Read-Host 'Enter matrix B columns count'
$groups = Read-Host 'Enter groups count'

$count = $ipAddresses.Count / 2

$question = 'Select executoin mode'
$choices  = '&File', '&NoFile'

$decision = $Host.UI.PromptForChoice($title, $question, $choices, 1)
$f = ''
$filePath = ''
$smpd = ''
if ($decision -eq 0){
    $f = '-f'
    $filePath = Read-Host 'Enter files path'
}
else {
    Start-Process -FilePath "smpd.exe" -ArgumentList ("-p", "8677", "-d", "0")
    $smpd = Get-Process -Name "smpd"
}

mpiexec.exe -p 8677 -hosts $count $ipAddresses .\bin\Debug\netcoreapp3.1\Matrices.Mpi8.exe $matrixARows $matrixAColumns $matrixBColumns $groups $f

Read-Host -Prompt "Press Enter to continue"
if ($smpd -ne '') {
    Stop-Process -InputObject $smpd
}