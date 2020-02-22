#pragma once

using namespace System;
using namespace System::Collections::Generic;
using namespace Matrices::Shared;

namespace FilesMultiplication {

	ref class MatrixSerializer abstract sealed
	{
	public:
		static Matrix2D<long long>^ DeserializeMatrix(int rows, int columns, long long* values);
	private:
		static array<long long>^ ToManagedArray(long long* ptr, int len);
	};
}

