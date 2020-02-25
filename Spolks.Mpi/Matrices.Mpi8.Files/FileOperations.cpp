#include "FileOperations.h"

FilesMultiplication::FileOperations::FileOperations(String^ fileA, String^ fileB, String^ fileResult, int groups)
{
	if (groups <= 0)
		throw gcnew ArithmeticException("groups count should be positive integer");

	this->_groups = groups;
	int rank, size;

	MPI_Comm world = MPI_COMM_WORLD;
	MPI_Comm_rank(world, &rank);
	MPI_Comm_size(world, &size);

	MPI_Group currentGroup = this->CreateCroups(world)[rank];

	MPI_Comm groupCommunicator;
	MPI_Comm_create(world, currentGroup, &groupCommunicator);

	String^ groupFileResult = "_" + Math::Abs(currentGroup).ToString() + "_" + const_cast<String^>(FILE_RESULT_NAME);

	this->_fileA = fileA + const_cast<String^>(FILE_A_NAME);
	this->_fileB = fileB + const_cast<String^>(FILE_B_NAME);
	this->_fileResult = fileResult + groupFileResult;

	this->_fileResult = groupFileResult;
	this->_groupCommunicator = groupCommunicator;
}

void FilesMultiplication::FileOperations::Fill(
	Func<Matrix2D<long long>^>^ fillerA, Func<Matrix2D<long long>^>^ fillerB)
{
	int rank;
	MPI_Comm_rank(this->_groupCommunicator, &rank);

	auto matrixA = fillerA();
	auto matrixB = fillerB();

	WriteMatrix(matrixA, ToCString(this->_fileA));
	WriteMatrix(matrixB, ToCString(this->_fileB));

	MPI_Barrier(this->_groupCommunicator);
}

void FilesMultiplication::FileOperations::Multiply()
{
	Matrix2D<long long>^ matrixA = ReadMatrix(ToCString(this->_fileA), this->_groupCommunicator);
	Matrix2D<long long>^ matrixB = ReadMatrix(ToCString(this->_fileB), this->_groupCommunicator);

	MPI_Barrier(this->_groupCommunicator);
	MultiplyToFile(matrixA, matrixB, this->_groupCommunicator);
}

bool FilesMultiplication::FileOperations::Compare()
{
	int rank, size;
	MPI_Comm_size(this->_groupCommunicator, &size);
	MPI_Comm_rank(this->_groupCommunicator, &rank);

	Matrix2D<long long>^ matrixA = ReadMatrix(ToCString(this->_fileA), this->_groupCommunicator);
	Matrix2D<long long>^ matrixB = ReadMatrix(ToCString(this->_fileB), this->_groupCommunicator);

	Matrix2D<long long>^ expected = MatrixService::MultiplyBy(matrixA, matrixB);
	Matrix2D<long long>^ actual = ReadMatrix(ToCString(this->_fileResult), this->_groupCommunicator);

	return MatrixService::CompareMatrices(expected, actual);
}

void FilesMultiplication::FileOperations::MultiplyToFile(
	Matrix2D<long long>^ matrixA, Matrix2D<long long>^ matrixB, MPI_Comm communicator)
{
	int rank, size;

	MPI_Comm_rank(this->_groupCommunicator, &rank);
	MPI_Comm_size(this->_groupCommunicator, &size);

	int resultSize = matrixA->Rows * matrixA->Columns;
	array<int>^ counts = Arrays::equalPartLengths(resultSize, size);
	ValueTuple<int, int> frameRange = Arrays::getPartIndicesRange(counts, rank);

	array<long long>^ localResult = Enumerable::ToArray(MatrixDivisionService
		::MultiplyFrame(frameRange.Item1, frameRange.Item2, matrixB->Columns, matrixA, matrixB));

	MPI_Barrier(this->_groupCommunicator);
	WriteFrame(localResult, matrixA->Rows, matrixB->Columns, frameRange.Item1);
}

void FilesMultiplication::FileOperations::WriteFrame(array<long long>^ frame, int rows, int columns, int start)
{
	int rank;
	MPI_Comm_rank(this->_groupCommunicator, &rank);

	MPI_File file;
	pin_ptr<const char> fileResult_cstr = ToCString(this->_fileResult);
	int err = MPI_File_open(this->_groupCommunicator, fileResult_cstr,
		MPI_MODE_CREATE | MPI_MODE_RDWR, MPI_INFO_NULL, &file);

	if (rank == MASTER_RANK) {
		MPI_File_write_at(file, 0, &rows, 1, MPI_INT, MPI_STATUSES_IGNORE);
		MPI_File_write_at(file, sizeof(int), &columns, 1, MPI_INT, MPI_STATUSES_IGNORE);
	}

	const long long* localResult_ptr = ToCArray(frame);
	int length = frame->Length;

	MPI_File_write_at_all(file, start * sizeof(long long) + 2 * sizeof(int),
		localResult_ptr, length, MPI_LONG_LONG, MPI_STATUSES_IGNORE);

	delete[] localResult_ptr;
	MPI_File_close(&file);
}

