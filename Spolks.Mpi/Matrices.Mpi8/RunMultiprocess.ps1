$processes = Read-Host 'Enter count of processes'
$matrixN = Read-Host 'Enter matrix N'
$groups = Read-Host 'Enter groups count'

mpiexec -n $processes .\bin\Debug\netcoreapp3.1\Matrices.Mpi8.exe $matrixN $groups -f

Read-Host -Prompt "Press Enter to continue"