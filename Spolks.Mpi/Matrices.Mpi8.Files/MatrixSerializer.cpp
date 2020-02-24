#include "MatrixSerializer.h"

Matrix2D<long long>^ FilesMultiplication::MatrixSerializer::DeserializeMatrix(int rows, int columns, long long* values)
{
	int len = rows * columns;
	array<long long>^ managedValues = ToManagedArray(values, len);

	return Matrix2D<long long>::FromArray(managedValues, rows, columns);
}

array<long long>^ FilesMultiplication::MatrixSerializer::ToManagedArray(long long* ptr, int len)
{
	auto result = gcnew List<long long>();
	for (int i = 0; i < len; i++) {
		result->Add(*(ptr + i));
	}

	return result->ToArray();
}
