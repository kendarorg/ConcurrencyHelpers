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


using System.Threading;
using System.Threading.Tasks;
using CoroutinesLib.Shared;
using CoroutinesLib.Shared.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;

namespace CoroutinesLib.Test
{

	[TestClass]
	public class CoroutinesManagerTest
	{
		#region Simple Coroutines


		[TestMethod]
		public void RunnerShouldCallExecute()
		{
			var coroutine = new Mock<ICoroutineThread>();
			coroutine.Setup(a => a.Execute())
				.Returns(Execute());

			var target = new CoroutinesManager();
			target.TestInitialize();

			target.StartCoroutine(coroutine.Object);

			target.TestRun();

			coroutine.Verify(a => a.Execute(), Times.Once);
		}

		[TestMethod]
		public void WhenTerminatingShouldBeCalledOnDestroy()
		{
			var coroutine = new Mock<ICoroutineThread>();
			coroutine.Setup(a => a.Execute())
				.Returns(ExecuteAndTerminate);

			var target = new CoroutinesManager();
			target.TestInitialize();

			target.StartCoroutine(coroutine.Object);

			target.TestRun();

			coroutine.Verify(a => a.OnDestroy(), Times.Once);
		}

		[TestMethod]
		public void ExceptionOnFirstyCycleShouldBeHandledAndRoutineTerminatedForcibly()
		{
			var coroutine = new Mock<ICoroutineThread>();
			coroutine.Setup(a => a.Execute())
				.Returns(ExecuteAndThrowInstantly);

			var target = new CoroutinesManager();
			target.TestInitialize();

			target.StartCoroutine(coroutine.Object);

			target.TestRun();

			coroutine.Verify(a => a.OnError(It.IsAny<NotImplementedException>()), Times.Once);
			coroutine.Verify(a => a.OnDestroy(), Times.Once);
		}

		[TestMethod]
		public void ExceptionShouldNotTerminateCoroutineWhenOnErrorReturnFalse()
		{
			var coroutine = new Mock<ICoroutineThread>();
			coroutine.Setup(a => a.Execute())
				.Returns(ExecuteWithErrorAndContinue);

			var target = new CoroutinesManager();
			target.TestInitialize();

			target.StartCoroutine(coroutine.Object);
			_shouldThrowExceptionOnExecuteWithErrorAndContinue = true;
			target.TestRun(2);
			_shouldThrowExceptionOnExecuteWithErrorAndContinue = false;
			target.TestRun(2);

			coroutine.Verify(a => a.OnError(It.IsAny<NotImplementedException>()), Times.Once);
			coroutine.Verify(a => a.OnDestroy(), Times.Never);
		}

		[TestMethod]
		public void ExceptionShouldTerminateCoroutineWhenOnErrorReturnTrue()
		{
			var coroutine = new Mock<ICoroutineThread>();
			coroutine.Setup(a => a.Execute())
				.Returns(ExecuteWithErrorAndContinue);
			coroutine.Setup(a => a.OnError(It.IsAny<Exception>()))
				.Returns(true);

			var target = new CoroutinesManager();
			target.TestInitialize();

			target.StartCoroutine(coroutine.Object);
			_shouldThrowExceptionOnExecuteWithErrorAndContinue = true;
			target.TestRun(3);

			coroutine.Verify(a => a.OnError(It.IsAny<NotImplementedException>()), Times.Once);
			coroutine.Verify(a => a.OnDestroy(), Times.Once);
		}

		[TestMethod]
		public void StoppingRunnerShouldSendManagerStoppedExceptionToAllCoroutinesDestroyingEvenIfNotRequired()
		{
			var coroutine = new Mock<ICoroutineThread>();
			coroutine.Setup(a => a.Execute())
				.Returns(Execute(3));
			coroutine.Setup(a => a.OnError(It.IsAny<Exception>()))
				.Returns(false);


			var target = new CoroutinesManager();
			target.TestInitialize();

			target.StartCoroutine(coroutine.Object);
			target.TestRun();
			target.Stop();
			target.TestRun();

			coroutine.Verify(a => a.OnError(It.IsAny<ManagerStoppedException>()), Times.Once);
			coroutine.Verify(a => a.OnDestroy(), Times.Once);
		}

		[TestMethod]
		public void PausingRunnerShouldPauseAllCoroutines()
		{
			var coroutine = new Mock<ICoroutineThread>();
			coroutine.Setup(a => a.Execute())
				.Returns(Execute(10));

			var target = new CoroutinesManager();
			target.TestInitialize();

			target.StartCoroutine(coroutine.Object);
			target.TestRun();
			target.Pause();
			target.TestRunWhenPaused(10);
			target.Restart();
			target.TestRun();

			Assert.AreEqual(_cyclesRunSimpleExecute, 2);
			coroutine.Verify(a => a.OnDestroy(), Times.Never);
		}

