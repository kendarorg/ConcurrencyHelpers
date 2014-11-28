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
using System.Threading.Tasks;
using System.Timers;
using ConcurrencyHelpers.Interfaces;
using Timer = System.Timers.Timer;
using System.Diagnostics;

namespace ConcurrencyHelpers.Utils
{
	public class SystemTimer : ITimer
	{
		private int _period;
		private int _delay;
		private ConcurrentInt64 _timesRun;
		private Timer _timer;
		private ElapsedTimerEventHandler _onIntervalElapsed;
		private readonly ConcurrentInt64 _running;

		public int Period
		{
			get
			{
				return _period;
			}
		}

		public SystemTimer(int period = 0, int delay = 0)
		{
			_delay = 0;
			if (period <= 0) throw new ArgumentException("No negative or zero allowed", "period");
			if (delay < 0) throw new ArgumentException("No negative or zero allowed", "delay");

			if (delay > 0) _delay = delay;
			_period = period;
			_delay = delay;
			_timesRun = new ConcurrentInt64();
			_running = new ConcurrentInt64();
			_timer = new Timer(_period);
			_timer.Elapsed += OnTimerElapsed;
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
			if (period < 0) throw new ArgumentException("No negative or zero allowed", "period");
			if (delay < 0) throw new ArgumentException("No negative or zero allowed", "delay");

			if (period > 0) _period = period;
			if (delay > 0) _delay = delay;
			Stop();
			var stopWatch = new Stopwatch();
			stopWatch.Start();
			Task.Factory.StartNew(() =>
							 {
								 var sw = stopWatch.Elapsed.Milliseconds;
								 if (sw < _delay)
								 {
									 Thread.Sleep(_delay - sw);
								 }
								 stopWatch = null;
								 CallElapsed(this, new ElapsedTimerEventArgs(DateTime.Now));
								 _timesRun.Increment();
								 _timer.Interval = _period;
								 _timer.Start();
								 _running.Value = 1;
							 });
		}

		public void Stop()
		{
			_running.Value = 0;
			_timer.Stop();
		}

		private void OnTimerElapsed(object sender, ElapsedEventArgs e)
		{
			if (_running.Value == 0) return;
			_timesRun++;
			CallElapsed(sender, new ElapsedTimerEventArgs(e));
		}

		private void CallElapsed(object sender, ElapsedTimerEventArgs e)
		{
			if (_onIntervalElapsed != null)
			{
				_onIntervalElapsed(sender, e);
			}
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

		~SystemTimer()
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
