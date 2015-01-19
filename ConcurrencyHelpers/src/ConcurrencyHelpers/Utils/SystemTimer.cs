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
