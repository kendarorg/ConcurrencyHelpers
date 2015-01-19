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
using System.Threading;
using System.Threading.Tasks;
using CoroutinesLib.Shared;
using CoroutinesLib.Shared.Enums;
using CoroutinesLib.Shared.Logging;

namespace CoroutinesLib.TestHelpers
{
	public sealed class RunnerForTest : ICoroutinesManager
	{
		private readonly CoroutinesManager _runner;

		public RunnerForTest()
		{
			RunnerFactory.Initialize();
			_runner = (CoroutinesManager)RunnerFactory.Create();
			_runner.TestInitialize();
		}

		public RunnerForTest(CoroutinesManager runner)
		{
			_runner = runner;
		}

		public void StartCoroutine(ICoroutineThread coroutineThreadToStart, Action<Exception> onError = null)
		{
			_runner.StartCoroutine(coroutineThreadToStart, onError);
		}

		public void RunCycleFor(int milliseconds, Action onEach = null)
		{
			var task = Task.Factory.StartNew(() =>
			{
				var sw = new Stopwatch();
				sw.Start();
				while (sw.ElapsedMilliseconds < milliseconds)
				{

					RunCycle();
					Thread.Sleep(1);
					if (onEach != null)
					{
						onEach();
					}
				}
			});
			Task.WaitAll(task);
		}

		public void RunCycle(int count = 1, bool considerPause = false)
		{
			_runner.TestRun(count, considerPause);
		}

		public IMessageBus MessageBus
		{
			get { return _runner.MessageBus; }
		}
		public void Start()
		{
			throw new System.NotImplementedException();
		}

		public void Stop()
		{
			_runner.Stop();
		}

		public void Pause()
		{
			_runner.Pause();
		}

		public void Abort()
		{
			_runner.Abort();
		}

		public void Restart()
		{
			_runner.Restart();
		}

		public RunningStatus Status
		{
			get { return _runner.Status; }
		}
		public bool AllowIncomingMessage
		{
			get { return _runner.AllowIncomingMessage; }
			set { _runner.AllowIncomingMessage = value; }
		}

		public List<string> ListRunningStatus()
		{
			return ((ICoroutinesManager)_runner).ListRunningStatus();
		}

		public IEnumerable<ICoroutineThread> ListCoroutines()
		{
			return ((ICoroutinesManager)_runner).ListCoroutines();
		}

		public ILogger Log { get; set; }
	}
}
