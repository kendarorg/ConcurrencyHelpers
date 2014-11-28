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
using System.Threading;
using ConcurrencyHelpers.Containers;

namespace ConcurrencyHelpers.Test.Utils
{
	public class LockFreeQueueTestUtils
	{
		private static LockFreeQueue<CollectionElement> _internalQueue;
		private static Int64 _collectedElements;
		private static Int64 _sentElements;
		private static Int64 _toSendElements;

		public static bool IsSendCompleted
		{
			get { return _toSendElements == Interlocked.Read(ref _collectedElements); }
		}

		public static void Initialize(int toSendElements)
		{
			_internalQueue = new LockFreeQueue<CollectionElement>();
			_collectedElements = 0;
			_sentElements = 0;
			_toSendElements = toSendElements;
		}

		public static void ProducerThread()
		{
			while (_toSendElements > Interlocked.Read(ref _sentElements))
			{
				if (Interlocked.Increment(ref _sentElements) <= _toSendElements)
				{
					_internalQueue.Enqueue(new CollectionElement(0));
				}
			}
		}

		public static void ConsumerThread()
		{
			while (_toSendElements != Interlocked.Read(ref _collectedElements))
			{
#pragma warning disable 168
				// ReSharper disable once UnusedVariable
				foreach (CollectionElement ce in _internalQueue.Dequeue())
#pragma warning restore 168
				{
					Interlocked.Increment(ref _collectedElements);
				}
			}
		}
	}
}
