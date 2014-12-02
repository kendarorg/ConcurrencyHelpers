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
using System.Threading.Tasks;

namespace CoroutinesLib.Shared
{
	public interface IOnResponseMessage : IAndWait
	{
		IWithTimeout OnResponse(Action<IMessage> onResponse);
	}

	public interface IWithTimeout : IOnError, IAsCoroutine
	{
		IOnError WithTimeout(TimeSpan timeout);
		IOnError WithTimeout(int milliseconds);
	}

	public interface IOnError : IAndWait, IAsCoroutine
	{
		IWaitOrCoroutine OnError(Func<Exception, bool> onError);
	}

	public interface IWaitOrCoroutine : IAndWait, IAsCoroutine
	{

	}

	public interface IOnFunctionResult : IWaitOrCoroutine
	{
		IWithTimeout OnComplete<T>(Action<T> onComplete);
		IWithTimeout OnComplete(Action<ICoroutineResult> onComplete);
	}

	public interface IForEachItem
	{
		IOnComplete Do<T>(Func<T, bool> onEach);
		IOnComplete Do(Func<ICoroutineResult, bool> onEach);
	}

	public interface IOnComplete : IWithTimeout
	{
		IWithTimeout OnComplete(Action onComplete);
	}

	public interface IAndWait : ICoroutineResult
	{
		ICoroutineResult AndWait();
	}

	public interface IAsCoroutine 
	{
		ICoroutineThread AsCoroutine();
	}
}
