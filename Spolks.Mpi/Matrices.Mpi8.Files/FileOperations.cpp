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

	String^ groupFileResult = "_" + currentGroup.ToString() + "_" + fileResult;

	this->FillFileNames(fileA, fileB, groupFileResult);
	this->_groupCommunicator = groupCommunicator;
}

void FilesMultiplication::FileOperations::Multiply()
{
	Matrix2D<long long>^ matrixA = ReadMatrix(this->_fileA_cstr, this->_groupCommunicator);
	Matrix2D<long long>^ matrixB = ReadMatrix(this->_fileB_cstr, this->_groupCommunicator);

	MultiplyToFile(matrixA, matrixB, this->_groupCommunicator);
}

bool FilesMultiplication::FileOperations::Compare()
{
	Matrix2D<long long>^ matrixA = ReadMatrix(this->_fileA_cstr, this->_groupCommunicator);
	Matrix2D<long long>^ matrixB = ReadMatrix(this->_fileB_cstr, this->_groupCommunicator);

	Matrix2D<long long>^ expected = MatrixService::MultiplyBy(matrixA, matrixB);
	Matrix2D<long long>^ actual = ReadMatrix(this->_fileResult_cstr, this->_groupCommunicator);

	return MatrixService::CompareMatrices(expected, actual);
}

void FilesMultiplication::FileOperations::MultiplyToFile(
	Matrix2D<long long>^ matrixA, Matrix2D<long long>^ matrixB, MPI_Comm communicator)
{
	int rank, size;

	MPI_Comm_rank(communicator, &rank);
	MPI_Comm_size(communicator, &size);

	int resultSize = matrixA->Rows * matrixA->Columns;
	array<int>^ counts = Arrays::equalPartLengths(resultSize, this->_groups);
	ValueTuple<int, int>^ frameRange = Arrays::getPartIndicesRange(counts, rank);

	array<long long>^ localResult = Enumerable::ToArray(MatrixDivisionService
		::MultiplyFrame(frameRange->Item1, frameRange->Item2, matrixB->Columns, matrixA, matrixB));
	pin_ptr<const long long> ptr = &localResult[0];
	const long long* localResult_ptr = ptr;

	MPI_File file;
	const int amode = MPI_MODE_CREATE | MPI_MODE_WRONLY;
	MPI_File_open(communicator, this->_fileResult_cstr, amode, MPI_INFO_NULL, &file);

	int rows = matrixA->Rows;
	int columns = matrixB->Rows;

	int* rows_ptr = &rows;
	int* columns_ptr = &columns;

	if (rank == MASTER_RANK) {
		MPI_File_write_ordered(file, static_cast<void*>(rows_ptr), 1, MPI_INT32_T, MPI_STATUSES_IGNORE);
		MPI_File_write_ordered(file, static_cast<void*>(columns_ptr), 1, MPI_INT32_T, MPI_STATUSES_IGNORE);
	}

	MPI_File_write_ordered(file, localResult_ptr, localResult->Length, MPI_LONG_LONG, MPI_STATUS_IGNORE);
}

Matrix2D<long long>^ FilesMultiplication::FileOperations::ReadMatrix(const char* filePath, MPI_Comm communicator)
{
	MPI_File file;
	int err = MPI_File_open(communicator, filePath, MPI_MODE_RDONLY, MPI_INFO_NULL, &file);

	if (err) {
		MPI_Abort(communicator, MPI_ERR_BAD_FILE);
		throw gcnew SystemException("cannot open file " + gcnew String(filePath));
	}

	void* singleBuffer;
	MPI_File_read_at_all(file, 0, singleBuffer, 1, MPI_INT32_T, MPI_STATUSES_IGNORE);
	int* rows_ptr = reinterpret_cast<int*>(singleBuffer);

	MPI_File_read_at_all(file, 1, singleBuffer, 1, MPI_INT32_T, MPI_STATUSES_IGNORE);
	int* columns_ptr = reinterpret_cast<int*>(singleBuffer);

	int rows = *rows_ptr;
	int columns = *columns_ptr;
	int length = rows * columns;

	long long* buffer;
	MPI_File_read_at_all(file, 2, buffer, length, MPI_LONG_LONG, MPI_STATUSES_IGNORE);

	Matrix2D<long long>^ matrix = MatrixSerializer::DeserializeMatrix(rows, columns, buffer);

	return matrix;
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

		pin_ptr<int> ranks_pin_ptr = &groupRanks.Value[0];
		int* ranks_ptr = ranks_pin_ptr;

		MPI_Comm_group(communicator, &baseGroup);
		MPI_Group_incl(baseGroup, groupRanks.Value->Length, ranks_pin_ptr, &newGroup);

		for each (auto rank in groupRanks.Value) {
			groupsPerRanks->Add(rank, newGroup);
		}
	}

	return groupsPerRanks;
}

void FilesMultiplication::FileOperations::FillFileNames(String^ fileA, String^ fileB, String^ fileResult)
{
	marshal_context^ cstringMarshal = gcnew marshal_context();
	this->_fileA_cstr = cstringMarshal->marshal_as<const char*>(fileA);
	this->_fileB_cstr = cstringMarshal->marshal_as<const char*>(fileB);
	this->_fileResult_cstr = cstringMarshal->marshal_as<const char*>(fileResult);
}
