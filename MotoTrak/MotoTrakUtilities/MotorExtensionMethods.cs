using System.Collections.Generic;

namespace MotoTrakUtilities
{
    public static class MotorExtensionMethods
    {
        /// <summary>
        /// Beginning at the index "start" in the destination list, this method replaces elements found in the destination
        /// list with elements found in the source list, until it has replaced "count" elements.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="old"></param>
        /// <param name="new_elements"></param>
        /// <param name="start"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static List<T> ReplaceRange<T>(this List<T> destination, List<T> source, int start, int count)
        {
            if (destination != null && source != null)
            {
                int c = 0;
                for (int i = start; i < destination.Count && c < count; i++, c++)
                {
                    if (c < source.Count)
                    {
                        destination[i] = source[c];
                    }
                }
            }

            return destination;
        }

        /// <summary>
        /// Beginning at the index "start" in the destination list, this method replaces elements found in the destination
        /// list with elements found in the source list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="destination"></param>
        /// <param name="source"></param>
        /// <param name="start"></param>
        /// <returns></returns>
        public static List<T> ReplaceRange<T>(this List<T> destination, List<T> source, int start)
        {
            if (destination != null && source != null)
            {
                int c = 0;
                for (int i = start; i < destination.Count; i++, c++)
                {
                    if (c < source.Count)
                    {
                        destination[i] = source[c];
                    }
                }
            }

            return destination;
        }
    }
}
