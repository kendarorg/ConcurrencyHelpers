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

		public void StartCoroutine(ICoroutineThread coroutineThreadToStart)
		{
			_runner.StartCoroutine(coroutineThreadToStart);
		}

		public void RunCycleFor(int milliseconds,Action onEach=null)
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
			return ((ICoroutinesManager) _runner).ListCoroutines();
		}

		public ILogger Log { get; set; }
	}
}
