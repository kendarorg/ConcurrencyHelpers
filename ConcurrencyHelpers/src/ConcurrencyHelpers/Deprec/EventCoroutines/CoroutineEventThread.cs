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


using System.Diagnostics.CodeAnalysis;
using ConcurrencyHelpers.Containers;
using ConcurrencyHelpers.Utils;
using System;
using System.Collections.Generic;
namespace ConcurrencyHelpers.Coroutines
{

	[Obsolete]
	[ExcludeFromCodeCoverage]
	internal class EventMessage
	{
		public object Data;
		public string EventName;
	}


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

	[Obsolete]
	[ExcludeFromCodeCoverage]
	internal class EventDispatcher : Coroutine
	{
		public EventDispatcher(LockFreeQueue<EventMessage> events, CoroutineThread[] threads, CounterInt64 counter, Dictionary<string, Dictionary<Type, CoroutineEventDescriptor>> eventsList)
		{
			_events = events;
			_threads = threads;
			_counter = counter;
			_eventsList = eventsList;
		}

		private CounterInt64 _counter;
		private CoroutineThread[] _threads;
		private LockFreeQueue<EventMessage> _events;
		private Dictionary<string, Dictionary<Type, CoroutineEventDescriptor>> _eventsList;

		public override IEnumerable<Step> Run()
		{
			while (Thread.Status != CoroutineThreadStatus.Running)
			{
				foreach (var newEvt in _events.Dequeue(100))
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


	[Obsolete]
	[ExcludeFromCodeCoverage]
	public class CoroutineEventThread
	{
		private LockFreeQueue<EventMessage> _events;
		internal static CoroutineEventThread _eventThread;

		public static void Initialize(int threadsCount = 0, int cycleMaxMs = 10)
		{
			_eventThread = new CoroutineEventThread(threadsCount, cycleMaxMs);
		}

		private readonly int _threadsCount;
		private CoroutineThread[] _threads;
		private CoroutineThread _manager;
		private LockFreeQueue<CoroutineEventDescriptor> _eventQueue;
		private CounterInt64 _counter;
		private Dictionary<string, Dictionary<Type, CoroutineEventDescriptor>> _eventsList;

		protected CoroutineEventThread(int threadsCount, int cycleMaxMs)
		{
			_eventQueue = new LockFreeQueue<CoroutineEventDescriptor>();
			_events = new LockFreeQueue<EventMessage>();
			_eventsList = new Dictionary<string, Dictionary<Type, CoroutineEventDescriptor>>(StringComparer.OrdinalIgnoreCase);
			_counter = new CounterInt64(0);
			if (threadsCount < 1) threadsCount = 1;
			_threadsCount = threadsCount;
			_threads = new CoroutineThread[_threadsCount];
			_manager = new CoroutineThread(1);
			_manager.AddCoroutine(new EventManagerCoroutine(_eventQueue, _counter, _eventsList));

			for (int i = 0; i < _threadsCount; i++)
			{
				_threads[i] = new CoroutineThread(cycleMaxMs);
			}
		}

		public static void AddEntryCoroutines(params ICoroutine[] coroutines)
		{
			_eventThread._manager.AddCoroutine(coroutines);
		}

		/* Action(EventThread,string callerEvent,T eventParameter */
		public static void RegisterEventHandler<T>(string eventName, Action<CoroutineEventThread, string, T> action)
		{
			var eventDescriptor = new CoroutineEventDescriptor(eventName,
				(thread, caller, par) =>
				{
					action(
						_eventThread, caller, (T)par);
				}
				, typeof(T));
			_eventThread._eventQueue.Enqueue(eventDescriptor);
		}

		public static void RegisterEventHandler<T>(string eventName, IEventHandler<T> eventhandler)
		{
			var eventDescriptor = new CoroutineEventDescriptor(eventName,
				(thread, caller, par) =>
				{
					try
					{
						eventhandler.Run(_eventThread, caller, (T)par);
						eventhandler.OnSuccess();
					}
					catch (Exception ex)
					{
						eventhandler.OnError(ex);
					}
				}
				, typeof(T));
			_eventThread._eventQueue.Enqueue(eventDescriptor);
		}

		public static void Publish<T>(string eventName, T eventArgs)
		{
			_eventThread._counter++;
			_eventThread._events.Enqueue(new EventMessage { Data = eventArgs, EventName = eventName });
		}

		public static void Start()
		{
			foreach (var th in _eventThread._threads)
			{
				th.Start();
			}
			_eventThread._manager.Start();
		}

		public static void Stop()
		{
			_eventThread._manager.Stop();
			foreach (var th in _eventThread._threads)
			{
				th.Stop();
			}
		}
	}


	[Obsolete]
	public interface IEventCoroutine
	{
		IEnumerable<CoroutineEventDescriptor> Initialize();
	}
}