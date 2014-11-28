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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using ConcurrencyHelpers.Containers;
using ConcurrencyHelpers.Utils;

namespace ConcurrencyHelpers.Coroutines
{

	[Obsolete]
	[ExcludeFromCodeCoverage]
	public class CoroutineThread : IValuesCollection, IDisposable
	{
		private CounterInt64 _status;
		private readonly List<CoroutineStatus> _coroutines;

		private readonly LockFreeQueue<ICoroutine> _coroutinesQueue;
		private readonly Thread _thread;
		private readonly ManualResetEvent _pauseVerifier;
		private CounterInt64 _cycleMaxMs;
		private readonly int _affinity;
		private readonly Dictionary<string, object> _values;
		private readonly CounterInt64 _startedCoroutines;
		private readonly CounterInt64 _terminatedCoroutines;
		private readonly CultureInfo _currentCulture;

		public CoroutineThread(int cycleMaxMs = 10, int affinity = -1)
		{
			_startedCoroutines = new CounterInt64();
			_terminatedCoroutines = new CounterInt64();
			Status = (int)CoroutineThreadStatus.Stopped;
			_values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
			_cycleMaxMs = cycleMaxMs;
			_affinity = affinity;
			_coroutines = new List<CoroutineStatus>();
			_coroutinesQueue = new LockFreeQueue<ICoroutine>();
			_thread = new Thread(StartThread);
			_pauseVerifier = new ManualResetEvent(false);
			_currentCulture = Thread.CurrentThread.CurrentCulture;
		}

		public virtual void AddCoroutine(params ICoroutine[] coroutines)
		{
			foreach (var coroutine in coroutines)
			{
				_coroutinesQueue.Enqueue(coroutine);
			}
		}

		public int CycleMaxMs
		{
			get
			{
				return (int)_cycleMaxMs;
			}
			set
			{
				_cycleMaxMs = value;
			}
		}

		public CoroutineThreadStatus Status
		{
			get
			{
				return (CoroutineThreadStatus)(int)_status;
			}
			protected set
			{
				_status = (int)value;
			}
		}

		public void Stop()
		{
			Status = CoroutineThreadStatus.Stopped;
		}

		public void Pause()
		{
			Status = CoroutineThreadStatus.Paused;
		}

		public void Start()
		{
			if (Status == CoroutineThreadStatus.Paused)
			{
				_pauseVerifier.Set();
			}
			else
			{
				if (_thread.ThreadState != System.Threading.ThreadState.Running)
				{
					_thread.Start();
				}
			}
		}

		public Action<Exception, ICoroutine> HandleError { get; set; }

		public long StartedCoroutines
		{
			get { return _startedCoroutines.Value; }
		}

		public long TerminatedCoroutines
		{
			get { return _terminatedCoroutines.Value; }
		}

		[DllImport("kernel32.dll")]
		public static extern int GetCurrentThreadId();

		[DllImport("kernel32.dll")]
		public static extern int GetCurrentProcessorNumber();

		private ProcessThread CurrentThread
		{
			get
			{
				int id = GetCurrentThreadId();
				return
						(from ProcessThread th in Process.GetCurrentProcess().Threads
						 where th.Id == id
						 select th).Single();
			}
		}

		private void StartThread()
		{
			Thread.BeginThreadAffinity();
			try
			{
				if ( _affinity >= 0 )
        {
            CurrentThread.ProcessorAffinity = new IntPtr ( _affinity );
        }
				// ReSharper disable once TooWideLocalVariableScope
				Status = CoroutineThreadStatus.Running;
				var stopWatch = new Stopwatch();
				while (Status != CoroutineThreadStatus.Stopped)
				{
					RunStep(stopWatch);
				}
			}
			finally
			{
				// reset affinity
				CurrentThread.ProcessorAffinity =new IntPtr(0xFFFF);
				Thread.EndThreadAffinity();
			}
		}

		protected void RunStep(Stopwatch stopWatch)
		{
			Thread.CurrentThread.CurrentCulture = _currentCulture;
			PauseWait();
			if (Status == CoroutineThreadStatus.Stopped) return;
			stopWatch.Reset();
			stopWatch.Start();
			foreach (var coroutineToAdd in _coroutinesQueue.Dequeue())
			{
				coroutineToAdd.Thread = this;
				coroutineToAdd.ShouldTerminate = false;
				_coroutines.Add(new CoroutineStatus { Instance = coroutineToAdd });
				_startedCoroutines.Increment();
			}
			for (int i = (_coroutines.Count - 1); i >= 0; i--)
			{
				PauseWait();

				var coroutineStatus = _coroutines[i];
				var coroutine = coroutineStatus.Instance;


				try
				{
					// ReSharper disable PossibleMultipleEnumeration
					if (coroutineStatus.Enumerator == null)
					{
						coroutineStatus.Enumerator = new CoroutineStack(coroutine.Run().GetEnumerator(), coroutine);
					}
					if (!coroutineStatus.Enumerator.MoveNext())
					{
						coroutine.ShouldTerminate = true;
					}
					// ReSharper restore PossibleMultipleEnumeration
				}
				catch (Exception ex)
				{
					if (HandleError != null)
					{
						try
						{
							HandleError(ex, coroutine);
						}
						catch (Exception)
						{

						}
					}
					coroutine.OnError(ex);
					if (coroutineStatus.Enumerator != null)
					{
						coroutineStatus.Enumerator.Reset();
					}
					coroutineStatus.Enumerator = null;
				}
				if (coroutine.ShouldTerminate)
				{
					_terminatedCoroutines.Increment();
					_coroutines.RemoveAt(i);
					// ReSharper disable once RedundantAssignment
					coroutine = null;
				}
			}
			stopWatch.Stop();
			var wait = (int)(CycleMaxMs - stopWatch.ElapsedMilliseconds);
			Thread.Sleep(wait > 1 ? wait : 0);
		}

		private void PauseWait()
		{
			if (Status == CoroutineThreadStatus.Paused)
			{
				_pauseVerifier.Reset();
				while (false == _pauseVerifier.WaitOne(1000))
				{
					if (Status == CoroutineThreadStatus.Stopped)
					{
						_pauseVerifier.Reset();
						return;
					}
				}
				Status = CoroutineThreadStatus.Running;
			}
		}

		~CoroutineThread()
		{
			// Finalizer calls Dispose(false)
			Dispose(false);
		}

		public virtual void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				// free managed resources
				Status = CoroutineThreadStatus.Stopped;
				_pauseVerifier.Reset();
			}
		}

		public void ClearValues()
		{
			_values.Clear();
		}

		public object this[string index]
		{
			get { return _values[index]; }
			set { _values[index] = value; }
		}

		public void RemoveValueAt(string index)
		{
			if (ContainsKey(index))
			{
				_values.Remove(index);
			}
		}

		public bool ContainsKey(string index)
		{
			return _values.ContainsKey(index);
		}
	}
}
