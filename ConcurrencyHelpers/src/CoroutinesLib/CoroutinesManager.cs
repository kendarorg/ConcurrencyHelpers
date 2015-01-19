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


using ConcurrencyHelpers.Containers;
using ConcurrencyHelpers.Utils;
using CoroutinesLib.Internals;
using CoroutinesLib.Internals.MessageBus;
using CoroutinesLib.Shared;
using CoroutinesLib.Shared.Enums;
using CoroutinesLib.Shared.Exceptions;
using CoroutinesLib.Shared.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace CoroutinesLib
{
	public class CoroutinesManager : ICoroutinesManager, ILoggable
	{
		internal class CoroutineQueueContent
		{
			public CoroutineQueueContent(ICoroutineThread coroutine, Action<Exception> onError)
			{
				Coroutine = coroutine;
				OnError = onError;
			}
			public ICoroutineThread Coroutine { get; private set; }
			public Action<Exception> OnError { get; private set; }
		}
		private const int PAUSE_WAIT_MS = 10;
		private readonly Thread _thread;
		private readonly ConcurrentQueue<CoroutineQueueContent> _coroutinesQueue;
		private readonly List<CoroutineInstance> _running;

		private ConcurrentInt64 _status = new ConcurrentInt64((long)RunningStatus.None);
		private readonly LockFreeItem<string> _actionRequested = new LockFreeItem<string>(null);
		private readonly LockFreeItem<object> _actionRequestedResult = new LockFreeItem<object>(null);

		public void StartCoroutine(ICoroutineThread coroutineThreadToStart, Action<Exception> onError = null)
		{
			var loggable = coroutineThreadToStart as ILoggable;
			if (loggable != null)
			{
				loggable.Log = Log;
			}
			_coroutinesQueue.Enqueue(new CoroutineQueueContent(coroutineThreadToStart,onError));
		}

		public IMessageBus MessageBus { get; private set; }

		public RunningStatus Status
		{
			get { return (RunningStatus)_status.Value; }
			private set { _status = (long)value; }
		}

		/// <summary>
		/// This must be checked by the coroutines that use external communcation medi a to prevent as
		/// much as possible being interrupted before a recycle or shutdown
		/// </summary>
		public bool AllowIncomingMessage { get; set; }

		public List<string> ListRunningStatus()
		{
			_actionRequested.Data = "ListRunningStatus";

			while (_actionRequested.Data == "ListRunningStatus")
			{
				Thread.Sleep(250);
			}
			return _actionRequestedResult.Data as List<string>;
		}

		public IEnumerable<ICoroutineThread> ListCoroutines()
		{
			_actionRequested.Data = "ListCoroutines";

			while (_actionRequested.Data == "ListCoroutines")
			{
				Thread.Sleep(250);
			}
			return _actionRequestedResult.Data as IEnumerable<ICoroutineThread>;
		}

		public int ThreadsCount { get; set; }

		public CoroutinesManager()
		{
			ThreadsCount = 1;
			Log = NullLogger.Create();
			MessageBus = new MessageBus();
			_coroutinesQueue = new ConcurrentQueue<CoroutineQueueContent>();
			_running = new List<CoroutineInstance>();
			_thread = new Thread(Run);
		}

		private void Run()
		{
			try
			{
				Status = RunningStatus.Running;

				var sw = new Stopwatch();
				while (Status.Is(RunningStatus.Running))
				{
					sw.Start();
					OnCycle();
					OnEndOfCycle(sw.ElapsedMilliseconds);
					sw.Reset();
				}
				if (Status.Is(RunningStatus.Stopping))
				{
					HandleStopping(new Queue<CoroutineInstance>());
					Status = RunningStatus.Stopped;
				}
			}
			catch (ThreadAbortException)
			{
				Log.Warning("CoroutinesManager aborted");
				Status = RunningStatus.Aborted;
				Thread.ResetAbort();
			}
		}

		internal void TestInitialize()
		{
			Status = RunningStatus.Running;
		}

		internal bool TestRunWhenPaused(int count = 1)
		{
			return TestRun(count, true);
		}

		internal bool TestRun(int count = 1, bool considerPause = false)
		{
			while (count > 0)
			{
				if (!Status.Is(RunningStatus.Stopped))
				{
					OnCycle();
					if (considerPause)
					{
						OnEndOfCycle(0);
					}
					if (count <= 0) return true;
				}
				else if (Status.Is(RunningStatus.Stopped))
				{
					return false;
				}

				count--;
			}
			return false;
		}

		private void OnEndOfCycle(long elapsedMilliseconds)
		{
			if (Status.Is(RunningStatus.Paused))
			{
				Thread.Sleep(PAUSE_WAIT_MS);
			}
			else
			{
				Thread.Sleep(1);
			}
		}

		private void OnCycle()
		{
			if (_actionRequested.Data != null)
			{
				HandleActionRequested(_actionRequested.Data);
			}
			var removeIds = new Queue<CoroutineInstance>(); ;
			if (Status.Is(RunningStatus.Stopping))
			{
				HandleStopping(removeIds);
			}
			else
			{
				if (Status.Is(RunningStatus.Paused)) return;
				HandleCoroutines(removeIds);
			}
			RemoveTerminatedCoroutines(removeIds);
		}

		private void RemoveTerminatedCoroutines(Queue<CoroutineInstance> removeIds)
		{
			while (removeIds.Count > 0)
			{
				var instanceToRemove = removeIds.Dequeue();
				//if (_running.Count > index)
				//{
				var index = _running.FindIndex((a) => ReferenceEquals(a, instanceToRemove));

				var coroutineToRemove = instanceToRemove.Coroutine;
				var enumToRemove = instanceToRemove.Enumerators;
				MessageBus.Unregister(coroutineToRemove);
				//DebugLog.Info("Removing "+coroutineToRemove.InstanceName);
				try
				{
					coroutineToRemove.OnDestroy();
					enumToRemove.Dispose();
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex);
				}
				_running.RemoveAt(index);
				//}
			}
		}

		private void HandleCoroutines(Queue<CoroutineInstance> removeIds)
		{
			ICoroutineThread newCoroutineThread;
			CoroutineQueueContent newCoroutineContent;
			
			while (_coroutinesQueue.TryDequeue(out newCoroutineContent))
			{
				newCoroutineThread = newCoroutineContent.Coroutine;
				MessageBus.Register(newCoroutineThread);
				var instance = new CoroutineInstance(newCoroutineThread, newCoroutineContent.OnError);
				_running.Add(instance);
			}

			for (int index = 0; index < _running.Count; index++)
			{
				if (Status.Is(RunningStatus.Paused)) return;
				var instance = _running[index];

				try
				{
					if (!instance.IsInitialized)
					{
						instance.Initialize(Log);
					}
					var enumerator = instance.Enumerators;

					if (!enumerator.MoveNext())
					{
						removeIds.Enqueue(instance);
					}
					else
					{
						var current = enumerator.Current as FluentResultBuilder;
						if (current != null && !current.Type.HasFlag(FluentResultType.Waiting))
						{
							StartCoroutine(current.AsCoroutine());
						}
					}
				}
				catch (Exception ex)
				{
					OnError(ex, removeIds, index);
				}
				if (Status.Is(RunningStatus.Paused))
				{
					break;
				}
			}
		}

		private void HandleStopping(Queue<CoroutineInstance> removeIds)
		{
			var ex = new ManagerStoppedException("Manager Stopped");
			for (int index = 0; index < _running.Count; index++)
			{
				OnError(ex, removeIds, index, true);
			}
			Status = RunningStatus.Stopped;
		}

		private void HandleActionRequested(string data)
		{
			if (data == null) return;
			if (data == "ListCoroutines")
			{
				var result = new List<ICoroutineThread>();
				foreach (var c in _running)
				{
					result.Add(c.Coroutine);
				}
				_actionRequestedResult.Data = result;
			}
			else if (data == "ListRunningStatus")
			{
				var result = new List<string>();
				foreach (var c in _running)
				{
					result.Add(c.BuildRunningStatus());
				}
				_actionRequestedResult.Data = result;
			}
			_actionRequested.Data = null;
		}

		private void OnError(Exception ex, Queue<CoroutineInstance> removeIds, int index, bool forceTerminate = false)
		{
			var instance = _running[index];
			var coroutine = instance.Coroutine;

			bool shouldTerminate;
			try
			{
				shouldTerminate = coroutine.OnError(ex);
				if (instance.OnError != null)
				{
					instance.OnError(ex);
				}
			}
			catch (Exception)
			{
				shouldTerminate = true;
			}


			if (!instance.IsInitialized)
			{
				shouldTerminate = true;
			}

			if (forceTerminate)
			{
				shouldTerminate = true;
			}

			if (shouldTerminate)
			{
				removeIds.Enqueue(instance);
			}
			else
			{
				instance.Initialize(Log);
			}
		}

		public void Start()
		{
			_thread.Start();
		}

		public void Stop()
		{
			Status = RunningStatus.Stopping;
		}

		public void Abort()
		{
			_thread.Abort();
			Status = RunningStatus.Aborted;
		}

		public void Pause()
		{
			Status = RunningStatus.Paused;
		}


		public void Restart()
		{
			if (Status != RunningStatus.Paused)
			{
				throw new RunningStatusException("Can restart only paused runners.");
			}
			Status = RunningStatus.Running;
		}

		public ILogger Log { get; set; }
	}
}
