#pragma once

#include <mpi.h>
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
		const String^ FILE_A_NAME = "matrixA.matrix";
		const String^ FILE_B_NAME = "matrixB.matrix";
		const String^ FILE_RESULT_NAME = "result.matrix";

		String^ _fileA;
		String^ _fileB;
		String^ _fileResult;

		int _groups;
		MPI_Comm _groupCommunicator;

	public:
		FileOperations(String^ fileA, String^ fileB, String^ fileResult, int groups);
		
		void Fill(Func<Matrix2D<long long>^>^ fillerA, Func<Matrix2D<long long>^>^ fillerB);
		void Multiply();
		bool Compare();
	private:
		Dictionary<int, MPI_Group>^ CreateCroups(MPI_Comm communicator);
		void MultiplyToFile(Matrix2D<long long>^ matrixA, Matrix2D<long long>^ matrixB, MPI_Comm communicator);
		Matrix2D<long long>^ ReadMatrix(const char* filePath, MPI_Comm communicator);
		void WriteMatrix(Matrix2D<long long>^ matrix, const char* filePath);
		const char* ToCString(String^ str);
		const long long* ToCArray(array<long long>^ arr);
		const int* ToCArray(array<int>^ arr);
		void WriteFrame(array<long long>^ frame, int rows, int columns, int start);
	};
}