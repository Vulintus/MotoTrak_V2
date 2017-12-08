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
        /// Converts a .NET DateTime object into a 64-bit floating-point value that
        /// is the same format that Matlab uses to represents dates and times.
        /// </summary>
        /// <param name="date_time">The .NET DateTime object</param>
        /// <returns>Matlab datecode</returns>
        public static double ConvertDateTimeToMatlabDatenum ( DateTime date_time )
        {
            double result = double.NaN;

            //Get the ticks from the DateTime object
            long ticks = date_time.Ticks;
            long a = Convert.ToInt64(Math.Pow(10, 7)) * 60 * 60 * 24;
            long offset = -367 * Convert.ToInt64(Math.Pow(10, 7)) * 60 * 60 * 24;
            
            result = Convert.ToDouble(ticks - offset) / a;
            
            return result;
        }

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
        /// Calculates the Matlab diff function of a list of integers
        /// </summary>
        /// <param name="a"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static List<int> DiffInt (List<int> a, int startIndex, int count)
        {
            List<int> b = new List<int>();
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
        /// Calculates the Matlab diff function of a list of integers
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        public static List<int> DiffInt (List<int> a)
        {
            return DiffInt(a, 0, a.Count);
        }

        /// <summary>
        /// Smooths a signal using a simple running average. The smoothing factor defines how many elements before and after each element to include in the running average.
        /// This function handles end-cases by duplicating the current element N times, where N is the smoothing factor.
        /// </summary>
        /// <param name="smoothing_factor"></param>
        /// <returns></returns>
        public static List<double> SmoothSignal (List<double> signal, int smoothing_factor = 3)
        {
            List<double> result_signal = new List<double>();

            if (signal != null && signal.Count > 0)
            {
                //Create a small list of elements to be appended onto the beginning and end of the list
                List<double> signal_ends = Enumerable.Repeat(signal[0], smoothing_factor).ToList();

                //Make a copy of the signal
                List<double> temp_signal = new List<double>();
                temp_signal.AddRange(signal_ends);
                temp_signal.AddRange(signal);
                temp_signal.AddRange(signal_ends);

                //Smooth the list
                for (int i = 0; i < signal.Count; i++)
                {
                    int total_elements_to_take = (smoothing_factor * 2) + 1;
                    var avg_of_elements = temp_signal.Skip(i).Take(total_elements_to_take).Average();
                    result_signal.Add(avg_of_elements);
                }
            }

            return result_signal;
        }

        public static List<Tuple<double, double>> FindPeaksAboveInitiationThreshold (List<double> v, double init_thresh)
        {
            List<Tuple<double, double>> peaks = new List<Tuple<double, double>>();

            var binary_signal = v.Select(x => (x >= init_thresh) ? 1 : 0).ToList();
            var diff_binary_signal = MotorMath.DiffInt(binary_signal);

            //Get all points where the signal rose above the initiation threshold and where it fell below
            var rises = Enumerable.Range(0, diff_binary_signal.Count).Where(x => diff_binary_signal[x] == 1).ToList();
            var falls = Enumerable.Range(0, diff_binary_signal.Count).Where(x => diff_binary_signal[x] == -1).ToList();

            //If there is a fall before the first rise, then insert a rise at the very beginning
            if (falls.Count > 0 && rises.Count > 0)
            {
                var first_rise = rises.FirstOrDefault();
                var first_fall = falls.FirstOrDefault();

                var last_rise = rises.LastOrDefault();
                var last_fall = falls.LastOrDefault();

                if (first_rise >= first_fall)
                {
                    rises.Insert(0, 0);
                }

                if (last_rise >= last_fall)
                {
                    falls.Add(diff_binary_signal.Count - 1);
                }
            }
            else if (falls.Count > 0 && rises.Count == 0)
            {
                rises.Add(0);
            }
            else if (falls.Count == 0 && rises.Count > 0)
            {
                falls.Add(diff_binary_signal.Count - 1);
            }
            
            //Get elements of the signal between each rise and fall
            for (int i = 0; i < rises.Count && i < falls.Count; i++)
            {
                var r = rises[i];
                var f = falls[i];

                var s = v.Where((y, x) => x >= r && x < f).Select(x => x).ToList();
                var s_max = s.Max();
                var s_max_idx = s.IndexOf(s_max);
                var real_index = r + s_max_idx;
                Tuple<double, double> new_peak = new Tuple<double, double>(s_max, real_index);

                peaks.Add(new_peak);
            }

            return peaks;
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
        /// Finds the standard deviation of all values within a list
        /// </summary>
        public static double StdDev (List<double> values)
        {
            double ret = double.NaN;

            if (values.Count > 0)
            {
                //Compute the Average      
                double avg = values.Average();

                //Perform the Sum of (value-avg)_2_2      
                double sum = values.Sum(d => Math.Pow(d - avg, 2));

                //Put it all together      
                ret = Math.Sqrt((sum) / (values.Count - 1));
            }

            return ret;
        }

        /// <summary>
        /// Finds the deviation of all values around a specified "mean".
        /// </summary>
        /// <param name="values">A list of values</param>
        /// <param name="mean">The mean</param>
        /// <returns>The standard deviation of the list of values around the specified mean</returns>
        public static double StdDevAroundMean (List<double> values, double mean)
        {
            double ret = double.NaN;

            if (values.Count > 0)
            {
                //Perform the Sum of (value-avg)_2_2      
                double sum = values.Sum(d => Math.Pow(d - mean, 2));

                //Put it all together      
                ret = Math.Sqrt((sum) / (values.Count - 1));
            }

            return ret;
        }

        /// <summary>
        /// A function which finds the median of an array of numbers.
        /// </summary>
        /// <param name="numbers">An array of numbers</param>
        /// <returns>The median of the numbers in the array</returns>
        public static double Median(List<double> numbers)
        {
            if (numbers.Count == 0)
                return double.NaN;

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
        /// Calculate the Nth percentile of a sequence of numbers
        /// </summary>
        /// <param name="sequence">The sequence of numbers</param>
        /// <param name="excelPercentile">Desired percentile between 0 and 1</param>
        /// <returns>The number at the Nth percentile</returns>
        public static double Percentile(double[] sequence, double excelPercentile)
        {
            Array.Sort(sequence);
            int N = sequence.Length;
            double n = (N - 1) * excelPercentile + 1;
            // Another method: double n = (N + 1) * excelPercentile;
            if (n == 1d) return sequence[0];
            else if (n == N) return sequence[N - 1];
            else
            {
                int k = (int)n;
                double d = n - k;
                return sequence[k - 1] + d * (sequence[k] - sequence[k - 1]);
            }
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
        
        /// <summary>
        /// Calculates the minimum number in an array, excluding NaN values.
        /// </summary>
        /// <param name="t">The array of numbers</param>
        /// <returns>The minimum number found in the array</returns>
        public static double NanMin ( List<double> t )
        {
            return t.Where(x => !Double.IsNaN(x)).Min();
        }

        /// <summary>
        /// Calculates the maximum number in an array, excluding NaN values.
        /// </summary>
        /// <param name="t">The array of numbers</param>
        /// <returns>The maximum number found in the array</returns>
        public static double NanMax ( List<double> t )
        {
            return t.Where(x => !Double.IsNaN(x)).Max();
        }

        /// <summary>
        /// Calculates the absolute value of every element in a list, and returns the result.
        /// </summary>
        /// <param name="t">A list of doubles</param>
        /// <returns>A list of doubles, but all values are the absolute value of their respective input value</returns>
        public static List<double> AbsList ( List<double> t )
        {
            return t.Select(x => Math.Abs(x)).ToList();
        }

        /// <summary>
        /// Substracts a scalar value from all elements in a list, and returns the result.
        /// </summary>
        /// <param name="a">A list of doubles</param>
        /// <param name="b">The value to subtract</param>
        /// <returns>A list of doubles, with all elements equal to their original value minus the value of b</returns>
        public static List<double> SubtractScalarFromList ( List<double> a, double b )
        {
            return a.Select(x => x - b).ToList();
        }
    }
}
