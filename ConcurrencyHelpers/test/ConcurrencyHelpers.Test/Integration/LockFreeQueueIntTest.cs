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
using System.Diagnostics;
using System.Threading;
using ConcurrencyHelpers.Containers;
using ConcurrencyHelpers.Test.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConcurrencyHelpers.Test.Integration
{
	[TestClass]
	public class LockFreeQueueIntTest
	{
		protected const int ENQUEUED_DATA = 10;

		[TestMethod]
		public void LockFreeQueueIntItShouldBePossibleToReceiveAllStuffPuttedOnTheQueue()
		{
			const int threadsCount = 100;
			const int toSendElements = 10000;
			LockFreeQueueTestUtils.Initialize(toSendElements);

			var threadList = new List<Thread>();

			for (int i = 0; i < threadsCount / 2; i++)
			{
				threadList.Add(new Thread(LockFreeQueueTestUtils.ProducerThread));
				threadList.Add(new Thread(LockFreeQueueTestUtils.ConsumerThread));
			}

			var sw = new Stopwatch();
			sw.Start();

			foreach (Thread t in threadList)
			{
				t.Start();
			}

			while (!LockFreeQueueTestUtils.IsSendCompleted && sw.ElapsedMilliseconds < toSendElements)
			{
				Thread.Sleep(10);
			}

			Assert.IsTrue(LockFreeQueueTestUtils.IsSendCompleted, "Did not completed the send of data");
		}

		[TestMethod]
		public void LockFreeQueueIntItShouldBePossibleToClearAQueueAndDequeueASingleItem()
		{
			var lfq = new LockFreeQueue<string>();
			var lele = new List<string>();
			for (int i = 0; i < ENQUEUED_DATA; i++)
			{
				lele.Add("TEST_" + i);
			}
			lfq.Enqueue(lele);
			Assert.AreEqual("TEST_0", lfq.DequeueSingle());
			lfq.Clear();
			Assert.IsNull(lfq.DequeueSingle());
		}

		[TestMethod]
		public void LockFreeQueueIntItShouldBePossibleToEnqueuAndDequeuNonNullableElements()
		{
			var lfq = new LockFreeQueue<int>();
			var lele = new List<int>();
			for (int i = 0; i < ENQUEUED_DATA; i++)
			{
				lele.Add(i);
			}
			lfq.Enqueue(lele);
			Assert.AreEqual(0, lfq.DequeueSingle());
			lfq.Clear();
			Assert.AreEqual(0, lfq.Count);
			Assert.AreEqual(0, lfq.DequeueSingle());
		}
	}
}
