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