		private bool _shouldThrowExceptionOnExecuteWithErrorAndContinue;
		private IEnumerable<ICoroutineResult> ExecuteWithErrorAndContinue()
		{
			yield return CoroutineResult.Wait;
			if (_shouldThrowExceptionOnExecuteWithErrorAndContinue)
			{
				throw new NotImplementedException();
			}
			yield return CoroutineResult.Wait;
		}

		private IEnumerable<ICoroutineResult> ExecuteAndThrowInstantly()
		{
			throw new NotImplementedException();
		}

		private IEnumerable<ICoroutineResult> ExecuteAndThrowAfterInitialization()
		{
			yield return CoroutineResult.Wait;
			throw new NotImplementedException();
		}


		private IEnumerable<ICoroutineResult> ExecuteAndTerminate()
		{
			yield break;
		}

		private int _cyclesRunSimpleExecute;
		private IEnumerable<ICoroutineResult> Execute(int cycles = 1)
		{
			_cyclesRunSimpleExecute = 0;
			while (cycles > 0)
			{
				_cyclesRunSimpleExecute++;
				yield return CoroutineResult.Wait;
				cycles--;
			}
		}
		#endregion

		#region Nested Coroutines

		private int _cyclesRunExecuteNested;
		private IEnumerable<ICoroutineResult> ExecuteNested(int cycles = 1)
		{
			var original = cycles;
			_cyclesRunExecuteNested = 0;
			while (cycles > 0)
			{
				_cyclesRunExecuteNested++;
				if (cycles == original / 2)
				{
					yield return CoroutineResult.Run(Execute(original), "Execute")
						.WithTimeout(TimeSpan.FromMinutes(10))
						.AndWait();
					cycles--;
				}
				else
				{
					yield return CoroutineResult.Wait;
					cycles--;
				}
			}
		}

		[TestMethod]
		public void NestedCallsToCoroutinesShouldWork()
		{
			_cyclesRunSimpleExecute = 0;
			_cyclesRunExecuteNested = 0;
			var coroutine = new Mock<ICoroutineThread>();
			coroutine.Setup(a => a.Execute())
				.Returns(ExecuteNested(10));

			var target = new CoroutinesManager();
			target.TestInitialize();

			target.StartCoroutine(coroutine.Object);

			target.TestRun(20);

			Assert.AreEqual(10, _cyclesRunSimpleExecute);
			Assert.AreEqual(10, _cyclesRunExecuteNested);
			coroutine.Verify(a => a.OnDestroy(), Times.Never);

			target.TestRun();
			coroutine.Verify(a => a.OnDestroy(), Times.Once);
		}

		[TestMethod]
		public void ExceptionInNestedCallShouldBeHandledByCaller()
		{
			var coroutine = new Mock<ICoroutineThread>();
			coroutine.Setup(a => a.Execute())
				.Returns(ExecuteNestedAndThrowInsideNested(10));

			var target = new CoroutinesManager();
			target.TestInitialize();

			target.StartCoroutine(coroutine.Object);
			target.TestRun(6);

			Assert.AreEqual(6, _cyclesRunExecuteNested);

			coroutine.Verify(a => a.OnError(It.IsAny<NotImplementedException>()));
			coroutine.Verify(a => a.OnDestroy(), Times.Never);
		}


		[TestMethod]
		public void ExceptionInNestedCallShouldNotTerminateWithOnErforFalse()
		{
			var coroutine = new Mock<ICoroutineThread>();
			coroutine.Setup(a => a.Execute())
				.Returns(ExecuteNestedAndThrowInsideNested(10));

			var target = new CoroutinesManager();
			target.TestInitialize();

			target.StartCoroutine(coroutine.Object);
			target.TestRun(6);

			Assert.AreEqual(6, _cyclesRunExecuteNested);

			coroutine.Verify(a => a.OnError(It.IsAny<NotImplementedException>()), Times.Once);
			coroutine.Verify(a => a.OnDestroy(), Times.Never);

			target.TestRun(6);
			coroutine.Verify(a => a.OnError(It.IsAny<NotImplementedException>()), Times.Exactly(2));
		}

		[TestMethod]
		public void ExceptionInNestedCallShouldTerminateWithOnErrorTrue()
		{
			var coroutine = new Mock<ICoroutineThread>();
			coroutine.Setup(a => a.Execute())
				.Returns(ExecuteNestedAndThrowInsideNested(10));
			coroutine.Setup(a => a.OnError(It.IsAny<Exception>()))
				.Returns(true);

			var target = new CoroutinesManager();
			target.TestInitialize();

			target.StartCoroutine(coroutine.Object);
			target.TestRun(6);

			Assert.AreEqual(6, _cyclesRunExecuteNested);

			coroutine.Verify(a => a.OnError(It.IsAny<NotImplementedException>()), Times.Once);
			coroutine.Verify(a => a.OnDestroy(), Times.Once);
		}




