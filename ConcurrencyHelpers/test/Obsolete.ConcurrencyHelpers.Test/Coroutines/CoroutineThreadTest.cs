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
