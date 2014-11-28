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


using System.Threading;
using ConcurrencyHelpers.Containers.Asyncs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConcurrencyHelpers.Test.Containers
{
	[TestClass]
	public class AsyncLockFreeListTest
	{
		[TestMethod]
		public void AsyncLockFreeListRemoveAt()
		{
			var list = new AsyncLockFreeList<int>();
			//We enqueue some items to test
			list.Add(1);
			list.Add(2);
			list.Add(3);
			list.Add(4);
			list.Add(5);
			Thread.Sleep(100);

			Assert.AreEqual(list.Count, 5);
			list.RemoveAt(0);
			Thread.Sleep(100);
			Assert.AreEqual(list.Count, 4);
			Assert.AreEqual(list[0], 2);
			Assert.AreEqual(list[1], 3);
			Assert.AreEqual(list[2], 4);
			Assert.AreEqual(list[3], 5);
		}

		[TestMethod]
		public void AsyncLockFreeListRemove()
		{
			var list = new AsyncLockFreeList<int>();
			//We enqueue some items to test
			list.Add(1);
			list.Add(2);
			list.Add(3);
			list.Add(4);
			list.Add(5);
			Thread.Sleep(100);

			Assert.AreEqual(list.Count, 5);
			list.Remove(1);
			Thread.Sleep(100);
			Assert.AreEqual(list.Count, 4);
			Assert.AreEqual(list[0], 2);
			Assert.AreEqual(list[1], 3);
			Assert.AreEqual(list[2], 4);
			Assert.AreEqual(list[3], 5);
		}

		[TestMethod]
		public void AsyncLockFreeListAdd()
		{
			var list = new AsyncLockFreeList<int>();
			//We enqueue some items to test and check that the items are inserted correctly
			list.Add(1);
			Thread.Sleep(100);
			Assert.AreEqual(list.Count, 1);
			list.Add(2);
			Thread.Sleep(100);
			Assert.AreEqual(list.Count, 2);
			list.Add(3);
			Thread.Sleep(100);
			Assert.AreEqual(list.Count, 3);
			list.Add(4);
			Thread.Sleep(100);
			Assert.AreEqual(list.Count, 4);
			list.Add(5);
			Thread.Sleep(100);
			Assert.AreEqual(list.Count, 5);
		}


		[TestMethod]
		public void AsyncLockFreeListClear()
		{
			var list = new AsyncLockFreeList<int>();
			list.Add(1);
			list.Add(2);
			list.Add(3);
			list.Add(4);
			list.Add(5);
			Thread.Sleep(100);
			Assert.AreEqual(list.Count, 5);
			list.Clear();
			Thread.Sleep(100);
			Assert.AreEqual(list.Count, 0);
		}

		[TestMethod]
		public void AsyncLockFreeListIndexOf()
		{
			var list = new AsyncLockFreeList<int>();
			list.Add(1);
			list.Add(2);
			list.Add(3);
			list.Add(4);
			list.Add(5);
			Thread.Sleep(100);
			Assert.AreEqual(1, list.IndexOf(2));
		}

		[TestMethod]
		public void AsyncLockFreeListContains()
		{
			var list = new AsyncLockFreeList<int>();
			list.Add(1);
			list.Add(2);
			list.Add(3);
			list.Add(4);
			list.Add(5);
			Thread.Sleep(100);
			Assert.IsTrue(list.Contains(2));
			Assert.IsFalse(list.Contains(6));
		}

		[TestMethod]
		public void AsyncLockFreeListInsert()
		{
			var list = new AsyncLockFreeList<int>();
			list.Add(1);
			list.Add(2);
			list.Add(3);
			list.Add(4);
			list.Add(5);
			list.Insert(1, 999);
			Thread.Sleep(100);
			Assert.IsTrue(list.Contains(999));
			Assert.AreEqual(1, list.IndexOf(999));
			Assert.AreEqual(0, list.IndexOf(1));
			Assert.AreEqual(2, list.IndexOf(2));
			Assert.AreEqual(6, list.Count);
		}

		[TestMethod]
		public void AsyncLockFreeListEnumerator()
		{
			var list = new AsyncLockFreeList<int>();
			list.Add(1);
			list.Add(2);
			list.Add(3);
			list.Add(4);
			list.Add(5);
			Thread.Sleep(100);

			int compare = 1;
			foreach (var item in list)
			{
				var result = item;
				Assert.AreEqual(compare,result);
				compare++;
			}
		}
	}


}
