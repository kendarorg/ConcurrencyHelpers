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
using System.Collections.ObjectModel;
using System.Diagnostics;
using ConcurrencyHelpers.Containers;
using ConcurrencyHelpers.Containers.Asyncs;
using ConcurrencyHelpers.Interfaces;
using ConcurrencyHelpers.Utils;

namespace ConcurrencyHelpers.Monitor
{
	public static class PerfMon
	{
		private static Dictionary<string, BaseMetric> _beforeStart = new Dictionary<string, BaseMetric>();
		private static bool _started = false;
		private static LockFreeItem<ReadOnlyCollection<BaseMetric>> _accessibleData;
		private static CollectedPerfDataEventHandler _onDataCollected;
		public const string PERFMON_RUNS = "PerfMon.Runs";
		private static SystemTimer _timer;
		private static readonly AsyncLockFreeDictionary<string, BaseMetric> _perfData;
		private static bool _overlapping = false;
		private static readonly Stopwatch _stopWatch;
		private static int _periodRun;

		private static void ShareData(List<BaseMetric> perfData)
		{
			if (!_started) return;
			_accessibleData.Data = new ReadOnlyCollection<BaseMetric>(perfData);
			if (_onDataCollected != null)
			{
				_onDataCollected(null, new CollectedPerfDataEventArgs(_accessibleData.Data));
			}
		}

		public static ReadOnlyCollection<BaseMetric> Data
		{
			get
			{
				if (!_started) return null;
				return _accessibleData.Data;
			}
		}

		public static event CollectedPerfDataEventHandler OnDataCollected
		{
			add
			{
				_onDataCollected += value;
			}
			remove
			{
				if (_onDataCollected != null)
				{
					// ReSharper disable once DelegateSubtraction
					_onDataCollected -= value;
				}
			}
		}

		/*public static void AddRunCountMonitor(string id)
		{
			_perfData.TryAdd(id.ToLowerInvariant(), new BaseMetric(true, false, _periodRun));
		}

		public static void AddDurationMonitor(string id)
		{
			_perfData.TryAdd(id.ToLowerInvariant(), new BaseMetric(false, true, _periodRun));
		}

		public static void AddMonitor(string id)
		{
			_perfData.TryAdd(id.ToLowerInvariant(), new BaseMetric(true, true, _periodRun));
		}

		public static void AddStatusMonitor(string id, object baseStatus)
		{
			_perfData.TryAdd(id.ToLowerInvariant(), new BaseMetric(false, false, _periodRun, baseStatus));
		}*/

		static PerfMon()
		{
			_accessibleData = new LockFreeItem<ReadOnlyCollection<BaseMetric>>(new ReadOnlyCollection<BaseMetric>(new List<BaseMetric>()));
			_perfData = new AsyncLockFreeDictionary<string, BaseMetric>();
			_stopWatch = new Stopwatch();
		}

		static void OnElapsed(object sender, ElapsedTimerEventArgs e)
		{
			if (!_started) return;
			if (_overlapping) return;
			_overlapping = true;
			_stopWatch.Reset();
			_stopWatch.Start();
			WriteAllData();
			SetElapsedAndRun(PERFMON_RUNS, _stopWatch.ElapsedMilliseconds);
			_overlapping = false;
		}

		public static void Start(int periodRun = 1000)
		{
			_started = true;
			_overlapping = false;
			_periodRun = periodRun;
			_timer = new SystemTimer(periodRun, 0);
			_timer.Elapsed += OnElapsed;

			foreach (var item in _beforeStart)
			{
				AddMonitor(item.Key, item.Value);
			}
			AddMonitor(PERFMON_RUNS, new RunAndExcutionCounterMetric());
			_timer.Start();


		}

		public static void Reset()
		{

		}

		static void WriteAllData()
		{
			if (!_started) return;
			var listOfData = new List<BaseMetric>();
			var now = DateTime.UtcNow;
			foreach (var item in _perfData)
			{
				item.Value.DoCalculation(now);
				listOfData.Add(item.Value);
			}
			ShareData(listOfData);
		}

		public static void AddMonitor(string id, BaseMetric metric)
		{
			if (!_started)
			{
				if (!_beforeStart.ContainsKey(id))
				{
					_beforeStart.Add(id, metric);
				}
			}
			else
			{
				metric.Initialize(_periodRun, id.ToLowerInvariant());
				_perfData.TryAdd(id.ToLowerInvariant(), metric);
			}

		}

		public static bool SetElapsedAndRun(string id, long elapsedMs, int runs = 1)
		{
			if (!_started) return true;
			if(!_perfData.ContainsKey(id.ToLowerInvariant())) return false;
			var item = _perfData[id.ToLowerInvariant()] as RunAndExcutionCounterMetric;
			if (item == null) return false;
			item.InternalElapsedCount.SetValue(elapsedMs);
			item.InternalRunCount.SetValue(runs);
			return true;
		}

		public static bool SetMetric(string id, long value)
		{
			if (!_started) return true;
			if (!_perfData.ContainsKey(id.ToLowerInvariant())) return false;
			var item = _perfData[id.ToLowerInvariant()] as ValueCounterMetric;
			if (item == null) return false;
			item.InternalRunCount.SetValue(value);
			return true;
		}

		public static bool SetStatus(string id, object status)
		{
			if (!_started) return true;
			if (!_perfData.ContainsKey(id.ToLowerInvariant())) return false;
			var item = _perfData[id.ToLowerInvariant()] as StatusMetric;
			if (item == null) return false;
			item.InternalStatus.SetValue(status);
			return true;
		}
	}
}
