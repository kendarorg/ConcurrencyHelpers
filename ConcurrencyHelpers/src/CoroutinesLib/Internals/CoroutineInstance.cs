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


using CoroutinesLib.Shared;
using CoroutinesLib.Shared.Enumerators;
using CoroutinesLib.Shared.Logging;

namespace CoroutinesLib.Internals
{
	internal class CoroutineInstance
	{
		public CoroutineInstance(ICoroutineThread coroutineThread)
		{
			Coroutine = coroutineThread;
		}

		internal bool IsInitialized { get; set; }

		internal void Initialize(ILogger log)
		{
			var en = new CoroutineResultEnumerator(

				string.Format("Enumerator for '{0}'.", Coroutine.InstanceName),

				Coroutine.Execute().GetEnumerator())
			{
				Log = log
			};
			Enumerators = en;
			IsInitialized = true;
		}

		public ICoroutineThread Coroutine { get; private set; }
		public CoroutineResultEnumerator Enumerators { get; private set; }

		public string BuildRunningStatus()
		{

			return Coroutine.GetType().Name + "(" + (Coroutine.InstanceName ?? "") + ")" + Enumerators.BuildRunningStatus();

		}
	}
}