// ===========================================================
// Copyright (C) 2014-2015 Kendar.org
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation 
// files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, 
// modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software 
// is furnished to do so, subject to the following conditions:
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES 
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS 
// BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF 
// OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ===========================================================


using System.Collections.Generic;
using System.Threading;
using ConcurrencyHelpers.Containers.Asyncs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConcurrencyHelpers.Test.Containers
{
	[TestClass]
	public class LockFreeDictionaryTest
	{
		[TestMethod]
		public void LockFreeDictionaryAdd()
		{
			var dict = new AsyncLockFreeDictionary<int, int>();
			Thread.Sleep(100);
			dict.Add(1, 1);
			dict.Add(2, 2);
			dict.Add(3, 3);
			dict.Add(4, 4);
			dict.Add(5, 5);
			Thread.Sleep(100);

			Assert.AreEqual(5, dict.Count);

			Assert.AreEqual(1, dict[1]);
			Assert.AreEqual(2, dict[2]);
			Assert.AreEqual(3, dict[3]);
			Assert.AreEqual(4, dict[4]);
			Assert.AreEqual(5, dict[5]);

		}

		[TestMethod]
		public void LockFreeDictionaryAddKeyValuePair()
		{
			var dict = new AsyncLockFreeDictionary<int, int>();
			Thread.Sleep(100);
			dict.Add(new KeyValuePair<int, int>(1, 1));
			dict.Add(new KeyValuePair<int, int>(2, 2));
			dict.Add(new KeyValuePair<int, int>(3, 3));
			dict.Add(new KeyValuePair<int, int>(4, 4));
			dict.Add(new KeyValuePair<int, int>(5, 5));
			Thread.Sleep(100);

			Assert.AreEqual(5, dict.Count);

			Assert.AreEqual(1, dict[1]);
			Assert.AreEqual(2, dict[2]);
			Assert.AreEqual(3, dict[3]);
			Assert.AreEqual(4, dict[4]);
			Assert.AreEqual(5, dict[5]);
		}


		[TestMethod]
		public void LockFreeDictionaryContainsKey()
		{
			var dict = new AsyncLockFreeDictionary<int, int>();
			Thread.Sleep(100);
			dict.Add(1, 1);
			dict.Add(2, 2);
			dict.Add(3, 3);
			dict.Add(4, 4);
			dict.Add(5, 5);
			Thread.Sleep(100);

			Assert.AreEqual(5, dict.Count);


			Assert.IsTrue(dict.ContainsKey(1));
			Assert.IsTrue(dict.ContainsKey(2));
			Assert.IsTrue(dict.ContainsKey(3));
			Assert.IsTrue(dict.ContainsKey(4));
			Assert.IsTrue(dict.ContainsKey(5));
		}

		[TestMethod]
		public void LockFreeDictionaryTryAdd()
		{
			var dict = new AsyncLockFreeDictionary<int, int>();
			Thread.Sleep(100);
			dict.Add(1, 1);
			dict.Add(2, 2);
			Thread.Sleep(100);
			dict.TryAdd(1, 100, (preExisting, newKeyValuePair) => new KeyValuePair<int, int>(newKeyValuePair.Key, newKeyValuePair.Value * 2));
			Thread.Sleep(100);

			Assert.AreEqual(2, dict.Count);
			Assert.AreEqual(200, dict[1]);
			Assert.AreEqual(2, dict[2]);
		}

		[TestMethod]
		public void LockFreeDictionaryTryAddKeyValuePair()
		{
			var dict = new AsyncLockFreeDictionary<int, int>();
			Thread.Sleep(100);
			dict.Add(1, 1);
			dict.Add(2, 2);
			Thread.Sleep(100);
			dict.TryAdd(new KeyValuePair<int, int>(1, 100), (preExisting, newKeyValuePair) => new KeyValuePair<int, int>(newKeyValuePair.Key, newKeyValuePair.Value * 2));
			Thread.Sleep(100);

			Assert.AreEqual(2, dict.Count);
			Assert.AreEqual(200, dict[1]);
			Assert.AreEqual(2, dict[2]);
		}


		[TestMethod]
		public void LockFreeDictionaryRemove()
		{
			var dict = new AsyncLockFreeDictionary<int, int>();
			Thread.Sleep(100);
			dict.Add(1, 1);
			dict.Add(2, 2);
			dict.Add(3, 3);
			dict.Add(4, 4);
			dict.Add(5, 5);
			Thread.Sleep(100);

			dict.Remove(4);
			Thread.Sleep(100);

			Assert.AreEqual(4, dict.Count);


			Assert.IsTrue(dict.ContainsKey(1));
			Assert.IsTrue(dict.ContainsKey(2));
			Assert.IsTrue(dict.ContainsKey(3));
			Assert.IsTrue(dict.ContainsKey(5));
		}

		[TestMethod]
		public void LockFreeDictionaryRemoveKeyValuePair()
		{
			var dict = new AsyncLockFreeDictionary<int, int>();
			Thread.Sleep(100);
			dict.Add(1, 1);
			dict.Add(2, 2);
			dict.Add(3, 3);
			dict.Add(4, 4);
			dict.Add(5, 5);
			Thread.Sleep(100);

			dict.Remove(new KeyValuePair<int, int>(4, 0));
			Thread.Sleep(100);

			Assert.AreEqual(4, dict.Count);


			Assert.IsTrue(dict.ContainsKey(1));
			Assert.IsTrue(dict.ContainsKey(2));
			Assert.IsTrue(dict.ContainsKey(3));
			Assert.IsTrue(dict.ContainsKey(5));
		}


		[TestMethod]
		public void LockFreeDictionaryContainsKeyValuePair()
		{
			var dict = new AsyncLockFreeDictionary<int, int>();
			Thread.Sleep(100);
			dict.Add(1, 1);
			dict.Add(2, 2);
			dict.Add(3, 3);
			dict.Add(4, 4);
			dict.Add(5, 5);
			Thread.Sleep(100);

			Assert.AreEqual(5, dict.Count);
			Assert.IsFalse(dict.Contains(new KeyValuePair<int, int>(1, 9)));
			Assert.IsTrue(dict.Contains(new KeyValuePair<int, int>(1, 1)));
		}


		[TestMethod]
		public void LockFreeDictionaryEnumerator()
		{
			var dict = new AsyncLockFreeDictionary<int, int>();
			Thread.Sleep(100);
			dict.Add(1, 1);
			dict.Add(2, 2);
			dict.Add(3, 3);
			dict.Add(4, 4);
			dict.Add(5, 5);
			Thread.Sleep(100);

			int compare = 1;
			foreach (var item in dict)
			{
				var result = item.Value;
				Assert.AreEqual(compare, result);
				compare++;
			}
		}
	}
}
