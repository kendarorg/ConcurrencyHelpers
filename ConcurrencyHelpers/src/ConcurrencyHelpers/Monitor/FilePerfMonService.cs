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
