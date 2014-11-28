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
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using ConcurrencyHelpers.Coroutines;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConcurrencyHelpers.Test.Coroutines
{
	

	public class ContinuosCoroutine : Coroutine
	{
		public int Counter;
		public bool Stop;

		public override IEnumerable<Step> Run()
		{
			while (!Stop)
			{
				Counter++;
				yield return Step.Current;
			}
		}

		public override void OnError(Exception ex)
		{
			throw new NotImplementedException();
		}
	}

	public class CounterCoroutine : Coroutine
	{
		private readonly Exception _exception;
		public int InvokedOnError;

		public CounterCoroutine(Exception exception = null)
		{
			_exception = exception;
		}

		public static int Disposed = 0;
		public int Counter;
		public override IEnumerable<Step> Run()
		{
			var i = 10;
			while (i > 0)
			{
				yield return Step.Current;
				i--;
				Counter++;
			}
			if (_exception != null)
			{
				throw _exception;
			}
		}

		public override void OnError(Exception ex)
		{
			ShouldTerminate = true;
			InvokedOnError++;
		}

	}

	[TestClass]
	public class CoroutineThreadTest
	{
		[TestMethod]
		public void CoroutineThreadEachStepShouldAdvanceOfOneTheCoroutines()
		{
			var ct = new CoroutineTestThread();
			ct.Initialize();
			var cr = new CounterCoroutine();
			ct.AddCoroutine(cr);
			//Initialize step
			ct.StepForward();
			//Run
			ct.StepForward();
			Assert.AreEqual(1, cr.Counter);
			ct.StepForward();
			Assert.AreEqual(2, cr.Counter);
		}

		[TestMethod]
		public void CoroutineThreadAfterTerminationShouldNotExistsTheCoroutine()
		{
			var ct = new CoroutineTestThread();
			ct.Initialize();
			var cr = new CounterCoroutine();
			ct.AddCoroutine(cr);
			//Initialize step
			ct.StepForward();

			while (cr.Counter < 10)
			{
				ct.StepForward();
			}

			Assert.AreEqual(10, cr.Counter);
			ct.StepForward();
			ct.StepForward();
			Assert.AreEqual(10, cr.Counter);
			Assert.AreEqual(1, ct.StartedCoroutines);
			Assert.AreEqual(1, ct.TerminatedCoroutines);
		}

		[TestMethod]
		public void CoroutineThreadItShouldBePossibleToStoreDataInsideTheGlobalCoroutineVars()
		{
			var ct = new CoroutineTestThread();
			ct.Initialize();
			ct["test"] = "test";
			Assert.IsTrue(ct.ContainsKey("test"));
			ct.RemoveValueAt("test");
			Assert.IsFalse(ct.ContainsKey("test"));
			ct["test2"] = "test";
			ct.ClearValues();
			Assert.IsFalse(ct.ContainsKey("test2"));
			ct["test2"] = "test3";
			Assert.AreEqual("test3", ct["test2"]);
		}

		[TestMethod]
		public void CoroutineThreadExceptionShouldBeHandledTerminatingThread()
		{
			var ct = new CoroutineTestThread();
			ct.Initialize();
			var cr = new CounterCoroutine(new Exception());
			ct.AddCoroutine(cr);
			//Initialize step
			ct.StepForward();

			while (cr.Counter < 10)
			{
				ct.StepForward();
			}

			Assert.AreEqual(10, cr.Counter);
			ct.StepForward();
			ct.StepForward();
			Assert.AreEqual(10, cr.Counter);
			Assert.AreEqual(1, cr.InvokedOnError);
			Assert.AreEqual(1, ct.StartedCoroutines);
			Assert.AreEqual(1, ct.TerminatedCoroutines);
		}

		[TestMethod]
		public void CoroutineThreadItShouldBePossibleToPauseAndRestart()
		{
			var ct = new CoroutineTestThread();
			ct.Initialize();
			var cr = new ContinuosCoroutine();
			ct.AddCoroutine(cr);
			//Initialize step
			ct.StepForward();

			_continueThread = true;
			var th = new Thread(PauseRestartThread);
			th.Start(ct);
			Thread.Sleep(100);
			Assert.IsTrue(cr.Counter <= 12, cr.Counter.ToString());
			ct.Pause();
			var runs = cr.Counter;
			Thread.Sleep(1100);
			Assert.AreEqual(ct.Status,CoroutineThreadStatus.Paused);
			Assert.AreEqual(runs, cr.Counter);
			ct.Start();
			Thread.Sleep(100);
			Assert.IsTrue(cr.Counter < (12 + runs));
			Assert.AreEqual(ct.Status, CoroutineThreadStatus.Running);
			_continueThread = false;
		}

		private bool _continueThread = false;

		private void PauseRestartThread(object obj)
		{
			var ct = (CoroutineTestThread)obj;
			while (_continueThread)
			{
				ct.StepForward();
			}
		}



		[TestMethod]
		public void CoroutineThreadItShouldBePossibleToStopAPausedThread()
		{
			var ct = new CoroutineTestThread();
			ct.Initialize();
			var cr = new ContinuosCoroutine();
			ct.AddCoroutine(cr);
			//Initialize step
			ct.StepForward();

			_continueThread = true;
			var th = new Thread(PauseRestartThread);
			th.Start(ct);
			Thread.Sleep(100);
			Assert.IsTrue(cr.Counter < 12);
			ct.Pause();
			var runs = cr.Counter;
			Thread.Sleep(100);
			Assert.AreEqual(ct.Status, CoroutineThreadStatus.Paused);
			ct.Stop();
			Thread.Sleep(1100);
			Assert.AreEqual(runs, cr.Counter);
			Assert.AreEqual(ct.Status, CoroutineThreadStatus.Stopped);
			_continueThread = false;
		}

		[TestMethod]
		public void CoroutineThreadCultureShouldBeMantainedBetweenCoroutineCalls()
		{
			var ct = new CoroutineTestThread();
			ct.Initialize();
			var crEs = new CultureCoroutine(CultureInfo.GetCultureInfo("es-ES"));
			var crFr = new CultureCoroutine(CultureInfo.GetCultureInfo("fr-FR"));
			ct.AddCoroutine(crEs);
			ct.AddCoroutine(crFr);
			//Initialize step
			ct.StepForward();
			//Run
			for (int i = 0; i < 100; i++)
			{
				ct.StepForward();	
			}

			Assert.AreEqual(0, crEs.Errors);
			Assert.AreEqual(0, crFr.Errors);
		}
	}

	public class CultureCoroutine : ContinuosCoroutine
	{
		public CultureInfo LocalCultureInfo;
		public int Errors;

		public CultureCoroutine(CultureInfo cultureInfo)
		{
			LocalCultureInfo = cultureInfo;
		}

		public override IEnumerable<Step> Run()
		{
			System.Threading.Thread.CurrentThread.CurrentCulture = LocalCultureInfo;
			while (!Stop)
			{
				Counter++;
				if (System.Threading.Thread.CurrentThread.CurrentCulture != LocalCultureInfo)
				{
					Errors++;
				}
				yield return Step.Current;
			}
		}

		public override void OnError(Exception ex)
		{
			throw new NotImplementedException();
		}
	}
}
