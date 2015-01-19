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

	public class CoroutinesManagerTest_NotWaiting
	{
		#region Coroutines not waiting

		public IEnumerable<ICoroutineResult> NotWaitingWaitForReturnValue(int waitCount)
		{
			while (waitCount > 0)
			{
				yield return CoroutineResult.Wait;
				waitCount--;
			}
			yield return CoroutineResult.Return("RESULT");
		}

		public IEnumerable<ICoroutineResult> NotWaitingCoroutineWaitingForSingleResult(int waitCount = 1)
		{
			yield return CoroutineResult.Wait;
			yield return CoroutineResult.RunAndGetResult(NotWaitingWaitForReturnValue(waitCount),"NotWaitingWaitForReturnValue")
				.OnComplete<string>((r) =>
				{
					_coroutineSingleResult = r;
				});
		}

		

		[TestMethod]
		public void NotWaitingItShouldBePossibleToWaitForCoroutineCallingFunctionThatWait()
		{
			const int items = 10;
			const int itemsAndWait = items*2;
			var coroutine = new Mock<ICoroutineThread>();
			coroutine.Setup(a => a.Execute())
				.Returns(NotWaitingCoroutineWaitingForSingleResult(items));

			var target = new CoroutinesManager();
			target.TestInitialize();

			target.StartCoroutine(coroutine.Object);
			target.TestRun(itemsAndWait);

			Assert.AreEqual("RESULT", _coroutineSingleResult);
		}


		[TestMethod]
		public void NotWaitingItShouldBePossibleToWaitForEnEntireForEach()
		{
			_notWaitingStarted = false;
			_notWaitingCompleted = false;
			_coroutineResultsCount = 0;
			var coroutine = new Mock<ICoroutineThread>();
			coroutine.Setup(a => a.Execute())
				.Returns(NotWaitingForAllResults(10));

			var target = new CoroutinesManager();
			target.TestInitialize();
			var rtt = new RunnerForTest(target);

			target.StartCoroutine(coroutine.Object);

			rtt.RunCycle(); //Coroutine initialized
			Assert.IsTrue(_notWaitingStarted);
			rtt.RunCycle(2); //Coroutine started
			Assert.IsTrue(_notWaitingCompleted);
			Assert.AreEqual(0, _coroutineResultsCount);

			rtt.RunCycle(50); //Coroutine started


			Assert.AreEqual(10, _coroutineResultsCount);
		}



		public IEnumerable<ICoroutineResult> GetAllItems(int waitCount)
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

		bool _notWaitingStarted;
		bool _notWaitingCompleted;
		public IEnumerable<ICoroutineResult> NotWaitingForAllResults(int waitCount = 1)
		{
			_notWaitingStarted = true;
			_completed = false;
			var result = 0;
			yield return CoroutineResult.Wait;
			yield return CoroutineResult.ForEachItem(GetAllItems(waitCount),"GetAllItems")
				.Do<int>((r) =>
				{
					result++;
					return true;
				})
				.OnComplete(() =>
				{
					_coroutineResultsCount = result;
				})
				.WithTimeout(TimeSpan.FromDays(10));
			_notWaitingCompleted = true;
		}


		public IEnumerable<ICoroutineResult> NotWaitingForTask()
		{
			_notWaitingStarted = true;
			yield return CoroutineResult.Wait;
			yield return CoroutineResult.RunTask(Task.Factory.StartNew(() =>
				{
					_taskStarted = true;
					Thread.Sleep(100);
					_taskRun = true;
				}),"NotWaitingForTask")
				.OnComplete(() =>
				{
					_completedTaks = true;
				});
			_notWaitingCompleted = true;
		}

		bool _completedTaks;
		[TestMethod]
		public void NotWaitingItShouldBePossibleToWaitForATaskToComplete()
		{
			_notWaitingCompleted = false;
			_notWaitingStarted = false;
			_taskRun = false;
			_taskStarted = false;
			_completedTaks = false;
			_exception = null;
			var coroutine = new Mock<ICoroutineThread>();
			coroutine.Setup(a => a.Execute())
				.Returns(NotWaitingForTask());

			var target = new CoroutinesManager();
			target.TestInitialize();
			var rtt = new RunnerForTest(target);

			target.StartCoroutine(coroutine.Object);

			rtt.RunCycle(); //Coroutine initialized
			Assert.IsTrue(_notWaitingStarted);
			rtt.RunCycle(); //Coroutine started
			Thread.Sleep(50);
			rtt.RunCycle();
			Assert.IsTrue(_notWaitingCompleted);


			Assert.IsTrue(_taskStarted);
			Assert.IsNull(_exception);
			Assert.IsFalse(_completedTaks);

			rtt.RunCycleFor(150);
			Assert.IsTrue(_taskStarted);
			Assert.IsNull(_exception);
			Assert.IsTrue(_completedTaks);
		}


		private Exception _exception;
		private string _coroutineSingleResult;
		private bool _taskRun;
		private bool _taskStarted;
		private bool _completed;
		private int _coroutineResultsCount;

		public IEnumerable<ICoroutineResult> NotWaitingHavingTimeout()
		{
			_notWaitingStarted = true;
			yield return CoroutineResult.Wait;
			yield return CoroutineResult.RunTask(Task.Factory.StartNew(() =>
					{
						_taskStarted = true;
						Thread.Sleep(150);
						_taskRun = true;
					}),"NotWaitingHavingTimeout")
				.OnComplete(() =>
				{
					_completedTaks = true;
				})
				.WithTimeout(100)
				.OnError((e) =>
				{
					_exception = e;
					return true;
				});
			_notWaitingCompleted = true;
		}

		[TestMethod]
		public void NotWaitingItShouldBePossibleToWaitForATaskToCompleteWithTimeoutError()
		{
			_notWaitingCompleted = false;
			_notWaitingStarted = false;
			_taskRun = false;
			_taskStarted = false;
			_completedTaks = false;
			_exception = null;
			var coroutine = new Mock<ICoroutineThread>();
			coroutine.Setup(a => a.Execute())
				.Returns(NotWaitingHavingTimeout());

			var target = new CoroutinesManager();
			target.TestInitialize();
			var rtt = new RunnerForTest(target);

			target.StartCoroutine(coroutine.Object);

			rtt.RunCycle(); //Coroutine initialized
			Assert.IsTrue(_notWaitingStarted);
			rtt.RunCycle(2); //Coroutine started
			Assert.IsTrue(_notWaitingCompleted);


			Assert.IsTrue(_taskStarted);
			Assert.IsNull(_exception);
			Assert.IsFalse(_completedTaks);

			rtt.RunCycleFor(150);
			Assert.IsTrue(_taskStarted);
			Assert.IsNotNull(_exception);
			Assert.IsFalse(_completedTaks);
		}

		public IEnumerable<ICoroutineResult> NotWaitingCoroutineWaitingHavingTimeoutNotExploding()
		{
			_taskRun = false;
			_taskStarted = false;
			_completed = false;
			_completed = false;
			yield return CoroutineResult.Wait;
			yield return CoroutineResult.RunTask(Task.Factory.StartNew(() =>
			{
				_taskStarted = true;
				Thread.Sleep(100);
				_taskRun = true;
			}),"NotWaitingCoroutineWaitingHavingTimeoutNotExploding")
				.OnComplete(() =>
				{
					_completed = true;
				})
				.WithTimeout(1000);
		}

		[TestMethod]
		public void NotWaitingItShouldBePossibleToWaitForATaskToCompleteWithTimeout()
		{
			var coroutine = new Mock<ICoroutineThread>();
			coroutine.Setup(a => a.Execute())
				.Returns(NotWaitingCoroutineWaitingHavingTimeoutNotExploding());
			coroutine.Setup(o => o.OnError(It.IsAny<Exception>())).Returns(true);

			var target = new CoroutinesManager();
			target.TestInitialize();

			target.StartCoroutine(coroutine.Object);
			Task.Factory.StartNew(() => target.TestRun(3));
			Thread.Sleep(300);
			target.TestRun();

			coroutine.Verify(a => a.OnError(It.IsAny<Exception>()), Times.Never);
			Assert.IsTrue(_taskStarted);
			Assert.IsTrue(_taskRun);
			Assert.IsTrue(_completed);
		}
		#endregion
		
	}
}