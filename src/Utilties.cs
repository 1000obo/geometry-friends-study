using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace GeometryFriendsAgents
{
    public class Utilities
    {
        public enum numTrue
        {
            MORETRUE, LESSTRUE, DIFFERENTTRUE, SAMETRUE
        };

        public static numTrue CompTrueNum(bool[] matrix1, bool[] matrix2)
        {
            bool[] orMatrix = GetOrMatrix(matrix1, matrix2);

            bool sameFlag1 = IsSameMatrix(orMatrix, matrix1);
            bool sameFlag2 = IsSameMatrix(orMatrix, matrix2);

            if (sameFlag1)
            {
                if (sameFlag2)
                {
                   return numTrue.SAMETRUE;
                }

                return numTrue.MORETRUE;
            }
            else
            {
                if (sameFlag2)
                {
                    return numTrue.LESSTRUE;
                }

                return numTrue.DIFFERENTTRUE;
            }
        }

        public static bool[] GetOrMatrix(bool[] matrix1, bool[] matrix2)
        {
            bool[] returnMatrix = new bool[matrix1.Length];

            if(matrix1.Length == matrix2.Length)
            {
                for(int i = 0; i < matrix1.Length; i++)
                {
                    returnMatrix[i] = matrix1[i] | matrix2[i];
                }
            }

            return returnMatrix;
        }

        public static bool[] GetXorMatrix(bool[] matrix1, bool[] matrix2)
        {
            bool[] returnMatrix = new bool[matrix1.Length];

            if (matrix1.Length == matrix2.Length)
            {
                for (int i = 0; i < matrix1.Length; i++)
                {
                    returnMatrix[i] = matrix1[i] ^ matrix2[i];
                }
            }

            return returnMatrix;
        }

        private static bool IsSameMatrix(bool[] matrix1, bool[] matrix2)
        {
            for (int i = 0; i < matrix1.Length; i++)
            {
                if (matrix1[i] ^ matrix2[i])
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsTrueValue_inMatrix(bool[] matrix)
        {
            foreach(bool i in matrix)
            {
                if(i)
                {
                    return true;
                }
            }

            return false;
        }

        public static float[,] ReadCsvFile(int row, int column, string fileName)
        {
            float[,] matrix = new float[row, column];

            try
            {
                using (var sr = new System.IO.StreamReader(fileName))
                {
                    for (int i = 0; i < row; i++)
                    {
                        var line = sr.ReadLine();
                        var values = line.Split(',');

                        int j = 0;
                        foreach (var value in values)
                        {
                            if (j < column)
                            {
                                matrix[i, j] = float.Parse(value, CultureInfo.InvariantCulture);
                                j++;
                            }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.WriteLine("Please locate Qmap.csv at the same folder as GeometryFriends.exe");
                Debug.WriteLine(e.Message);
                Environment.Exit(0);
            }

            return matrix;
        }

    }
}
