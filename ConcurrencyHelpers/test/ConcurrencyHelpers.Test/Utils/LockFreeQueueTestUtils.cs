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
