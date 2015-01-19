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
