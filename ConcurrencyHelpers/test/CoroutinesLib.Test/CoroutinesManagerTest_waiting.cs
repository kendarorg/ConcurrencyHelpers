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