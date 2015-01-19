// ===========================================================
// Copyright (c) 2014-2015, Enrico Da Ros/kendar.org
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 
// * Redistributions of source code must retain the above copyright notice, this
//   list of conditions and the following disclaimer.
// 
// * Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
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
