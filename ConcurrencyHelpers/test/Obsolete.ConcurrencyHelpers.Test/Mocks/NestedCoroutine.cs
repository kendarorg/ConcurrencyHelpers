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
using ConcurrencyHelpers.Coroutines;

namespace ConcurrencyHelpers.Test.Mocks
{
	public class NestedCoroutine : Coroutine
	{
		public bool FirstCallFinished { get; set; }
		public bool SecondCallFinished { get; set; }

		public override IEnumerable<Step> Run()
		{
			//Called as local, on complete will set the is ready flag to true
			yield return InvokeLocalAndWait(() => FirstCall());
			yield return Step.Current;
			if (!SecondCallFinished)
			{
				throw new Exception();
			}
			if (!FirstCallFinished)
			{
				throw new Exception();
			}
			yield return Step.Current;
			ShouldTerminate = true;
			RunFinished = true;
		}

		public bool RunFinished { get; set; }

		private IEnumerable<Step> FirstCall()
		{
			//Re-set the is read
			yield return InvokeLocalAndWait(() => SecondCall());
			if (!SecondCallFinished)
			{
				throw new Exception();
			}
			yield return Step.Current;
			FirstCallFinished = true;
			yield return Step.Current;
		}


		private IEnumerable<Step> SecondCall()
		{
			yield return Step.Current;
			yield return Step.Current;
			SecondCallFinished = true;
			yield return Step.Current;
		}

		public override void OnError(Exception ex)
		{
			throw ex;
		}
	}
}