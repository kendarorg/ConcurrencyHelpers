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
using System.IO;

namespace ConcurrencyHelpers.Monitor
{
	public class FilePerfMonService : IDisposable
	{
		private readonly string _rootDir;
		private bool _overlapping;

		public FilePerfMonService(string rootDir)
		{
			_overlapping = false;
			_rootDir = rootDir;
			PerfMon.OnDataCollected += OnDataCollected;
		}

		void OnDataCollected(object sender, CollectedPerfDataEventArgs e)
		{
			if (_overlapping) return;
			_overlapping = true;
			foreach (var metric in e.Data)
			{
				var path = Path.Combine(_rootDir, metric.Id + ".csv");
				//RunAndExcutionCounterMetric
				//StatusMetric
				//ValueCounterMetric
				var sc = metric as StatusMetric;
				if (sc != null)
				{
					WriteMetric(path, sc);
				}
				var rc = metric as RunAndExcutionCounterMetric;
				if (rc != null)
				{
					WriteMetric(path, rc);
				}
				var vc = metric as ValueCounterMetric;
				if (vc != null)
				{
					WriteMetric(path, vc);
				}
			}
			_overlapping = false;
		}

		private void WriteMetric(string path, StatusMetric metric)
		{
			if (!File.Exists(path))
			{
				const string header =
					"Timestamp\tStatus\tStartTimestamp\r\n";
				File.WriteAllText(path, header);
			}

			var result = string.Format("{0}\t{1}\t{2}\r\n",
				metric.LastTimestamp.ToString("yyyy/MM/dd-HH:mm:ss"),
				metric.Status.ToString(),
				metric.StartTimestamp.ToString("yyyy/MM/dd-HH:mm:ss"));
			File.AppendAllText(path, result);
		}

		private void WriteMetric(string path, ValueCounterMetric metric)
		{
			if (!File.Exists(path))
			{
				const string header =
					"Timestamp\tValue\tMin\tMax\tAvg\tStartTimestamp\r\n";
				File.WriteAllText(path, header);
			}

			var result = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\r\n",
				metric.LastTimestamp.ToString("yyyy/MM/dd-HH:mm:ss"),
				metric.Value,
				metric.Max < 0 ? -1 : metric.Min,
				metric.Max < 0 ? -1 : metric.Max,
				metric.Max < 0 ? -1 : metric.Avg,
				metric.StartTimestamp.ToString("yyyy/MM/dd-HH:mm:ss"));
			File.AppendAllText(path, result);
		}

		private void WriteMetric(string path, RunAndExcutionCounterMetric metric)
		{
			if (!File.Exists(path))
			{
				const string header =
					"Timestamp\tRunCount\tRunAvg\tValue\tMin\tMax\tAvg\tStartTimestamp\r\n";
				File.WriteAllText(path, header);
			}

			var result = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\r\n",
				metric.LastTimestamp.ToString("yyyy/MM/dd-HH:mm:ss"),
				metric.RunValue,
				metric.RunAvg,
				metric.ElapsedValue,
				metric.ElapsedMax < 0 ? -1 : metric.ElapsedMin,
				metric.ElapsedMax < 0 ? -1 : metric.ElapsedMax,
				metric.ElapsedMax < 0 ? -1 : metric.ElapsedAvg,
				metric.StartTimestamp.ToString("yyyy/MM/dd-HH:mm:ss"));
			File.AppendAllText(path, result);
		}

		public void Dispose()
		{
			PerfMon.OnDataCollected -= OnDataCollected;
		}
	}
}
