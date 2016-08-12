using System;
using System.Collections.Generic;
using System.Linq;

namespace MotoTrakUtilities
{
    /// <summary>
    /// This class provides some basic math functions that are useful for MotoTrak.
    /// </summary>
    public static class MotorMath
    {
        /// <summary>
        /// This function converts a Matlab datenum to a C#/.NET DateTime.
        /// </summary>
        /// <param name="datenum">Matlab datenum</param>
        /// <returns>C# DateTime</returns>
        public static DateTime ConvertMatlabDatenumToDateTime ( double datenum )
        {
            DateTime result = DateTime.MinValue;

            long a = Convert.ToInt64(Math.Pow(10, 7)) * 60 * 60 * 24;
            long offset = -367 * Convert.ToInt64(Math.Pow(10, 7)) * 60 * 60 * 24;
            long ticks = Convert.ToInt64(a * datenum) + offset;

            result = new DateTime(ticks);

            return result;
        }

        /// <summary>
        /// Calculates the iterative mean of a set of numbers
        /// </summary>
        /// <param name="previous_mean">The previous mean of all the numbers consumed so far</param>
        /// <param name="new_sample">A new number to add to the set of numbers</param>
        /// <param name="new_count">The new count of the total numbers in the set</param>
        /// <returns>The iterative mean</returns>
        public static double IterativeMean (double previous_mean, double new_sample, int new_count)
        {
            double count = Convert.ToDouble(new_count);
            double result = (((count - 1) / count) * previous_mean) + ((1 / count) * new_sample);
            return result;
        }

        /// <summary>
        /// Establishes the first element in the signal as "zero", and then offsets all other elements from that value.
        /// </summary>
        /// <param name="a">The signal to be offset.</param>
        /// <returns>The offset signal, where the first element is zero.  All other elements are offset from zero.</returns>
        public static List<double> OffsetFromFirstElement(List<double> a)
        {
            List<double> b = new List<double>();

            for (int i = 0; i < a.Count; i++)
            {
                b.Add(a[i] - a[0]);
            }

            return b;
        }

        /// <summary>
        /// Finds the "derivative" of a signal by calculating the difference between each element.
        /// </summary>
        /// <param name="a">The signal as a list of a doubles.</param>
        /// <returns>The derivative of the input signal.</returns>
        public static List<double> Diff(List<double> a)
        {
            return MotorMath.Diff(a, 0, a.Count);
        }

        /// <summary>
        /// Finds the "derivative" of a signal by calculating the difference between each element.
        /// </summary>
        /// <param name="a">The signal as a list of a doubles.</param>
        /// <param name="startIndex">The index at which to start calculating the derivative.</param>
        /// <param name="count">How many elements to use in the calculation.</param>
        /// <returns>The derivative of the segment being used within the signal.</returns>
        public static List<double> Diff(List<double> a, int startIndex, int count)
        {
            List<double> b = new List<double>();
            for (int i = 0; i < a.Count; i++)
            {
                if ((i < startIndex) || (i > (startIndex + count)))
                {
                    b.Add(a[i]);
                }
                else if (i <= (startIndex + count))
                {
                    if ((i + 1) < a.Count)
                    {
                        b.Add(a[i + 1] - a[i]);
                    }
                    else
                    {
                        //This is so we come out with the same amount of elements as we came in with
                        b.Add(b.LastOrDefault());
                    }
                }
            }

            return b;
        }

        /// <summary>
        /// Finds the position of local maxima through a signal.
        /// </summary>
        /// <param name="v">The signal to be analyzed, as a list of doubles.</param>
        /// <returns>A list of tuples.  Each tuple represents a peak found in the signal.  The first value of the tuple is the position of the peak
        /// within the signal.  The second value of the tuple is the magnitude of the peak.</returns>
        public static List<Tuple<double, double>> FindPeaks(List<double> v)
        {
            List<Tuple<double, double>> peaks = new List<Tuple<double, double>>();

            double mn = double.PositiveInfinity;
            double mx = double.NegativeInfinity;
            double mnpos = double.NaN;
            double mxpos = double.NaN;

            bool lookformax = true;

            for (int i = 0; i < v.Count; i++)
            {
                double element = v[i];
                if (element > mx)
                {
                    mx = element;
                    mxpos = i;
                }
                if (element < mn)
                {
                    mn = element;
                    mnpos = i;
                }

                if (lookformax)
                {
                    if (element < mx)
                    {
                        peaks.Add(new Tuple<double, double>(mx, mxpos));
                        mn = element;
                        mnpos = i;
                        lookformax = false;
                    }
                }
                else
                {
                    if (element > mn)
                    {
                        mx = element;
                        mxpos = i;
                        lookformax = true;
                    }
                }
            }

            return peaks;
        }
        
        /// <summary>
        /// A function which finds the median of an array of numbers.
        /// </summary>
        /// <param name="numbers">An array of numbers</param>
        /// <returns>The median of the numbers in the array</returns>
        public static double Median(List<double> numbers)
        {
            int numberCount = numbers.Count();
            int halfIndex = numbers.Count() / 2;
            var sortedNumbers = numbers.OrderBy(n => n);
            double median;
            if ((numberCount % 2) == 0)
            {
                int halfIndexMinus1 = halfIndex - 1;
                median = (sortedNumbers.ElementAt(halfIndex) + sortedNumbers.ElementAt(halfIndexMinus1)) / 2;
            }
            else
            {
                median = sortedNumbers.ElementAt(halfIndex);
            }

            return median;
        }

        /// <summary>
        /// Transposes a matrix (in this case a List of List<T>).
        /// Example:
        /// The following: [ [a1,b1,c1] [a2,b2,c2] [a3,b3,c3] ]
        /// Becomes: [ [a1,a2,a3] [b1,b2,b3] [c1,c2,c3] ]
        /// </summary>
        /// <typeparam name="T">Any type</typeparam>
        /// <param name="lists">A list of lists</param>
        /// <returns>The transposed list of lists</returns>
        public static List<List<T>> Transpose<T> (List<List<T>> lists)
        {
            var longest = lists.Any() ? lists.Max(l => l.Count) : 0;
            List<List<T>> outer = new List<List<T>>(longest);
            for (int i = 0; i < longest; i++)
            {
                outer.Add(new List<T>(lists.Count));
            }

            for (int j = 0; j < lists.Count; j++)
            {
                for (int i = 0; i < longest; i++)
                {
                    outer[i].Add(lists[j].Count > i ? lists[j][i] : default(T));
                }
            }

            return outer;
        }
    }
}
