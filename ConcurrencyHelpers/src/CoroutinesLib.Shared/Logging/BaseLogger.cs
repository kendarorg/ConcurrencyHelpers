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
using System.Diagnostics;

namespace CoroutinesLib.Shared.Logging
{
	public abstract class BaseLogger : ILogger
	{
		protected BaseLogger(LoggerLevel level = LoggerLevel.Error)
		{
			Level = level;
		}

		public LoggerLevel Level { get; private set; }

		public void Fatal(Exception ex, string format = null, params object[] pars)
		{
			LogExceptionInternal(ex, format, pars, LoggerLevel.Fatal);
		}

		public void Debug(Exception ex, string format = null, params object[] pars)
		{
			LogExceptionInternal(ex, format, pars, LoggerLevel.Debug);
		}

		public void Error(Exception ex, string format = null, params object[] pars)
		{
			LogExceptionInternal(ex, format, pars, LoggerLevel.Error);
		}

		public void Fatal(string format, params object[] pars)
		{
			LogFormatInternal(format, pars, LoggerLevel.Fatal);
		}

		public void Debug(string format, params object[] pars)
		{
			LogFormatInternal(format, pars, LoggerLevel.Debug);
		}

		public void Error(string format, params object[] pars)
		{
			LogFormatInternal(format, pars, LoggerLevel.Error);
		}

		public void Warning(string format, params object[] pars)
		{
			LogFormatInternal(format, pars, LoggerLevel.Warning);
		}

		public void Info(string format, params object[] pars)
		{
			LogFormatInternal(format, pars, LoggerLevel.Info);
		}

		private void LogFormatInternal(string format, object[] pars, LoggerLevel level)
		{
			try
			{
				if (level <= Level)
				{
					LogFormat(format, pars, level);
				}
			}
			catch (Exception ex)
			{
				Trace.WriteLine("Logger Exception");
				Trace.WriteLine(ex);
			}
		}

		private void LogExceptionInternal(Exception exception, string format, object[] pars, LoggerLevel level)
		{
			try
			{
				if (level <= Level)
				{
					LogException(exception, format, pars, level);
				}
			}
			catch (Exception ex)
			{
				Trace.WriteLine("Logger Exception:");
				Trace.WriteLine(ex);
				Trace.WriteLine("Caused by Exception:");
				Trace.WriteLine(exception);
			}
		}

		protected virtual string CreateLogLine(string format, object[] pars, LoggerLevel level)
		{
			return string.Format("{0}  {1,-10} {2}",
													 DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss.ff"),
													 "(" + level.ToString() + ")",
													 pars != null ? string.Format(format, pars) : (format != null ? format : ""));
		}

		protected virtual void LogFormat(string format, object[] pars, LoggerLevel level)
		{
			WriteLine(CreateLogLine(format, pars, level), level);
		}

		protected virtual void LogException(Exception exception, string format, object[] pars, LoggerLevel level)
		{
			WriteLine(CreateLogLine(format, pars, level), level);
			WriteLine(string.Format(" {0}", exception.Message), level);
			WriteLine(string.Format(" {0}", exception.Source), level);
			if (!string.IsNullOrEmpty(exception.StackTrace))
			{
				WriteLine(string.Format("  {0}", exception.StackTrace), level);
			}
		}

		protected abstract void WriteLine(string toWrite, LoggerLevel level);

		public void SetLoggingLevel(LoggerLevel level)
		{
			Level = level;
		}
	}
}
