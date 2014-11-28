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
using System.Linq;
using CoroutinesLib.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoroutinesLib.Test
{

	public class CoroutineBaseFake : CoroutineBase
	{
		public List<String> Called = new List<string>();
		public Exception Throws = null;
		public bool NoRun = false;
		public override void Initialize()
		{
			Called.Add("Initialize");
		}

		public override IEnumerable<ICoroutineResult> OnCycle()
		{
			if (NoRun)yield break;
			Called.Add("OnCycle");
			if (Throws != null)
			{
				throw Throws;
			}
			yield return CoroutineResult.YieldReturn(DateTime.UtcNow);
		}

		public override bool OnError(Exception exception)
		{
			Called.Add("OnError");
			return base.OnError(exception);
		}

		public override void OnEndOfCycle()
		{
			Called.Add("OnEndOfCycle");
		}

		public override void OnDestroy()
		{
			Called.Add("OnDestroy");
		}
	}
	[TestClass]
	public class CoroutineBaseTest
	{
		[TestMethod]
		public void CoroutineBase_ShouldBeRunnable()
		{
			var cr = new CoroutineBaseFake();
			var en = cr.Execute().GetEnumerator();
			en.MoveNext();
			Assert.AreEqual(2, cr.Called.Count);
			Assert.AreEqual("Initialize", cr.Called[0]);
			Assert.AreEqual("OnCycle", cr.Called[1]);

			en.MoveNext();
			Assert.AreEqual(4, cr.Called.Count);
			Assert.AreEqual("OnEndOfCycle", cr.Called[2]);
			Assert.AreEqual("OnCycle", cr.Called[3]);
		}


		[TestMethod]
		public void CoroutineBase_ShouldStopOnException()
		{
			Exception throws = null;
			var cr = new CoroutineBaseFake();
			var en = cr.Execute().GetEnumerator();
			en.MoveNext();
			en.MoveNext();

			Assert.AreEqual(4, cr.Called.Count);
			cr.Throws = new Exception();
			try
			{
				en.MoveNext();
			}
			catch (Exception ex)
			{
				throws = ex;
			}

			Assert.AreEqual(throws,cr.Throws);
			Assert.AreEqual(6, cr.Called.Count);
			Assert.AreEqual("OnEndOfCycle", cr.Called[4]);
			Assert.AreEqual("OnCycle", cr.Called[5]);
		}

		[TestMethod]
		public void CoroutineBase_ShouldWaitWhenNothingToDo()
		{
			var cr = new CoroutineBaseFake();
			var en = cr.Execute().GetEnumerator();
			en.MoveNext();
			en.MoveNext();

			Assert.AreEqual(4, cr.Called.Count);
			cr.NoRun = true;
			en.MoveNext();
			Assert.AreEqual(CoroutineResult.Wait,en.Current);

			Assert.AreEqual(6, cr.Called.Count);
			Assert.AreEqual("OnEndOfCycle", cr.Called[4]);
			Assert.AreEqual("OnEndOfCycle", cr.Called[5]);
		}
	}
}