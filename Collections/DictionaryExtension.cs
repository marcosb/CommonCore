using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonCore.Collections
{
	public static class DictionaryExtension
	{
		public static IDictionaryEnumerator AsIDictionaryEnumerator<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source)
		{
			return new DictionaryEnumeratorWrapper<TKey, TValue>(source);
		}
	}
}
