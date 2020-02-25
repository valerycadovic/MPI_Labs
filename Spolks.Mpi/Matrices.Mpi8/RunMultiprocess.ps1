$processes = Read-Host 'Enter count of processes'
$matrixARows = Read-Host 'Enter matrix A rows count'
$matrixAColumns = Read-Host 'Enter matrix A columns and matrix B rows count'
$matrixBColumns = Read-Host 'Enter matrix B columns count'
$groups = Read-Host 'Enter groups count'

$question = 'Select executoin mode'
$choices  = '&File', '&NoFile'

$decision = $Host.UI.PromptForChoice($title, $question, $choices, 1)
$f = ''
$filePath = ''
if ($decision -eq 0){
    $f = '-f'
    $filePath = Read-Host 'Enter files path'
}

mpiexec -n $processes .\bin\Debug\netcoreapp3.1\Matrices.Mpi8.exe $matrixARows $matrixAColumns $matrixBColumns $groups $f

Read-Host -Prompt "Press Enter to continue"