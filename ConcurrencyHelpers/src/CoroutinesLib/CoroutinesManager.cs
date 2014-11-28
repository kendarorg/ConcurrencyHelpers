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
		private const int PAUSE_WAIT_MS = 10;
		private readonly Thread _thread;
		private readonly ConcurrentQueue<ICoroutineThread> _coroutinesQueue;
		private readonly List<CoroutineInstance> _running;

		private ConcurrentInt64 _status = new ConcurrentInt64((long)RunningStatus.None);
		private readonly LockFreeItem<string> _actionRequested = new LockFreeItem<string>(null);
		private readonly LockFreeItem<object> _actionRequestedResult = new LockFreeItem<object>(null);

		public void StartCoroutine(ICoroutineThread coroutineThreadToStart)
		{
			var loggable = coroutineThreadToStart as ILoggable;
			if (loggable != null)
			{
				loggable.Log = Log;
			}
			_coroutinesQueue.Enqueue(coroutineThreadToStart);
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
			_coroutinesQueue = new ConcurrentQueue<ICoroutineThread>();
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

			while (_coroutinesQueue.TryDequeue(out newCoroutineThread))
			{
				MessageBus.Register(newCoroutineThread);
				var instance = new CoroutineInstance(newCoroutineThread);
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
