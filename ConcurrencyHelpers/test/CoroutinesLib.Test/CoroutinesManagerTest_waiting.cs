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
using System.Threading.Tasks;
using CoroutinesLib.Shared;
using CoroutinesLib.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CoroutinesLib.Test
{
	[TestClass]
	public class CoroutinesManagerTest_Waiting
	{
		#region Coroutines waiting
		public IEnumerable<ICoroutineResult> WaitAndReturnSingleValue(int waitCount)
		{
			while (waitCount > 0)
			{
				yield return CoroutineResult.Wait;
				waitCount--;
			}
			yield return CoroutineResult.Return("RESULT");
		}

		public string _coroutineSingleResult;
		public IEnumerable<ICoroutineResult> CoroutineWaitingForSingleResult(int waitCount = 1)
		{
			string result = null;
			yield return CoroutineResult.Wait;
			yield return CoroutineResult.RunAndGetResult(WaitAndReturnSingleValue(waitCount),"WaitAndReturnSingleValue")
				.OnComplete<string>((r) =>
				{
					result = r;
				}).AndWait();
			_coroutineSingleResult = result;
		}


		public IEnumerable<ICoroutineResult> ReturnSeveralItemsAndWaitInBetween(int waitCount)
		{
			int results = 0;
			while (waitCount > 0)
			{
				yield return CoroutineResult.Wait;
				yield return CoroutineResult.YieldReturn(results);
				waitCount--;
				results++;
			}
		}

		public int _coroutineResultsCount;
		public int _partialCoroutineResultsCount;
		public bool _completed = false;
		public IEnumerable<ICoroutineResult> CoroutineWaitingForSeveralItems(int waitCount = 1)
		{
			_coroutineResultsCount = 0;
			_completed = false;
			var result = 0;
			yield return CoroutineResult.Wait;
			yield return CoroutineResult.ForEachItem(ReturnSeveralItemsAndWaitInBetween(waitCount),"ReturnSeveralItemsAndWaitInBetween")
				.Do((r) =>
				{
					Console.Write(" _");
					_partialCoroutineResultsCount++;
					result++;
					return true;
				})
				.OnComplete(() =>
				{

					Console.WriteLine(" X");
					_completed = true;
				})
				.WithTimeout(TimeSpan.FromDays(10))
				.AndWait();
			Console.WriteLine("U");
			if (_completed) _coroutineResultsCount = result;
		}

		[TestMethod]
		public void ItShouldBePossibleToWaitForCoroutineCallingFunctionThatWait()
		{
			var coroutine = new Mock<ICoroutineThread>();
			coroutine.Setup(a => a.Execute())
				.Returns(CoroutineWaitingForSingleResult(10));

			var target = new CoroutinesManager();
			target.TestInitialize();

			target.StartCoroutine(coroutine.Object);
			target.TestRun(20);

			Assert.AreEqual("RESULT", _coroutineSingleResult);
		}


		[TestMethod]
		public void ItShouldBePossibleToWaitForEnEntireForEach()
		{
			_completed = false;
			var coroutine = new Mock<ICoroutineThread>();
			coroutine.Setup(a => a.Execute())
				.Returns(CoroutineWaitingForSeveralItems(10));

			var target = new CoroutinesManager();
			target.TestInitialize();

			target.StartCoroutine(coroutine.Object);
			target.TestRun(3);	//Initialize the call
			target.TestRun(20);	//Set results

			Assert.AreEqual(10, _partialCoroutineResultsCount);
			Assert.IsTrue(_completed);

			target.TestRun(2);	//Copy the completed
			Assert.AreEqual(10, _coroutineResultsCount);
		}


		[TestMethod]
		public void WaitForTaskToComplete()
		{
			const int waitTime = 100;
			var coroutine = new Mock<ICoroutineThread>();
			coroutine.Setup(a => a.Execute())
				.Returns(CoroutineWaitingForTask(waitTime));

			var target = new CoroutinesManager();
			target.TestInitialize();
			var rft = new RunnerForTest(target);

			target.StartCoroutine(coroutine.Object);
			target.TestRun();	//Initialize
			rft.RunCycleFor(waitTime*2);
			target.TestRun();	//CleanUp

			Assert.IsTrue(_taskStarted);
			Assert.IsTrue(_taskRun);
			Assert.IsTrue(_completed);
		}

		private bool _taskRun;
		private bool _taskStarted;
		public IEnumerable<ICoroutineResult> CoroutineWaitingForTask(int wait = 100)
		{
			_taskRun = false;
			_taskStarted = false;
			_completed = false;
			var completed = false;
			yield return CoroutineResult.Wait;
			yield return CoroutineResult.RunTask(Task.Factory.StartNew(() =>
			{
				_taskStarted = true;
				Thread.Sleep(wait);
				_taskRun = true;
			}),"CoroutineWaitingForTask")
				.OnComplete(() =>
				{
					completed = true;
				}).AndWait();

			if (completed) _completed = true;
		}

		public IEnumerable<ICoroutineResult> CoroutineWaitingHavingTimeout()
		{
			_taskRun = false;
			_taskStarted = false;
			_completed = false;
			var completed = false;
			yield return CoroutineResult.Wait;
			yield return CoroutineResult.RunTask(Task.Factory.StartNew(() =>
			{
				_taskStarted = true;
				Thread.Sleep(300);
				_taskRun = true;
			}),"CoroutineWaitingForTask")
				.OnComplete(() =>
				{
					completed = true;
				})
				.WithTimeout(10)
				.AndWait();

			if (completed) _completed = true;
		}

		[TestMethod]
		public void ItShouldBePossibleToWaitForATaskToCompleteWithTimeoutError()
		{
			var coroutine = new Mock<ICoroutineThread>();
			coroutine.Setup(a => a.Execute())
				.Returns(CoroutineWaitingHavingTimeout());
			coroutine.Setup(o => o.OnError(It.IsAny<Exception>())).Returns(true);

			var target = new CoroutinesManager();
			target.TestInitialize();
			var rft = new RunnerForTest(target);

			target.StartCoroutine(coroutine.Object);
			target.TestRun();
			rft.RunCycleFor(200);
			target.TestRun();

			Assert.IsTrue(_taskStarted);
			Assert.IsFalse(_taskRun);

			coroutine.Verify(a => a.OnError(It.IsAny<Exception>()), Times.Once);
		}

		public IEnumerable<ICoroutineResult> CoroutineWaitingHavingTimeoutNotExploding()
		{
			_taskRun = false;
			_taskStarted = false;
			_completed = false;
			var completed = false;
			yield return CoroutineResult.Wait;
			yield return CoroutineResult.RunTask(Task.Factory.StartNew(() =>
			{
				_taskStarted = true;
				Thread.Sleep(100);
				_taskRun = true;
			}),"CoroutineWaitingHavingTimeoutNotExploding")
				.OnComplete(() =>
				{
					completed = true;
				})
				.WithTimeout(1000)
				.AndWait();

			if (completed) _completed = true;
		}

		[TestMethod]
		public void ItShouldBePossibleToWaitForATaskToCompleteWithinTimeout()
		{

			_taskStarted = false;
			_taskRun = false;
			_completed = false;
			var coroutine = new Mock<ICoroutineThread>();

			coroutine.Setup(a => a.Execute())
				.Returns(CoroutineWaitingHavingTimeoutNotExploding());

			var target = new CoroutinesManager();
			target.TestInitialize();
			var rft = new RunnerForTest(target);

			target.StartCoroutine(coroutine.Object);
			target.TestRun();
			rft.RunCycleFor(200);
			target.TestRun();

			Assert.IsTrue(_taskStarted);
			Assert.IsTrue(_taskRun);
			Assert.IsTrue(_completed);
			coroutine.Verify(a => a.OnError(It.IsAny<Exception>()), Times.Never);
		}
		#endregion

	}
}