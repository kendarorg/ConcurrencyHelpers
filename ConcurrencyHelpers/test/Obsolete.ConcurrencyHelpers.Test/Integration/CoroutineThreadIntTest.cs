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
using System.Collections.Generic;
using System.Threading;
using ConcurrencyHelpers.Coroutines;
using ConcurrencyHelpers.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConcurrencyHelpers.Test.Integration
{
	[TestClass]
	public class CoroutineThreadIntTest
	{
		private const int MULTIPLE_COUNT = 30;

		[TestMethod]
		public void CoroutineThreadIntShouldBeExecutedTheRightNumberOfCycles()
		{
			var ft = new CoroutineThread(1);
			var first = new WaitCoroutine(100);
			var second = new WaitCoroutine(200);
			ft.AddCoroutine(first, second);
			ft.Start();
			Thread.Sleep(1100);
			ft.Stop();
			Thread.Sleep(300);
			Assert.IsTrue((first.Cycles + second.Cycles) >= 13, "Cycles were " + (first.Cycles + second.Cycles));
			Console.WriteLine("First: " + first.Cycles);
			Console.WriteLine("Second: " + second.Cycles);
		}

		[TestMethod]
		[Ignore]
		public void CoroutineThreadIntShouldBeExecutedTheRightNumberOfCyclesByHundredsOfThings()
		{
			// ReSharper disable once ConvertToConstant.Local
			int cycleLength = 100;
			int coroutinesCount = 30;
			int runningTimeMs = 1000;
			var ft = new CoroutineThread(1);
			var fl = new List<WaitCoroutine>();
			for (int i = 0; i < coroutinesCount; i++)
			{
				var fb = new WaitCoroutine(cycleLength);
				fl.Add(fb);
				ft.AddCoroutine(fb);
			}

			ft.Start();
			Thread.Sleep(1000 + 100);
			ft.Stop();
			Thread.Sleep(300);
			var total = 0;
			foreach (var fb in fl)
			{
				total += fb.Cycles;
				Console.WriteLine("Result: " + fb.Cycles);
			}
			Console.WriteLine("Total: " + total);
			Assert.IsTrue(total > 100, "Executed " + total);
		}

		private ManualResetEvent _reset;
		[TestMethod]
		[Ignore]
		public void ShouldBeExecutedTheRightNumberOfCyclesDefaultThreads()
		{
			var threadContainer = new CoroutineThread(100);
			threadContainer.Start();
			_reset = new ManualResetEvent(false);
			// ReSharper disable once ConvertToConstant.Local
			int hundred = 100;

			var first = new WaitCoroutine(hundred) { Thread = threadContainer };
			var second = new WaitCoroutine(200) { Thread = threadContainer };
			var firstt = new Thread(ExecuteTimerCoroutine);
			var secondt = new Thread(ExecuteTimerCoroutine);
			firstt.Start(first);
			secondt.Start(second);
			_reset.Set();
			Thread.Sleep(1000);
			threadContainer.Stop();
			Thread.Sleep(300);
			Assert.IsTrue((first.Cycles + second.Cycles) < 100);
			Console.WriteLine("First: " + first.Cycles);
			Console.WriteLine("Second: " + second.Cycles);
		}

		[TestMethod]
		[Ignore]
		public void ShouldBeExecutedTheRightNumberOfCyclesByHundredsOfThingsDefaultThreads()
		{
			// ReSharper disable once ConvertToConstant.Local
			int hundred = 100;
			_reset = new ManualResetEvent(false);

			var threadContainer = new CoroutineThread(100);
			threadContainer.Start();

			var fl = new List<WaitCoroutine>();
			for (int i = 0; i < MULTIPLE_COUNT; i++)
			{
				var fb = new WaitCoroutine(hundred) { Thread = threadContainer };
				fl.Add(fb);
				var flt = new Thread(ExecuteTimerCoroutine);
				flt.Start(fb);
			}
			_reset.Set();

			Thread.Sleep(1000);
			threadContainer.Stop();
			Thread.Sleep(300);
			var total = 0;
			foreach (var fb in fl)
			{
				total += fb.Cycles;
				Console.WriteLine("Result: " + fb.Cycles);
			}
			Console.WriteLine("Total: " + total);
			Assert.IsTrue(total < 100);
		}


		[TestMethod]
		public void CoroutineThreadIntShouldHandleCorrectlyTheSubroutines()
		{
			// ReSharper disable once ConvertToConstant.Local
			int hundred = 100;
			var ft = new CoroutineThread(1);
			var first = new CoroutineWithSub(hundred);
			var second = new CoroutineWithSub(200);
			ft.AddCoroutine(first);//, second);
			ft.Start();
			Thread.Sleep(1000);
			ft.Stop();
			Thread.Sleep(300);
			//Assert.AreEqual(15, (int)(first.Cycles + second.Cycles));
			Console.WriteLine("First: " + first.Cycles);
			Console.WriteLine("Second: " + second.Cycles);
		}

		public void ExecuteTimerCoroutine(object param)
		{
			var timerCoroutine = (WaitCoroutine)param;
			_reset.WaitOne(1000);
			while (timerCoroutine.Thread.Status != CoroutineThreadStatus.Stopped)
			{
				Step finished = null;

				foreach (var step in timerCoroutine.Run())
				{
					finished = step;
				}
				if (finished == null)
				{
					return;
				}
			}
		}

		[TestMethod]
		public void CoroutineThreadIntShouldBePossibleToPauseAndRestart()
		{
			// ReSharper disable once ConvertToConstant.Local
			int hundred = 100;
			var ft = new CoroutineThread(1);
			var first = new WaitCoroutine(hundred);
			var second = new WaitCoroutine(200);
			ft.AddCoroutine(first, second);
			ft.Start();
			Thread.Sleep(1000);
			ft.Pause();
			Thread.Sleep(300);
			var firstCycles = first.Cycles;
			var secondCycles = second.Cycles;
			Assert.AreEqual(ft.Status, CoroutineThreadStatus.Paused);
			Thread.Sleep(1500);
			Assert.AreEqual(firstCycles, first.Cycles);
			Assert.AreEqual(secondCycles, second.Cycles);
			ft.Start();
			Thread.Sleep(300);
			Assert.AreEqual(CoroutineThreadStatus.Running, ft.Status);
			ft.Stop();
			Thread.Sleep(300);
			Assert.AreEqual(CoroutineThreadStatus.Stopped, ft.Status);

			Assert.IsTrue(firstCycles <= first.Cycles);
			Assert.IsTrue(secondCycles <= second.Cycles);
		}

		[TestMethod]
		public void CoroutineThreadIntShouldBePossibleToInterceptExceptionWithoutBlocking()
		{
			// ReSharper disable once ConvertToConstant.Local
			int hundred = 100;
			var ft = new CoroutineThread(1);
			var first = new WaitCoroutine(hundred);
			var second = new WaitCoroutine(200) { ThrowsException = true };
			ft.AddCoroutine(first, second);
			ft.Start();
			Thread.Sleep(1000);
			ft.Pause();
			Thread.Sleep(300);

			Assert.IsTrue(second.Exceptions <= second.Cycles);

			var firstCycles = first.Cycles;
			var secondCycles = second.Cycles;
			Assert.AreEqual(ft.Status, CoroutineThreadStatus.Paused);
			Thread.Sleep(1500);
			Assert.AreEqual(firstCycles, first.Cycles);
			Assert.AreEqual(secondCycles, second.Cycles);
			ft.Start();
			Thread.Sleep(300);
			Assert.AreEqual(CoroutineThreadStatus.Running, ft.Status);
			ft.Stop();
			Thread.Sleep(300);
			Assert.AreEqual(CoroutineThreadStatus.Stopped, ft.Status);

			Assert.IsTrue(firstCycles <= first.Cycles);
			Assert.IsTrue(secondCycles <= second.Cycles);

			Assert.IsTrue(second.Exceptions<= second.Cycles);
		}
	}
}
