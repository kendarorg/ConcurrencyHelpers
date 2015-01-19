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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ConcurrencyHelpers.Containers;
using ConcurrencyHelpers.Coroutines;
using ConcurrencyHelpers.Utils;

namespace ConcurrencyHelpers.EventCoroutines
{

	[Obsolete]
	[ExcludeFromCodeCoverage]
	internal class EventManagerCoroutine : Coroutine
	{
		private LockFreeQueue<CoroutineEventDescriptor> _eventDescriptorsQueue;
		private CounterInt64 _counter;
		private Dictionary<string, Dictionary<Type, CoroutineEventDescriptor>> _eventsDescriptors;

		public EventManagerCoroutine(LockFreeQueue<CoroutineEventDescriptor> eventQueue, CounterInt64 counter,
			Dictionary<string, Dictionary<Type, CoroutineEventDescriptor>> eventsDescriptors)
		{
			_eventsDescriptors = eventsDescriptors;
			_eventDescriptorsQueue = eventQueue;
			_counter = counter;
		}

		public override IEnumerable<Step> Run()
		{
			while (Thread.Status != CoroutineThreadStatus.Running)
			{
				foreach (var newEvt in _eventDescriptorsQueue.Dequeue(100))
				{
					if (!_eventsDescriptors.ContainsKey(newEvt.EventName))
					{
						_eventsDescriptors.Add(newEvt.EventName, new Dictionary<Type, CoroutineEventDescriptor>());
					}
					if (!_eventsDescriptors[newEvt.EventName].ContainsKey(newEvt.EventArgType))
					{
						_eventsDescriptors[newEvt.EventName].Add(newEvt.EventArgType, newEvt);
					}
				}
				yield return Step.Current;
			}
		}

		public override void OnError(Exception ex)
		{

		}
	}
}