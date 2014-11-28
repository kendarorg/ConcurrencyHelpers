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


using System;
using System.Linq;
using ConcurrencyHelpers.Containers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConcurrencyHelpers.Test.Containers
{
	[TestClass]
	public class LockFreeQueueTest
	{
		[TestMethod]
		public void LockFreeQueueDequeue()
		{
			var queue = new LockFreeQueue<int>();
			//We enqueue some items to test
			queue.Enqueue(1);
			queue.Enqueue(2);
			queue.Enqueue(3);
			queue.Enqueue(4);
			queue.Enqueue(5);
			//For each number enqueued we check that is removed properly and in the
			//queue order (FIFO)
			Assert.AreEqual(queue.DequeueSingle(), 1);
			Assert.AreEqual(queue.Count, 4);
			Assert.AreEqual(queue.DequeueSingle(), 2);
			Assert.AreEqual(queue.Count, 3);
			Assert.AreEqual(queue.DequeueSingle(), 3);
			Assert.AreEqual(queue.Count, 2);
			Assert.AreEqual(queue.DequeueSingle(), 4);
			Assert.AreEqual(queue.Count, 1);
			Assert.AreEqual(queue.DequeueSingle(), 5);
			Assert.AreEqual(queue.Count, 0);
			Assert.AreEqual(default(int), queue.DequeueSingle());
		}

		[TestMethod]
		public void LockFreeQueueEnqueue()
		{
			var queue = new LockFreeQueue<int>();
			//We enqueue some items to test and check that the items are inserted correctly
			queue.Enqueue(1);
			Assert.AreEqual(queue.Count, 1);
			queue.Enqueue(2);
			Assert.AreEqual(queue.Count, 2);
			queue.Enqueue(3);
			Assert.AreEqual(queue.Count, 3);
			queue.Enqueue(4);
			Assert.AreEqual(queue.Count, 4);
			queue.Enqueue(5);
			Assert.AreEqual(queue.Count, 5);
		}

		[TestMethod]
		public void LockFreeQueuePeek()
		{
			var queue = new LockFreeQueue<int>();
			Int32 t = 0;
			//We enqueue some items to test
			queue.Enqueue(1);
			queue.Enqueue(2);
			queue.Enqueue(3);
			queue.Enqueue(4);
			queue.Enqueue(5);
			//We check that after a peeking we have the correct value but the item
			//is not deleted
			Assert.IsTrue(queue.Peek(ref t));
			Assert.AreEqual(t, 1);
			Assert.AreEqual(queue.Count, 5);
			queue.DequeueSingle();
			Assert.IsTrue(queue.Peek(ref t));
			Assert.AreEqual(t, 2);
			Assert.AreEqual(queue.Count, 4);
			queue.DequeueSingle();
			Assert.IsTrue(queue.Peek(ref t));
			Assert.AreEqual(t, 3);
			Assert.AreEqual(queue.Count, 3);
			queue.DequeueSingle();
			Assert.IsTrue(queue.Peek(ref t));
			Assert.AreEqual(t, 4);
			Assert.AreEqual(queue.Count, 2);
			queue.DequeueSingle();
			Assert.IsTrue(queue.Peek(ref t));
			Assert.AreEqual(t, 5);
			Assert.AreEqual(queue.Count, 1);
			queue.DequeueSingle();
			Assert.IsFalse(queue.Peek(ref t));
		}

		[TestMethod]
		public void LockFreeQueueClear()
		{
			var queue = new LockFreeQueue<int>();
			queue.Enqueue(1);
			queue.Enqueue(2);
			queue.Enqueue(3);
			queue.Enqueue(4);
			queue.Enqueue(5);
			queue.Clear();
			Assert.AreEqual(queue.Count, 0);
		}

		[TestMethod]
		public void LockFreeQueueDequeueMultiple()
		{
			var queue = new LockFreeQueue<int>();
			//We enqueue some items to test
			queue.Enqueue(1);
			queue.Enqueue(2);
			queue.Enqueue(3);
			queue.Enqueue(4);
			queue.Enqueue(5);

			var result = queue.Dequeue(3).ToArray();
			Assert.AreEqual(3, result.Count());
			Assert.AreEqual(1, result[0]);
			Assert.AreEqual(2, result[1]);
			Assert.AreEqual(3, result[2]);

		}

		[TestMethod]
		public void LockFreeQueueDequeueAll()
		{
			var queue = new LockFreeQueue<int>();
			//We enqueue some items to test
			queue.Enqueue(1);
			queue.Enqueue(2);
			queue.Enqueue(3);
			queue.Enqueue(4);
			queue.Enqueue(5);

			var result = queue.Dequeue().ToArray();
			Assert.AreEqual(5, result.Count());
			Assert.AreEqual(1,result[0]);
			Assert.AreEqual(2, result[1]);
			Assert.AreEqual(3, result[2]);
			Assert.AreEqual(4, result[3]);
			Assert.AreEqual(5, result[4]);
		}
	}
}
