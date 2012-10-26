using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;

using Xunit;

namespace CommonCore.Cache.Tests
{
	/// <summary>
	/// Tests for Cache class
	/// </summary>
	public class CacheTests
	{
		const int MillisExpire = 50;

		object Object1 = new object();
		object Object2 = new object();

		Cache<int, object> TestCache
		{
			get
			{
				return new Cache<int, object>.Builder() { MaxEntries = 100, EntryExpiration = TimeSpan.FromMilliseconds(MillisExpire) }.Cache;
			}
		}

		[Fact]
		public void TestTryAdd()
		{
			var test = TestCache;

			test.Add(5, Object1);

			Assert.False(test.TryAdd(5, Object2));
			Assert.Equal(Object1, test[5]);
			System.Threading.Thread.Sleep(MillisExpire + 1);

			Assert.True(test.TryAdd(5, Object2));
			Assert.Equal(Object2, test[5]);
		}

		[Fact]
		public void TestAdd()
		{
			var test = TestCache;

			test.Add(5, Object1);

			try
			{
				test.Add(5, Object2);
				Assert.True(false);
			}
			catch (ArgumentException)
			{
				Assert.Equal(Object1, test[5]);
			}
			System.Threading.Thread.Sleep(MillisExpire + 1);

			test.Add(5, Object2);
			Assert.Equal(Object2, test[5]);
		}

		[Fact]
		public void TestGetOrAdd()
		{
			var test = TestCache;

			test.Add(5, Object1);

			object existing;
			Assert.False(test.GetOrAdd(5, Object2, out existing));
			Assert.Equal(Object1, existing);
			Assert.Equal(Object1, test[5]);

			System.Threading.Thread.Sleep(MillisExpire + 1);

			Assert.True(test.GetOrAdd(5, Object2, out existing));
			Assert.Equal(Object2, existing);
			Assert.Equal(Object2, test[5]);
		}

		[Fact]
		public void TestRemove()
		{
			var test = TestCache;

			test.Add(5, Object1);
			Assert.True(test.Remove(5));
			test.Add(5, Object2);
			Assert.Equal(Object2, test[5]);

			System.Threading.Thread.Sleep(MillisExpire + 1);

			Assert.False(test.Remove(5));
			object existing;
			Assert.False(test.TryGetValue(5, out existing));
			Assert.Null(existing);
			test.Add(5, Object1);
			Assert.Equal(Object1, test[5]);
		}
	}
}
