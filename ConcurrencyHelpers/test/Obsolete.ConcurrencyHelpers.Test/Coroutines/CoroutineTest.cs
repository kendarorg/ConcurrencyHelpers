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
