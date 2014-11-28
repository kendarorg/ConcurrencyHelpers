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


namespace CoroutinesLib.Shared.Enums
{
	public enum RunningStatus
	{
		None = 0,
		Initialized = 1,
		Running = 2 | Initialized,
		NotRunning = 4,
		Paused = Running | 8,	//Paused is not stopped!!
		Aborted = NotRunning | 16,
		Stopping = NotRunning | 32,
		Stopped = NotRunning | 64,
		Exception = NotRunning | 128
	}

	public static class RunningStatusExtension
	{

		public static bool Is(this RunningStatus status,RunningStatus other)
		{
			return ((int) status & (int) other) == (int) other;
		}
	}
}
