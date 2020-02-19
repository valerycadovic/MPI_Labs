using System;
using System.Collections;
using System.Collections.Generic;

namespace Matrices.Shared
{
    public class Matrix2D<T> : IEnumerable<T>
    {
        private readonly T[,] _matrix;

        public int Rows { get; }

        public int Columns { get; }

        public int Size { get; }

        private Matrix2D(T[,] matrix)
        {
            _matrix = matrix ?? throw new ArgumentNullException($"{nameof(matrix)} cannot be null");

            Rows = _matrix.GetLength(0);
            Columns = _matrix.GetLength(1);
            Size = Rows * Columns;
        }

        public static Matrix2D<T> CreateEmpty(int rows, int columns)
        {
            if (rows <= 0 || columns <= 0)
            {
                throw new ArithmeticException("all dimensions should be positive values");
            }

            var matrix = new T[rows, columns];
            return FromArray(matrix);
        }

        public static Matrix2D<T> FromArray(T[,] array)
        {
            return new Matrix2D<T>((T[,])array.Clone());
        }

        public IEnumerable<T> GetRow(int row)
        {
            for (int i = 0; i < Columns; i++)
            {
                yield return _matrix[row, i];
            }
        }

        public IEnumerable<T> GetColumn(int column)
        {
            for (int i = 0; i < Rows; i++)
            {
                yield return _matrix[i, column];
            }
        }

        public IEnumerable<IEnumerable<T>> GetRows()
        {
            for (int i = 0; i < this.Rows; i++)
            {
                yield return this.GetRow(i);
            }
        }

        public IEnumerable<IEnumerable<T>> GetColumns()
        {
            for (int i = 0; i < this.Columns; i++)
            {
                yield return this.GetColumn(i);
            }
        }

        public T this[int row, int column]
        {
            get => _matrix[row, column];
            set => _matrix[row, column] = value;
        }

        public T this[int absoluteIndex]
        {
            get => _matrix[absoluteIndex / this.Columns, absoluteIndex % this.Columns];
            set => _matrix[absoluteIndex / this.Columns, absoluteIndex % this.Columns] = value;
        }

        public Matrix2D<T> Clone()
        {
            return FromArray(_matrix);
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < Size; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
