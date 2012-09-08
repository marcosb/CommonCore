using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonCore.Collections
{
	public static class CollectionExtension
	{
		/// <summary>
		/// Transforms a <see cref="System.Collections.Generic.Collection{T}"/> from one with elements of type TSource to one with elements of type T,
		/// with conversion specified by transformToFn.
		/// 
		/// The returned collection is a view, and as such any modifications to source will apply to the result.
		/// </summary>
		/// <typeparam name="T">The type of elements in the resulting view</typeparam>
		/// <typeparam name="TSource">The type of elements in the source collection</typeparam>
		/// 
		/// <param name="source">The source collection to be transformed</param>
		/// <param name="transformToFn">The function to transform elements in the collection from TSource to T</param>
		/// 
		/// <exception cref="System.InvalidOperationException">
		/// Thrown if <see cref="System.Collections.Generic.Collection{T}.Add(T)"/> is called.
		/// Use <see cref="Transform{TSource, T}(ICollection<TSource>, Func<TSource, T>, Func<T, TSource> transformFromFn)"/> to allow for
		/// adding elements to the resulting view.</exception>
		public static ITransformedCollection<T> Transform<TSource, T>(this ICollection<TSource> source, Func<TSource, T> transformToFn)
		{
			return new CollectionWrapperView<T, TSource>(source, transformToFn);
		}

		/// <summary>
		/// Transforms a <see cref="System.Collections.Generic.Collection{T}"/> from one with elements of type TSource to one with elements of type T,
		/// with conversion specified by transformToFn.
		/// 
		/// The returned collection is a view, and as such any modifications to source will apply to the result.
		/// </summary>
		/// <typeparam name="T">The type of elements in the resulting view</typeparam>
		/// <typeparam name="TSource">The type of elements in the source collection</typeparam>
		/// 
		/// <param name="source">The source collection to be transformed</param>
		/// <param name="transformToFn">The function to transform elements in the collection from TSource to T</param>
		/// <param name="transformFromFn">The function to transform elements added to the collection from T to TSource</param>
		public static ITransformedCollection<T> Transform<TSource, T>(
			this ICollection<TSource> source, Func<TSource, T> transformToFn, Func<T, TSource> transformFromFn)
		{
			return new CollectionWrapper<T, TSource>(source, transformToFn, transformFromFn);
		}
	}
}
