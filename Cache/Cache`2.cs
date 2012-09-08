using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;

using CommonCore.Collections;

namespace CommonCore.Cache
{
	/**
	 * A simple thread-safe cache which maps a key of type TKey to a value of type TValue.
	 */
	public class Cache<TKey, TValue> : IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>, IDictionary
	{
		public readonly TimeSpan? EntryExpiration;
		public readonly long MaxEntries;
		// TODO: This works for now, but the ideal solution is to split by segments, with each segment having a dictionary & queue.
		private readonly ConcurrentDictionary<TKey, EntryData> _Entries = new ConcurrentDictionary<TKey, EntryData>();
		private ConcurrentQueue<KeyValuePair<TKey, EntryData>> _LastWrite = new ConcurrentQueue<KeyValuePair<TKey, EntryData>>();

		#region IDictionary<TKey, TValue> Members
		public ICollection<TKey> Keys
		{
			get { return new KeyCollection(this); }
		}

		public ICollection<TValue> Values
		{
			get { return this.Transform(kvp => kvp.Value); }
		}

		public TValue this[TKey key]
		{
			get
			{
				TValue value;
				if (TryGetValue(key, out value))
					return value;
				else
					throw new KeyNotFoundException();
			}
			set
			{
				var newEntry = GetEntry(value);
				var entry = _Entries.AddOrUpdate(key, (k) => newEntry, (k, oldData) => newEntry);

				if (! Object.ReferenceEquals(entry, newEntry) && (entry != null))
					entry.SetRemoved();
				TrackWrite(newEntry, key);
			}
		}

		public void Add(TKey key, TValue value)
		{
			if (!_Entries.TryAdd(key, GetEntry(value)))
				throw new ArgumentException();

			if (_Entries.Count > MaxEntries)
				Clean();
		}

		public bool TryAdd(TKey key, TValue value)
		{
			bool result = _Entries.TryAdd(key, GetEntry(value));

			if ((!result) && (_Entries.Count > MaxEntries))
				Clean();

			return result;
		}

		public bool ContainsKey(TKey key)
		{
			return _Entries.ContainsKey(key);
		}

		public bool Remove(TKey key)
		{
			TValue value;
			return TryRemove(key, out value);
		}

		public bool TryRemove(TKey key, out TValue value)
		{
			EntryData entryData;
			bool result = _Entries.TryRemove(key, out entryData);
			if (result)
			{
				value = entryData.Data;
				if (entryData.Expired)
					result = false;
				entryData.SetRemoved();
			}
			else
				value = default(TValue);

			return result;
		}

		public bool TryGetValue(TKey key, out TValue value)
		{
			EntryData entryData;
			bool result = _Entries.TryGetValue(key, out entryData);
			if (result && !entryData.Expired)
			{
				value = entryData.Data;
				entryData.UtcLastUsed = DateTime.UtcNow;
			}
			else
				value = default(TValue);

			return result;
		}
		#endregion

		#region ICollection<KeyValuePair<TKey,TValue>> Members

		public void Add(KeyValuePair<TKey, TValue> item)
		{
			Add(item.Key, item.Value);
		}

		public void Clear()
		{
			_Entries.Clear();
			_LastWrite = new ConcurrentQueue<KeyValuePair<TKey, EntryData>>();
		}

		public bool Contains(KeyValuePair<TKey, TValue> item)
		{
			TValue data;
			if (TryGetValue(item.Key, out data))
				return Object.Equals(item.Value, data);
			else
				return false;
		}

		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			int i = 0;
			foreach (var entry in this)
			{
				array[arrayIndex + i++] = entry;
			}
		}

		public int Count
		{
			get { return _Entries.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool Remove(KeyValuePair<TKey, TValue> item)
		{
			EntryData entryData;
			return _Entries.TryGetValue(item.Key, out entryData)
				&& (Object.Equals(entryData.Data, item.Value))
				&& EntriesAsCollection.Remove(new KeyValuePair<TKey, EntryData>(item.Key, entryData));
		}

		#endregion

		#region IEnumerable<KeyValuePair<TKey,TValue>> Members

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			return EntriesAsCollection
				.Where(e => !e.Value.Expired)
				.Select(e => new KeyValuePair<TKey, TValue>(e.Key, e.Value.Data))
				.GetEnumerator();
		}

		#endregion

		#region ICollection Members

		void ICollection.CopyTo(Array array, int index)
		{
			if (array == null)
				throw new ArgumentNullException("array");
			if (index < 0)
				throw new ArgumentOutOfRangeException("index", "must be greater than or equal to zero");
			if ((array.Length - index) < Count)
				throw new ArgumentOutOfRangeException("array", "array not large enough");

			var asPairs = array as KeyValuePair<TKey, TValue>[];
			if (asPairs != null)
				this.CopyToArray(asPairs, index);
			else
			{
				var asEntries = array as DictionaryEntry[];
				if (asEntries != null)
					this.Select(e => new DictionaryEntry(e.Key, e.Value)).CopyToArray(asEntries, index);
				else
					this.Select(e => (object)e).CopyToArray((object[])array, index);
			}
		}

		public bool IsSynchronized
		{
			get { return false; }
		}

		object ICollection.SyncRoot
		{
			get { throw new NotSupportedException(); }
		}

		#endregion

		#region IDictionary Members

		void IDictionary.Add(object key, object value)
		{
			Add((TKey)key, (TValue)value);
		}

		bool IDictionary.Contains(object key)
		{
			return (key is TKey) ? ContainsKey((TKey)key) : false;
		}

		IDictionaryEnumerator IDictionary.GetEnumerator()
		{
			return this.AsIDictionaryEnumerator();
		}

		public bool IsFixedSize
		{
			get { return false; }
		}

		ICollection IDictionary.Keys
		{
			get { return new KeyCollection(this); }
		}

		void IDictionary.Remove(object key)
		{
			if (key is TKey)
				Remove((TKey)key);
		}

		ICollection IDictionary.Values
		{
			get { return this.Transform(kvp => kvp.Value); }
		}

		object IDictionary.this[object key]
		{
			get
			{
				if (key is TKey)
					return this[(TKey)key];
				else
					throw new KeyNotFoundException();
			}
			set
			{
				this[(TKey)key] = (TValue)value;
			}
		}

		#endregion

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		public class Builder
		{
			public Builder()
			{
				MaxEntries = long.MaxValue;
			}

			public TimeSpan? EntryExpiration { get; set; }
			public long MaxEntries { get; set; }
			public Cache<TKey, TValue> Cache
			{
				get
				{
					return new Cache<TKey, TValue>(this);
				}
			}
		}

		#region Implementation
		private Cache(Builder builder)
		{
			EntryExpiration = builder.EntryExpiration;
			MaxEntries = builder.MaxEntries;
		}

		private EntryData GetEntry(TValue value)
		{
			var now = DateTime.UtcNow;
			var expiration = (EntryExpiration != null) ? (DateTime?)(now + EntryExpiration.Value) : null;
			return new EntryData(value, now, expiration);
		}

		private void TrackWrite(EntryData newEntry, TKey key)
		{
			_LastWrite.Enqueue(new KeyValuePair<TKey, EntryData>(key, newEntry));
		}

		private void Clean()
		{
			KeyValuePair<TKey, EntryData> queued;
			bool expiredEntries = _LastWrite.TryPeek(out queued) && (queued.Value.Expired || queued.Value.Removed);
			while (expiredEntries || (_Entries.Count > MaxEntries))
			{
				// TODO: Ideally we'd have a queue where we could peek first, check if it's something we want,
				// then dequeue iff what's @ the head == what we peeked. But this is good enough - at worst
				// we'll have a slightly over-aggressive clean
				if (! _LastWrite.TryDequeue(out queued))
					break;

				expiredEntries = queued.Value.Expired || queued.Value.Removed;
				EntriesAsCollection.Remove(queued);
			}
		}

		private ICollection<KeyValuePair<TKey, EntryData>> EntriesAsCollection
		{
			get { return _Entries; }
		}

		private class EntryData
		{
			public TValue Data;
			public readonly DateTime? UtcExpires;
			public DateTime UtcLastUsed;
			public bool Removed = false;

			public EntryData(TValue data, DateTime utcLastUsed, DateTime? utcExpires)
			{
				Data = data;
				UtcLastUsed = utcLastUsed;
				UtcExpires = utcExpires;
			}

			public bool Expired
			{
				get { return (UtcExpires != null) && (UtcExpires.Value <= DateTime.UtcNow); }
			}

			public void SetRemoved()
			{
				Removed = true;
				// Clear the data so it gets gc'ed
				Data = default(TValue);
			}
		}

		private class KeyCollection : CollectionWrapperView<TKey, KeyValuePair<TKey, TValue>>
		{
			readonly IDictionary<TKey, TValue> _Dictionary;
			public KeyCollection(IDictionary<TKey, TValue> dictionary)
				: base(dictionary, kvp => kvp.Key)
			{
				_Dictionary = dictionary;
			}

			override public bool Contains(TKey key)
			{
				return _Dictionary.ContainsKey(key);
			}

			override public bool Remove(TKey item)
			{
				return _Dictionary.Remove(item);
			}
		}
		#endregion
	}
}
