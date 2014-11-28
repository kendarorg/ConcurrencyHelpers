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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConcurrencyHelpers.Test.Coroutines
{
	[TestClass]
	public class CoroutineTest
	{
		[TestInitialize]
		public void TestInitialize()
		{

		}

		[TestCleanup]
		public void TestCleanup()
		{

		}

		[TestMethod]
		public void ItShouldBePossibleToInvokeLocalAndWaitWithoutReturnValues()
		{
			var ct = new CoroutineTestThread();
			var cr = new LocalInvokerCoroutine();
			ct.Initialize();
			ct.AddCoroutine(cr);
			ct.StepForward();

			for (int i = 0; i < 11; i++)
			{
				ct.StepForward();
			}
			Assert.AreEqual(cr.NormalCalls, 2);
			Assert.AreEqual(cr.StepFunctionCall, 10);
			ct.StepForward();
			ct.StepForward();
			Assert.AreEqual(cr.NormalCalls, 2);
			Assert.AreEqual(cr.StepFunctionCall, 10);
		}


		[TestMethod]
		public void ItShouldBePossibleToInvokeLocalAndWaitWithReturnValues()
		{
			var ct = new CoroutineTestThread();
			const string result = "TestResult";
			var cr = new LocalInvokerCoroutine(result);
			ct.Initialize();
			ct.AddCoroutine(cr);
			ct.StepForward();

			for (int i = 0; i < 12; i++)
			{
				ct.StepForward();
			}
			Assert.AreEqual(cr.NormalCalls, 2);
			Assert.AreEqual(cr.StepFunctionCall, 10);
			Assert.AreEqual(result, cr.RealResult);
		}

		[TestMethod]
		public void ItShouldBePossibleToInvokeActionAsTask()
		{
			var ct = new CoroutineTestThread();
			var cr = new LocalInvokerCoroutine();
			cr.ActionAsTask = true;
			ct.Initialize();
			ct.AddCoroutine(cr);
			ct.StepForward();

			for (int i = 0; i < 12; i++)
			{
				ct.StepForward();
				Thread.Sleep(20);
			}
			Assert.IsTrue(cr.NormalCalls == 2 || cr.NormalCalls == 1);
			Assert.AreEqual(cr.StepFunctionCall, 1);
			Assert.AreEqual("ActionAsTask", cr.RealResult);
		}

		[TestMethod]
		public void ItShouldBePossibleToInvokeCoroutineAndWait()
		{

			var ct = new CoroutineTestThread();
			var subCr = new LocalInvokerCoroutine();
			subCr.ActionAsTask = true;

			var cr = new LocalInvokerCoroutine();
			cr.Coroutine = subCr;
			ct.Initialize();
			ct.AddCoroutine(cr);
			ct.StepForward();

			for (int i = 0; i < 12; i++)
			{
				ct.StepForward();
				Thread.Sleep(20);
			}

			Thread.Sleep(200);
			Assert.AreEqual(subCr.NormalCalls, 2);
			Assert.AreEqual(subCr.StepFunctionCall, 1);
			Assert.AreEqual("ActionAsTask", subCr.RealResult);


			Assert.AreEqual(cr.NormalCalls, 2);
			Assert.AreEqual(cr.StepFunctionCall, 0);
			Assert.AreEqual("ActionAsTaskTasked", cr.CoroutineResult);
		}

		[TestMethod]
		public void ItShouldBePossibleToInvokeCoroutineStatically()
		{
			var cr = new LocalInvokerCoroutine();
			cr.ActionAsTask = true;

			Coroutine.CallCoroutine(cr.Run());
			Thread.Sleep(200);
			Assert.AreEqual(cr.NormalCalls, 2);
			Assert.AreEqual(cr.StepFunctionCall, 1);
			Assert.AreEqual("ActionAsTask", cr.RealResult);
		}
	}

	#region Helper Classes


	public class LocalInvokerCoroutine : Coroutine
	{
		public string PossibleResult;
		public string RealResult;
		public string CoroutineResult;
		public bool ActionAsTask;

		public int NormalCalls;
		public LocalInvokerCoroutine Coroutine;

		public override IEnumerable<Step> Run()
		{
			NormalCalls++;
			var container = new Container();
			if (PossibleResult != null)
			{
				yield return InvokeLocalAndWait(StepFunctionResult, container);
				RealResult = container.RawData as string;
			}
			else if (Coroutine != null)
			{
				yield return InvokeCoroutineAndWait(Coroutine);
				CoroutineResult = Coroutine.RealResult+"Tasked";
			}
			else if (ActionAsTask)
			{
				string result = null;
				yield return InvokeAsTaskAndWait(() =>
				{
					StepFunctionCall++;
					System.Threading.Thread.Sleep(100);
					result = "ActionAsTask";
				});
				RealResult = result + "";
			}
			else
			{
				yield return InvokeLocalAndWait(StepFunction);
			}
			NormalCalls++;
			yield return Step.Current;
		}

		public IEnumerable<Step> StepFunction()
		{
			for (int i = 0; i < 10; i++)
			{
				StepFunctionCall++;
				yield return Step.Current;
			}
		}

		public IEnumerable<string> StepFunctionResult()
		{
			for (int i = 0; i < 10; i++)
			{
				StepFunctionCall++;
				yield return null;
			}
			yield return PossibleResult;
		}

		public int StepFunctionCall;

		public LocalInvokerCoroutine(string result = null)
		{
			PossibleResult = result;
		}

		public override void OnError(Exception ex)
		{

		}
	}

	#endregion
}
