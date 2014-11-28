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


using System.Diagnostics;
using CoroutinesLib.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CoroutinesLib.Test
{
	[TestClass]
	public class FluentResultBuilderTest
	{
		[Flags]
		private enum ToVerify
		{
			None = 0x00,
			AndWait = 0x01,
			OnComplete = 0x02,
			OnCompleteWithResults = 0x04,
			OnError = 0x08,
			AsCoroutine = 0x10,
			Timeout = 0x20,
			OnEach = 0x40,
			Do= 0x80

		}

		[DebuggerHidden]
		private void Verify(object item,ToVerify toVerify= ToVerify.None)
		{
			if (toVerify.HasFlag(ToVerify.AsCoroutine))
			{
				Assert.IsInstanceOfType(item,typeof(ICoroutineThread));
				return;
			}
			var frb = item as FluentResultBuilder;
			if(frb==null) throw new AssertFailedException("Not a fluent result builder");
			if (toVerify.HasFlag(ToVerify.AndWait))
			{
				Assert.IsTrue(frb.Type.HasFlag(FluentResultType.Waiting));
			}
			if (toVerify.HasFlag(ToVerify.Do))
			{
				//Assert.IsTrue(frb.Type.HasFlag(FluentResultType.ForeachFunction));
				Assert.IsNotNull(frb.OnEachItem);;
			}
			if (toVerify.HasFlag(ToVerify.OnComplete))
			{
				//Assert.IsTrue(frb.Type.HasFlag(FluentResultType.FunctionWithoutResult));
				Assert.IsNotNull(frb.OnCompleteWithoutResultDo);;
			}
			if (toVerify.HasFlag(ToVerify.OnCompleteWithResults))
			{
				//Assert.IsTrue(frb.Type.HasFlag(FluentResultType.FunctionWithResult));
				Assert.IsNotNull(frb.OnCompleteWithResults); ;
			}
			if (toVerify.HasFlag(ToVerify.OnError))
			{
				Assert.IsTrue(frb.Type.HasFlag(FluentResultType.WithOnError));
				Assert.IsNotNull(frb.OnErrorDo); ;
			}
			if (toVerify.HasFlag(ToVerify.Timeout))
			{
				Assert.IsTrue(frb.Type.HasFlag(FluentResultType.WithTimeout));
				Assert.AreEqual(10, frb.Timeout.TotalMilliseconds);
			}
			if (toVerify.HasFlag(ToVerify.OnEach))
			{
				Assert.IsTrue(frb.Type.HasFlag(FluentResultType.ForeachFunction));
				Assert.IsNotNull(frb.OnEachItem); ;
			}
		}

		private T Create<T>() where T : class
		{
			var res = new FluentResultBuilder();
			return res as T;
		}

		[TestMethod]
		public void FluentResultBuilder_ShouldBeUsableWithIForEachItem()
		{
			Verify(Create<IForEachItem>().Do((a) => true).AndWait(), ToVerify.Do | ToVerify.AndWait);
			Verify(Create<IForEachItem>().Do((a) => true), ToVerify.Do);
			Verify(Create<IForEachItem>().Do((a) => true).OnComplete(() => { }), ToVerify.Do | ToVerify.OnComplete);
			Verify(Create<IForEachItem>().Do((a) => true).AsCoroutine(), ToVerify.AsCoroutine);
			Verify(Create<IForEachItem>().Do((a) => true).OnError((ex) => true), ToVerify.Do | ToVerify.OnError);
			Verify(Create<IForEachItem>().Do((a) => true).OnError((ex) => true).AndWait(), ToVerify.Do | ToVerify.OnError|ToVerify.AndWait);
			Verify(Create<IForEachItem>().Do((a) => true).OnError((ex) => true).AsCoroutine(), ToVerify.Do | ToVerify.OnError | ToVerify.AsCoroutine);
			Verify(Create<IForEachItem>().Do((a) => true).WithTimeout(TimeSpan.FromMilliseconds(10)), ToVerify.Do | ToVerify.Timeout);
			Verify(Create<IForEachItem>().Do((a) => true).WithTimeout(10), ToVerify.Do | ToVerify.Timeout);
			Verify(Create<IForEachItem>().Do((a) => true).WithTimeout(10).AndWait(), ToVerify.Do | ToVerify.Timeout|ToVerify.AndWait);
			Verify(Create<IForEachItem>().Do((a) => true).WithTimeout(10).AsCoroutine(), ToVerify.Do | ToVerify.Timeout | ToVerify.AsCoroutine);
			Verify(Create<IForEachItem>().Do((a) => true).WithTimeout(10).OnError((ex) => true), ToVerify.Do | ToVerify.Timeout | ToVerify.OnError);
		}

		[TestMethod]
		public void FluentResultBuilder_ShouldBeUsableWithIOnComplete()
		{
			Verify(Create<IOnComplete>().OnComplete(() => { }).AndWait(), ToVerify.OnComplete | ToVerify.AndWait);
			Verify(Create<IOnComplete>().OnComplete(() => { }), ToVerify.OnComplete);
			Verify(Create<IOnComplete>().OnComplete(() => { }), ToVerify.OnComplete | ToVerify.OnComplete);
			Verify(Create<IOnComplete>().OnComplete(() => { }).AsCoroutine(), ToVerify.AsCoroutine);
			Verify(Create<IOnComplete>().OnComplete(() => { }).OnError((ex) => true), ToVerify.OnComplete | ToVerify.OnError);
			Verify(Create<IOnComplete>().OnComplete(() => { }).OnError((ex) => true).AndWait(), ToVerify.OnComplete | ToVerify.OnError | ToVerify.AndWait);
			Verify(Create<IOnComplete>().OnComplete(() => { }).OnError((ex) => true).AsCoroutine(), ToVerify.OnComplete | ToVerify.OnError | ToVerify.AsCoroutine);
			Verify(Create<IOnComplete>().OnComplete(() => { }).WithTimeout(TimeSpan.FromMilliseconds(10)), ToVerify.OnComplete | ToVerify.Timeout);
			Verify(Create<IOnComplete>().OnComplete(() => { }).WithTimeout(10), ToVerify.OnComplete | ToVerify.Timeout);
			Verify(Create<IOnComplete>().OnComplete(() => { }).WithTimeout(10).AndWait(), ToVerify.OnComplete | ToVerify.Timeout | ToVerify.AndWait);
			Verify(Create<IOnComplete>().OnComplete(() => { }).WithTimeout(10).AsCoroutine(), ToVerify.OnComplete | ToVerify.Timeout | ToVerify.AsCoroutine);
			Verify(Create<IOnComplete>().OnComplete(() => { }).WithTimeout(10).OnError((ex) => true), ToVerify.OnComplete | ToVerify.Timeout | ToVerify.OnError);
		}

		[TestMethod]
		public void FluentResultBuilder_ShouldBeUsableWithIOnResponseMessage()
		{
			Verify(Create<IOnResponseMessage>().OnResponse((a) => { }).AndWait(), ToVerify.OnCompleteWithResults | ToVerify.AndWait);
			Verify(Create<IOnResponseMessage>().OnResponse((a) => { }), ToVerify.OnCompleteWithResults);
			Verify(Create<IOnResponseMessage>().OnResponse((a) => { }), ToVerify.OnCompleteWithResults | ToVerify.OnComplete);
			Verify(Create<IOnResponseMessage>().OnResponse((a) => { }).AsCoroutine(), ToVerify.AsCoroutine);
			Verify(Create<IOnResponseMessage>().OnResponse((a) => { }).OnError((ex) => true), ToVerify.OnCompleteWithResults | ToVerify.OnError);
			Verify(Create<IOnResponseMessage>().OnResponse((a) => { }).OnError((ex) => true).AndWait(), ToVerify.OnCompleteWithResults | ToVerify.OnError | ToVerify.AndWait);
			Verify(Create<IOnResponseMessage>().OnResponse((a) => { }).OnError((ex) => true).AsCoroutine(), ToVerify.OnCompleteWithResults | ToVerify.OnError | ToVerify.AsCoroutine);
			Verify(Create<IOnResponseMessage>().OnResponse((a) => { }).WithTimeout(TimeSpan.FromMilliseconds(10)), ToVerify.OnCompleteWithResults | ToVerify.Timeout);
			Verify(Create<IOnResponseMessage>().OnResponse((a) => { }).WithTimeout(10), ToVerify.OnCompleteWithResults | ToVerify.Timeout);
			Verify(Create<IOnResponseMessage>().OnResponse((a) => { }).WithTimeout(10).AndWait(), ToVerify.OnCompleteWithResults | ToVerify.Timeout | ToVerify.AndWait);
			Verify(Create<IOnResponseMessage>().OnResponse((a) => { }).WithTimeout(10).AsCoroutine(), ToVerify.OnCompleteWithResults | ToVerify.Timeout | ToVerify.AsCoroutine);
			Verify(Create<IOnResponseMessage>().OnResponse((a) => { }).WithTimeout(10).OnError((ex) => true), ToVerify.OnCompleteWithResults | ToVerify.Timeout | ToVerify.OnError);
		}

		[TestMethod]
		public void FluentResultBuilder_ShouldBeUsableWithIOnFunctionResult()
		{

			Verify(Create<IOnFunctionResult>().OnComplete((a) => { }).AndWait(), ToVerify.OnCompleteWithResults | ToVerify.AndWait);
			Verify(Create<IOnFunctionResult>().OnComplete((a) => { }), ToVerify.OnCompleteWithResults);
			Verify(Create<IOnFunctionResult>().OnComplete((a) => { }), ToVerify.OnCompleteWithResults | ToVerify.OnComplete);
			Verify(Create<IOnFunctionResult>().OnComplete((a) => { }).AsCoroutine(), ToVerify.AsCoroutine);
			Verify(Create<IOnFunctionResult>().OnComplete((a) => { }).OnError((ex) => true), ToVerify.OnCompleteWithResults | ToVerify.OnError);
			Verify(Create<IOnFunctionResult>().OnComplete((a) => { }).OnError((ex) => true).AndWait(), ToVerify.OnCompleteWithResults | ToVerify.OnError | ToVerify.AndWait);
			Verify(Create<IOnFunctionResult>().OnComplete((a) => { }).OnError((ex) => true).AsCoroutine(), ToVerify.OnCompleteWithResults | ToVerify.OnError | ToVerify.AsCoroutine);
			Verify(Create<IOnFunctionResult>().OnComplete((a) => { }).WithTimeout(TimeSpan.FromMilliseconds(10)), ToVerify.OnCompleteWithResults | ToVerify.Timeout);
			Verify(Create<IOnFunctionResult>().OnComplete((a) => { }).WithTimeout(10), ToVerify.OnCompleteWithResults | ToVerify.Timeout);
			Verify(Create<IOnFunctionResult>().OnComplete((a) => { }).WithTimeout(10).AndWait(), ToVerify.OnCompleteWithResults | ToVerify.Timeout | ToVerify.AndWait);
			Verify(Create<IOnFunctionResult>().OnComplete((a) => { }).WithTimeout(10).AsCoroutine(), ToVerify.OnCompleteWithResults | ToVerify.Timeout | ToVerify.AsCoroutine);
			Verify(Create<IOnFunctionResult>().OnComplete((a) => { }).WithTimeout(10).OnError((ex) => true), ToVerify.OnCompleteWithResults | ToVerify.Timeout | ToVerify.OnError);
		}
	}
}