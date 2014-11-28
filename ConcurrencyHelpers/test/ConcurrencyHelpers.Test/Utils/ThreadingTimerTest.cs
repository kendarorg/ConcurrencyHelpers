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
using ConcurrencyHelpers.Interfaces;
using ConcurrencyHelpers.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConcurrencyHelpers.Test.Utils
{
	[TestClass]
	public class ThreadingTimerTest
	{
		private int _elapsedCount;
		private ThreadingTimer _t;

		private void TimerElapsed(object sender, ElapsedTimerEventArgs e)
		{
			_elapsedCount++;
		}

		private void TimerElapsed100(object sender, ElapsedTimerEventArgs e)
		{
			Thread.Sleep(100);
			_elapsedCount++;
		}

		[TestInitialize]
		public void TestInitialize()
		{
			_elapsedCount = 0;
		}

		[TestCleanup]
		public void TestCleanup()
		{
			_t.Dispose();
		}

		[TestMethod]
		public void ThreadingTimerDelayTimerStart()
		{
			_t = new ThreadingTimer(100, 100);
			_t.Elapsed += TimerElapsed;
			_t.Start();
			Thread.Sleep(90);
			Assert.AreEqual(0, _elapsedCount);
			Thread.Sleep(220);
			Assert.AreEqual(2, _elapsedCount);
		}

		[TestMethod]
		[Ignore]
		public void ThreadingTimerDelayTimerStartChangingTimers()
		{
			_t = new ThreadingTimer(1, 1);
			_t.Elapsed += TimerElapsed;
			_t.Start(100, 100);
			Thread.Sleep(90);
			Assert.AreEqual(0, _elapsedCount);
			Thread.Sleep(220);
			Assert.AreEqual(2, _elapsedCount);
			Assert.AreEqual(_t.TimesRun, _elapsedCount);
		}

		[TestMethod]
		[Ignore]
		public void ThreadingTimerRemoveElapsed()
		{
			_t = new ThreadingTimer(1, 1);
			_t.Elapsed += TimerElapsed;
			_t.Start(100, 100);
			Thread.Sleep(90);
			Assert.AreEqual(0, _elapsedCount);
			Thread.Sleep(120);
			_t.Stop();
			var elapsed = _elapsedCount;

			_t.Elapsed -= TimerElapsed;

			_t.Start(100, 100);
			Thread.Sleep(90);
			Assert.AreEqual(elapsed, _elapsedCount);
			Thread.Sleep(220);
			_t.Stop();
			Assert.AreEqual(elapsed, _elapsedCount);
			Assert.AreNotEqual(_t.TimesRun, _elapsedCount);
		}

		[TestMethod]
		[Ignore]
		public void ThreadingTimerRunGivenTimes()
		{
			_t = new ThreadingTimer(100, 10);
			_t.Elapsed += TimerElapsed;
			_t.Start();
			Thread.Sleep(240);
			Assert.IsTrue(_t.Running);
			_t.Stop();
			Assert.AreEqual(3, _elapsedCount);
			Assert.IsFalse(_t.Running);
		}

		[TestMethod]
		public void ThreadingTimerInitialization()
		{
			_t = new ThreadingTimer(15, 101);
			Assert.AreEqual(15, _t.Period);
			_t.Start();
			_t.Dispose();
			_t = new ThreadingTimer(15);
		}

		[TestMethod]
		public void ThreadingTimerZeroWait()
		{
			_t = new ThreadingTimer(50);
			_t.Elapsed += TimerElapsed;
			_t.Start();
			Thread.Sleep(265);
			Assert.AreEqual(5, _elapsedCount);
		}

		[TestMethod]
		public void ThreadingTimerNoOverlap()
		{
			_t = new ThreadingTimer(50);
			_t.Elapsed += TimerElapsed100;
			_t.Start();
			Thread.Sleep(350);
			_t.Stop();
			Assert.IsTrue(_elapsedCount >= 2);
		}

		[TestMethod]
		public void ThreadingTimerStop()
		{
			_t = new ThreadingTimer(50);
			_t.Elapsed += TimerElapsed;
			_t.Start();
			Thread.Sleep(265);
			Assert.AreEqual(5, _elapsedCount);
			_t.Stop();
			Thread.Sleep(265);
			Assert.AreEqual(5, _elapsedCount);
		}
	}
}