Matrix2D<long long>^ FilesMultiplication::FileOperations::ReadMatrix(const char* filePath, MPI_Comm communicator)
{
	MPI_File file;
	int err = MPI_File_open(communicator, filePath, MPI_MODE_RDONLY, MPI_INFO_NULL, &file);

	if (err) {
		MPI_Abort(communicator, MPI_ERR_BAD_FILE);
		throw gcnew SystemException("cannot open file " + gcnew String(filePath));
	}

	void* singleBuffer = new int[1];
	MPI_File_read_at(file, 0, singleBuffer, 1, MPI_INT, MPI_STATUSES_IGNORE);
	int* rows_ptr = reinterpret_cast<int*>(singleBuffer);

	MPI_File_read_at(file, sizeof(int), singleBuffer, 1, MPI_INT, MPI_STATUSES_IGNORE);
	int* columns_ptr = reinterpret_cast<int*>(singleBuffer);

	int rows = *rows_ptr;
	int columns = *columns_ptr;
	int length = rows * columns;

	long long* buffer = new long long[length];
	int code = MPI_File_read_at(file, 2 * sizeof(int), static_cast<void*>(buffer), length, MPI_LONG_LONG, MPI_STATUSES_IGNORE);
	MPI_File_close(&file);

	Matrix2D<long long>^ matrix = MatrixSerializer::DeserializeMatrix(rows, columns, buffer);

	delete[] buffer;
	delete[] singleBuffer;
	return matrix;
}

void FilesMultiplication::FileOperations::WriteMatrix(Matrix2D<long long>^ matrix, const char* filePath)
{
	MPI_File file;
	MPI_File_open(this->_groupCommunicator, filePath, MPI_MODE_WRONLY | MPI_MODE_CREATE, MPI_INFO_NULL, &file);

	int rows = matrix->Rows;
	int columns = matrix->Columns;

	int* rows_ptr = &rows;
	int* columns_ptr = &columns;

	MPI_File_write_at(file, 0, static_cast<void*>(rows_ptr), 1, MPI_INT, MPI_STATUSES_IGNORE);
	MPI_File_write_at(file, sizeof(int), static_cast<void*>(columns_ptr), 1, MPI_INT, MPI_STATUSES_IGNORE);

	array<long long>^ matrixArray = Enumerable::ToArray(matrix);
	const long long* matrix_ptr = ToCArray(matrixArray);

	MPI_File_write_at(file, 2 * sizeof(int), matrix_ptr, matrix->Size, MPI_LONG_LONG, MPI_STATUSES_IGNORE);

	MPI_File_close(&file);
}

const char* FilesMultiplication::FileOperations::ToCString(String^ str)
{
	marshal_context^ cstringMarshal = gcnew marshal_context();

	return cstringMarshal->marshal_as<const char*>(str);
}

const long long* FilesMultiplication::FileOperations::ToCArray(array<long long>^ arr)
{
	long long* result = new long long[arr->Length];

	for (int i = 0; i < arr->Length; i++) {
		result[i] = arr[i];
	}

	return result;
}

const int* FilesMultiplication::FileOperations::ToCArray(array<int>^ arr)
{
	int* result = new int[arr->Length];

	for (int i = 0; i < arr->Length; i++) {
		result[i] = arr[i];
	}

	return result;
}

Dictionary<int, MPI_Group>^ FilesMultiplication::FileOperations::CreateCroups(MPI_Comm communicator)
{
	int size;
	MPI_Comm_size(communicator, &size);

	auto groupsRanks = ArrayExtensions::SplitToDictionary(Enumerable::ToArray(Enumerable::Range(0, size)), this->_groups);
	auto groupsPerRanks = gcnew Dictionary<int, MPI_Comm>();

	for each (auto groupRanks in groupsRanks) {
		MPI_Group newGroup;
		MPI_Group baseGroup;

		const int* ranks_ptr = ToCArray(groupRanks.Value);

		MPI_Comm_group(communicator, &baseGroup);
		MPI_Group_incl(baseGroup, groupRanks.Value->Length, ranks_ptr, &newGroup);

		for each (auto rank in groupRanks.Value) {
			groupsPerRanks->Add(rank, newGroup);
		}
	}

	return groupsPerRanks;
}
