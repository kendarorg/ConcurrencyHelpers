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
using System.Diagnostics.CodeAnalysis;
using ConcurrencyHelpers.Containers;
using ConcurrencyHelpers.Coroutines;
using ConcurrencyHelpers.Utils;

namespace ConcurrencyHelpers.EventCoroutines
{

	[Obsolete]
	[ExcludeFromCodeCoverage]
	internal class EventDispatcher : Coroutine
	{
		public EventDispatcher(LockFreeQueue<Coroutines.EventMessage> events, CoroutineThread[] threads, CounterInt64 counter, Dictionary<string, Dictionary<Type, CoroutineEventDescriptor>> eventsList)
		{
			_events = events;
			_threads = threads;
			_counter = counter;
			_eventsList = eventsList;
		}

		private readonly CounterInt64 _counter;
		private readonly CoroutineThread[] _threads;
		private readonly LockFreeQueue<Coroutines.EventMessage> _events;
		private readonly Dictionary<string, Dictionary<Type, CoroutineEventDescriptor>> _eventsList;

		public override IEnumerable<Step> Run()
		{
			while (Thread.Status != CoroutineThreadStatus.Running)
			{
				foreach (Coroutines.EventMessage newEvt in _events.Dequeue(100))
				{
					if (_eventsList.ContainsKey(newEvt.EventName) && newEvt.Data!=null)
					{
						var type =newEvt.Data.GetType();
						if (_eventsList[newEvt.EventName].ContainsKey(type))
						{
							var handler = _eventsList[newEvt.EventName][type];
							var where = _counter.Value % _threads.Length;
							_threads[where].AddCoroutine(new ExecEventCoroutine(handler, newEvt));
						}
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