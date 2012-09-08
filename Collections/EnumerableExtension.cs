using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonCore.Collections
{
	public static class EnumerableExtension
	{
		public static void CopyToArray<T>(this IEnumerable<T> enumerable, T[] destinationArray, int startIndex)
		{
			int i = startIndex;
			foreach (T value in enumerable)
			{
				// We don't check for out-of-bounds before-hand as the input is an enumerable.  We'll
				// just let array throw the exception
				destinationArray[i++] = value;
			}
		}
	}
}
