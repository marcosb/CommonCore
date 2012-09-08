using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonCore.Collections
{
	public interface ITransformedCollection<T> : ICollection<T>, ICollection
	{ }

	public class CollectionWrapperView<T, TSource> : ITransformedCollection<T>
	{
		protected readonly ICollection<TSource> _Source;
		protected readonly Func<TSource, T> _TransformToFn;

		protected internal CollectionWrapperView(ICollection<TSource> source, Func<TSource, T> transformToFn)
		{
			_Source = source;
			_TransformToFn = transformToFn;
		}

		#region ICollection<T> Members

		public virtual void Add(T item)
		{
			throw new InvalidOperationException("Add is not supported for CollectionWrapperView");
		}

		public void Clear()
		{
			_Source.Clear();
		}

		public virtual bool Contains(T item)
		{
			foreach (var source in _Source)
			{
				if (Object.Equals(_TransformToFn(source), item))
					return true;
			}

			return false;
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			int i = 0;
			foreach (var source in _Source)
			{
				array[arrayIndex + i++] = _TransformToFn(source);
			}
		}

		public int Count
		{
			get { return _Source.Count; }
		}

		public bool IsReadOnly
		{
			get { return _Source.IsReadOnly; }
		}

		public virtual bool Remove(T item)
		{
			throw new InvalidOperationException("Remove is not implemented for CollectionWrapperView");
		}

		#endregion

		#region IEnumerable<T> Members

		public IEnumerator<T> GetEnumerator()
		{
			return _Source.Select(_TransformToFn).GetEnumerator();
		}

		#endregion

		#region ICollection Members

		public void CopyTo(Array array, int index)
		{
			int i = 0;
			foreach (var source in _Source)
			{
				array.SetValue(_TransformToFn(source), index + i++);
			}
		}

		public bool IsSynchronized
		{
			get
			{
				var asCollection = _Source as ICollection;
				return (asCollection != null) ? asCollection.IsSynchronized : false;
			}
		}

		public object SyncRoot
		{
			get
			{
				var asCollection = _Source as ICollection;
				return (asCollection != null) ? asCollection.SyncRoot : _Source;
			}
		}

		#endregion

		#region IEnumerable Members

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion
	}

	internal class CollectionWrapper<T, TSource> : CollectionWrapperView<T, TSource>
	{
		private readonly Func<T, TSource> _TransformFromFn;

		protected internal CollectionWrapper(ICollection<TSource> source, Func<TSource, T> transformToFn, Func<T, TSource> transformFromFn)
			: base(source, transformToFn)
		{
			_TransformFromFn = transformFromFn;
		}

		override public void Add(T item)
		{
			_Source.Add(_TransformFromFn(item));
		}

		override public bool Remove(T item)
		{
			return _Source.Remove(_TransformFromFn(item));
		}

		override public bool Contains(T item)
		{
			return _Source.Contains(_TransformFromFn(item));
		}
	}
}
