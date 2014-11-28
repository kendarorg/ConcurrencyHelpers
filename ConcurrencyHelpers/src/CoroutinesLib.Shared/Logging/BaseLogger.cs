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
