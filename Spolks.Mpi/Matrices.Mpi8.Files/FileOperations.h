#pragma once

#include <mpi.h>
#include <iostream>
#include <msclr/marshal.h>
#include "MatrixSerializer.h"

using namespace msclr::interop;
using namespace System;
using namespace System::Collections::Generic;
using namespace System::Text;
using namespace System::Linq;
using namespace Matrices::Shared;
using namespace Matrices::Shared::Services;
using namespace Matrices::Shared::Extensions;
using namespace FilesMultiplication;

namespace FilesMultiplication {

	public ref class FileOperations {
	private:
		const int MASTER_RANK = 0;

		const char* _fileA_cstr;
		const char* _fileB_cstr;
		const char* _fileResult_cstr;

		int _groups;
		MPI_Comm _groupCommunicator;

	public:
		FileOperations(String^ fileA, String^ fileB, String^ fileResult, int groups);
		
		void Multiply();
		bool Compare();
	private:
		Dictionary<int, MPI_Group>^ CreateCroups(MPI_Comm communicator);
		void FillFileNames(String^ fileA, String^ fileB, String^ fileResult);
		void MultiplyToFile(Matrix2D<long long>^ matrixA, Matrix2D<long long>^ matrixB, MPI_Comm communicator);
		Matrix2D<long long>^ ReadMatrix(const char* filePath, MPI_Comm communicator);
	};
}