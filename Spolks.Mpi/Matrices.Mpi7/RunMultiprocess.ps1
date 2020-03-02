$processes = Read-Host 'Enter count of processes'
$matrixN = Read-Host 'Enter matrix N'
$rowsPerCycle = Read-Host 'Enter rows per cycle'

mpiexec -n $processes .\bin\Debug\netcoreapp3.1\Matrices.Mpi7.exe $matrixN $matrixN $matrixN $rowsPerCycle

Read-Host -Prompt "Press Enter to continue"