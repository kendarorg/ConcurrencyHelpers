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
