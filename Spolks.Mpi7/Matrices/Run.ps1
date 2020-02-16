$processes = Read-Host 'Enter count of processes'

mpiexec -n $processes .\bin\Debug\netcoreapp3.0\Matrices.exe

Read-Host -Prompt "Press Enter to continue"