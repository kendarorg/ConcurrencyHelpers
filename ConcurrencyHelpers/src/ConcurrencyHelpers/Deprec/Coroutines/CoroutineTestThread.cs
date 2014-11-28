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
using System.Diagnostics.CodeAnalysis;

namespace ConcurrencyHelpers.Coroutines
{

	[Obsolete]
	[ExcludeFromCodeCoverage]
	public class CoroutineTestThread : CoroutineThread
	{
		private Stopwatch _stopWatch;

		public void Initialize()
		{
			Status = CoroutineThreadStatus.Running;
			_stopWatch = new Stopwatch();
		}

		public override void AddCoroutine(params ICoroutine[] coroutines)
		{
			if (Coroutine.InterceptExternalCalls)
			{
				var stepResult = Coroutine.Interceptor(coroutines, null);
				if (stepResult.TerminateHere) return;
			}
			base.AddCoroutine(coroutines);
		}

		public void StepForward()
		{
			RunStep(_stopWatch);
		}
	}
}