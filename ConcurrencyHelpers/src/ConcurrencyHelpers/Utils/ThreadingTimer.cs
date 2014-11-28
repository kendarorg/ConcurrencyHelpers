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
using System.Threading;
using ConcurrencyHelpers.Interfaces;
using Timer = System.Threading.Timer;

namespace ConcurrencyHelpers.Utils
{
	public class ThreadingTimer : ITimer
	{
		private int _period;
		private int _delay;
		private Timer _timer;
		private ElapsedTimerEventHandler _onIntervalElapsed;
		private ConcurrentInt64 _timesRun;
		private readonly ConcurrentInt64 _running;
		private bool _blockOverlap;

		public int Period
		{
			get
			{
				return _period;
			}
		}

		public ThreadingTimer(int period = 0, int delay = 0)
		{
			_delay = 0;
			if (period <= 0) throw new ArgumentException("No negative or zero allowed", "period");
			if (delay < 0) throw new ArgumentException("No negative or zero allowed", "delay");

			if (delay > 0) _delay = delay;
			_period = period;
			_delay = delay;
			_timer = new Timer(OnTimerElapsed, this, Timeout.Infinite, Timeout.Infinite);
			_timesRun = new ConcurrentInt64();
			_running = new ConcurrentInt64();
		}

		public int TimesRun
		{
			get { return (int)_timesRun.Value; }
		}

		public bool Running
		{
			get { return _running.Value == 1; }
		}

		public void Start(int period = 0, int delay = 0)
		{
			_blockOverlap = false;
			if (period < 0) throw new ArgumentException("No negative or zero allowed", "period");
			if (delay < 0) throw new ArgumentException("No negative or zero allowed", "delay");

			if (period > 0) _period = period;
			if (delay > 0) _delay = delay;
			Stop();
			_timer.Change(_delay, _period);
			_running.Value = 1;
		}

		public void Stop()
		{
			_running.Value = 0;
			_timer.Change(Timeout.Infinite, Timeout.Infinite);
		}

		private void OnTimerElapsed(object sender)
		{
			if (_blockOverlap) return;
			_blockOverlap = true;
			if (_running.Value == 0) return;
			_timesRun++;
			if (_onIntervalElapsed != null)
			{
				_onIntervalElapsed(sender, new ElapsedTimerEventArgs(DateTime.Now));
			}

			_blockOverlap = false;
		}

		public event ElapsedTimerEventHandler Elapsed
		{
			add
			{
				_onIntervalElapsed += value;
			}
			remove
			{
				if (_onIntervalElapsed != null)
				{
					// ReSharper disable once DelegateSubtraction
					_onIntervalElapsed -= value;
				}
			}
		}

		~ThreadingTimer()
		{
			// Finalizer calls Dispose(false)
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				// free managed resources
				if (_timer != null)
				{
					_timer.Dispose();
					_timer = null;
				}
			}
		}
	}
}
