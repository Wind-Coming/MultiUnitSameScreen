using System;
using System.Collections.Generic;

namespace Unity.Collections
{
    /// <summary>
    /// List extensions.
    /// </summary>
    public static class ListExtensions
    {
        /// <summary>
        /// Truncates the list by replacing the item at the specified index with the last item in the list. The list
        /// is shortened by one.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="list">List to perform removal.</param>
        /// <param name="item">Item value to remove.</param>
        /// <returns>Returns true if item is removed, if item was not in the container returns false.</returns>
        public static bool RemoveSwapBack<T>(this List<T> list, T item)
        {
            int index = list.IndexOf(item);
            if (index < 0)
                return false;

            RemoveAtSwapBack(list, index);
            return true;
        }

        /// <summary>
        /// Truncates the list by replacing the item at the specified index with the last item in the list. The list
        /// is shortened by one.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="list">List to perform removal.</param>
        /// <param name="matcher"></param>
        /// <returns>Returns true if item is removed, if item was not in the container returns false.</returns>
        public static bool RemoveSwapBack<T>(this List<T> list, Predicate<T> matcher)
        {
            int index = list.FindIndex(matcher);
            if (index < 0)
                return false;

            RemoveAtSwapBack(list, index);
            return true;
        }

        /// <summary>
        /// Truncates the list by replacing the item at the specified index with the last item in the list. The list
        /// is shortened by one.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="list">List to perform removal.</param>
        /// <param name="index">The index of the item to delete.</param>
        public static void RemoveAtSwapBack<T>(this List<T> list, int index)
        {
            int lastIndex = list.Count - 1;
            list[index] = list[lastIndex];
            list.RemoveAt(lastIndex);
        }

        /// <summary>
        /// Convert System.Collections.Generics.List to Unity.Collections.NativeList.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="list">List to perform conversion.</param>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        /// <returns>A NativeList copy of the List.</returns>
        public static NativeList<T> ToNativeList<T>(this List<T> list, Allocator allocator) where T : unmanaged
        {
            var container = new NativeList<T>(list.Count, allocator);
            for (int i = 0; i < list.Count; i++)
            {
                container.AddNoResize(list[i]);
            }
            return container;
        }

        /// <summary>
        /// Convert System.Collections.Generics.List to Unity.Collections.NativeArray.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="list">List to perform conversion.</param>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        /// <returns>A NativeArray copy of the List.</returns>
        public unsafe static NativeArray<T> ToNativeArray<T>(this List<T> list, Allocator allocator) where T : unmanaged
        {
            var container = new NativeArray<T>(list.Count, allocator);
            for (int i = 0; i < list.Count; i++)
            {
                container[i] = list[i];
            }
            return container;
        }
    }
}
