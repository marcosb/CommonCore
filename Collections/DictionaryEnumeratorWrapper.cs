using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonCore.Collections
{
	/// <summary>
	/// Converts a <see cref="System.Collections.Generic.Collection{T}"/>
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	public class DictionaryEnumeratorWrapper<TKey, TValue> : IDictionaryEnumerator
	{
		private readonly IEnumerator<KeyValuePair<TKey, TValue>> _Source;

		internal DictionaryEnumeratorWrapper(IEnumerable<KeyValuePair<TKey, TValue>> source)
		{
			_Source = source.GetEnumerator();
		}

		#region IDictionaryEnumerator Members

		public DictionaryEntry Entry
		{
			get { return new DictionaryEntry(_Source.Current.Key, _Source.Current.Value); }
		}

		public object Key
		{
			get { return _Source.Current.Key; }
		}

		public object Value
		{
			get { return _Source.Current.Value; }
		}

		#endregion

		#region IEnumerator Members

		public object Current
		{
			get { return _Source.Current; }
		}

		public bool MoveNext()
		{
			return _Source.MoveNext();
		}

		public void Reset()
		{
			_Source.Reset();
		}

		#endregion
	}
}