		private IEnumerable<ICoroutineResult> ExecuteNestedAndThrowInsideNested(int cycles = 1)
		{
			var original = cycles;
			while (cycles > 0)
			{
				_cyclesRunExecuteNested++;
				if (cycles == original / 2)
				{
					yield return CoroutineResult.Enumerable(ExecuteAndThrowInstantly(), "ExecuteAndThrowInstantly");
					cycles--;
				}
				else
				{
					yield return CoroutineResult.Wait;
					cycles--;
				}
			}
		}
		#endregion

		#region MissingStuffs

		[TestMethod]
		public void ItShoudlBePossibleToAbortTheManager()
		{
			Exception thrown = null;
			var coroutine = new Mock<ICoroutineThread>();
			coroutine.Setup(a => a.Execute())
				.Returns(Execute(int.MaxValue));
			coroutine.Setup(a => a.OnError(It.IsAny<Exception>()))
				.Returns((Exception ex) =>
				{
					thrown = ex;
					return false;
				});

			var target = new CoroutinesManager();

			target.StartCoroutine(coroutine.Object);

			target.Start();
			Thread.Sleep(200);
			target.Abort();
			Thread.Sleep(100);

			coroutine.Verify(a => a.Execute(), Times.AtLeastOnce);
			coroutine.Verify(a => a.OnError(It.IsAny<Exception>()), Times.Never);
			coroutine.Verify(a => a.OnDestroy(), Times.Never);
		}


		[TestMethod]
		public void ItShoudlBePossibleToStopTheManager()
		{
			Exception thrown = null;
			var coroutine = new Mock<ICoroutineThread>();
			coroutine.Setup(a => a.Execute())
				.Returns(Execute(int.MaxValue));
			coroutine.Setup(a => a.OnError(It.IsAny<Exception>()))
				.Returns((Exception ex) =>
				{
					thrown = ex;
					return false;
				});

			var target = new CoroutinesManager();

			target.StartCoroutine(coroutine.Object);

			target.Start();
			Thread.Sleep(200);
			target.Stop();
			Thread.Sleep(100);

			coroutine.Verify(a => a.Execute(), Times.AtLeastOnce);
			coroutine.Verify(a => a.OnError(It.IsAny<Exception>()), Times.Once);
			coroutine.Verify(a => a.OnError(It.IsAny<ManagerStoppedException>()), Times.Once);
			coroutine.Verify(a => a.OnDestroy(), Times.AtMostOnce);
		}
		#endregion

		#region Coroutines as Tasks

		private class ExeCoRoutine : CoroutineBase
		{
			public int Cycles = 0;
			public IEnumerator<ICoroutineResult> Enumerable;

			public override void Initialize()
			{
				
			}

			public override IEnumerable<ICoroutineResult> OnCycle()
			{
				if (!Enumerable.MoveNext())
				{
					yield return CoroutineResult.YieldBreak();
					TerminateElaboration();
				}
				else
				{
					Cycles++;
					yield return Enumerable.Current;
				}
			}

			public override void OnEndOfCycle()
			{

			}
		}

		[TestMethod]
		public void RunningACoroutineFromEverywhere()
		{
			var coroutine = new ExeCoRoutine();
			coroutine.Enumerable = Execute().GetEnumerator();

			var target = new CoroutinesManager();
			target.TestInitialize();
			RunnerFactory.Initialize(() => target);

			target.Start();
			var parent = Task.Run(() =>
			{
				var task = CoroutineResult.WaitForCoroutine(coroutine);
				task.Wait();
			});

			parent.Wait();
			target.Stop();

			Assert.AreEqual(1, coroutine.Cycles);
		}

		[TestMethod]
		public void RunningACoroutineFromEverywhereShouldPropagateExceptions()
		{
			var coroutine = new ExeCoRoutine();
			coroutine.Enumerable = ExecuteAndThrowAfterInitialization().GetEnumerator();

			var target = new CoroutinesManager();
			target.TestInitialize();
			RunnerFactory.Initialize(() => target);

			target.Start();
			var parent = Task.Run(() =>
			{
				var task = CoroutineResult.WaitForCoroutine(coroutine);
				task.Wait();
			});

			Exception expected = null;
			try
			{
				
				parent.Wait();
			}
			catch (Exception ex)
			{
				expected = ex;
			}
			target.Stop();

			Assert.IsNotNull(expected);
		}
		#endregion
	}
}
