$processes = Read-Host 'Enter count of processes'
$matrixARows = Read-Host 'Enter matrix A rows count'
$matrixAColumns = Read-Host 'Enter matrix A columns and matrix B rows count'
$matrixBColumns = Read-Host 'Enter matrix B columns count'
$groups = Read-Host 'Enter groups count'

mpiexec -n $processes .\bin\Debug\netcoreapp3.0\Matrices.Mpi8.exe $matrixARows $matrixAColumns $matrixBColumns $groups

Read-Host -Prompt "Press Enter to continue"