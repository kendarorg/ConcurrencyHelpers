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
using CoroutinesLib.Shared.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CoroutinesLib.Internals.MessageBus
{
	internal class MessageBus : CoroutineBase, IMessageBus, ILoggable
	{

#if nope
		private readonly ConcurrentQueue<ICoroutineMessageBusCommands> _commands = new ConcurrentQueue<ICoroutineMessageBusCommands>();

		private readonly Dictionary<Type, List<IMessageHandler>> _messageHandlers =
			 new Dictionary<Type, List<IMessageHandler>>();
		private readonly ConcurrentQueue<IMessage> _messages = new ConcurrentQueue<IMessage>();
		//private readonly Dictionary<Guid, MessageCoroutineResult> _responses = new Dictionary<Guid, MessageCoroutineResult>();


		public void Register<T>(ICoroutineThread coroutineThreadBase) where T : IMessage
		{
			_commands.Enqueue(new CoroutineMessageBusRegister(coroutineThreadBase, typeof(T)));
		}

		public void Unregister<T>(ICoroutineThread coroutineThreadBase) where T : IMessage
		{
			_commands.Enqueue(new CoroutineMessageBusUnregister(coroutineThreadBase, typeof(T)));
		}

		public IMessageResult GetResponse(Guid guid)
		{
			/*if (!_responses.ContainsKey(guid))
			{
				return null;
			}
			var response = _responses[guid];
			if (response.Timeout)
			{
				throw new MessageTimeoutException(response.TimeoutLength);
			}
			var result = (IMessageResult)response.Result;
			_responses.Remove(guid);
			return result;*/
			throw new NotImplementedException();
		}

		public void Post<T>(T message) where T : IMessage
		{
			_messages.Enqueue(message);
		}

		public ICoroutineResult Send<T>(ICoroutineThread caller, T message, Action<IMessageResult> action = null, TimeSpan? timeout = null) where T : IMessage
		{
			throw new NotImplementedException();
			/*var msTimeoutLength = timeout == null
				? -1
				: timeout.Value.Ticks/TimeSpan.TicksPerMillisecond;
			var msTimeout = timeout == null
				? -1
				: DateTime.UtcNow.Ticks/TimeSpan.TicksPerMillisecond + msTimeoutLength;
			
			var msRemove = timeout == null ? -1 : msTimeout*2;
			_messages.Enqueue(message);
			var result = new MessageCoroutineResult
			{
				ResultCallback = action,
				OriginalMesssage = message,
				Caller = caller,
				ExpireOn =msTimeout,
				RemoveOn = msRemove,
				TimeoutLength = msTimeoutLength
			};
			_responses.Add(message.Id, result);
			return result;*/
		}

		public override void Initialize()
		{

		}

		public override IEnumerable<ICoroutineResult> OnCycle()
		{
			HandleRegistraterUnregister();
			HandleMessagesRouting();
			HandleTimeouts();
			yield break;
		}

		private void HandleTimeouts()
		{
			throw new NotImplementedException();/*
			var currentMs = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
			var expired = new List<Guid>();
			foreach (var item in _responses)
			{
				var current = item.Value;
				if (current.ExpireOn != -1 && current.ExpireOn < currentMs)
				{
					current.Timeout = true;
				}
				if (current.RemoveOn != -1 && current.RemoveOn < currentMs)
				{
					expired.Add(item.Key);
				}
			}*/
		}

		private void HandleMessagesRouting()
		{
			IMessage message;
			while (_messages.TryDequeue(out message))
			{
				var t = message.GetType();
				var response = message as IMessageResult;

				if (response == null)
				{
					if (!_messageHandlers.ContainsKey(t))
					{
						continue;
					}
					var handlers = _messageHandlers[t];
					foreach (var handler in handlers)
					{
						if (HandleMessage(message, handler) && message is IOneTimeMessage)
						{
							break;
						}
					}
				}
				else
				{
					throw new NotImplementedException();
					/*if (!_responses.ContainsKey(response.InResponseOf))
					{
						continue;
					}
					_responses[response.InResponseOf].SetResult(response);*/
				}
			}
		}

		private bool HandleMessage(IMessage message, IMessageHandler handler)
		{
			try
			{
				handler.AsDynamic().Handle(message);
				return true;
			}
			catch (Exception ex)
			{
				handler.AsDynamic().Handle(new ExceptionMessage(ex, message));
				return false;
			}
		}

		private void HandleRegistraterUnregister()
		{
			ICoroutineMessageBusCommands command;
			while (_commands.TryDequeue(out command))
			{
				var register = command as CoroutineMessageBusRegister;
				if (register != null)
				{
					RegisterMessageType(register);
				}
				else
				{
					var unregister = command as CoroutineMessageBusUnregister;
					if (unregister != null)
					{
						UnregisterMessageType(unregister);
					}
				}
			}
		}

		private void UnregisterMessageType(CoroutineMessageBusUnregister register)
		{
			if (!_messageHandlers.ContainsKey(register.MessageType))
			{
				return;
			}
			var tHandlers = _messageHandlers[register.MessageType];
			var messageCoroutine = register.Coroutine as IMessageHandler;
			var toRemove = tHandlers.IndexOf(messageCoroutine);
			tHandlers.RemoveAt(toRemove);
		}

		private void RegisterMessageType(CoroutineMessageBusRegister register)
		{
			if (!_messageHandlers.ContainsKey(register.MessageType))
			{
				_messageHandlers[register.MessageType] = new List<IMessageHandler>();
			}
			var messageCoroutine = register.Coroutine as IMessageHandler;
			_messageHandlers[register.MessageType].Add(messageCoroutine);
		}

		public override void OnEndOfCycle()
		{

		}

		public override bool OnError(Exception exception)
		{
			return false;
		}
#endif
		private Dictionary<Type, List<IMessageHandler>> _handlers = new Dictionary<Type, List<IMessageHandler>>();
		private readonly ConcurrentQueue<IMessage> _messages = new ConcurrentQueue<IMessage>();
		private readonly List<IMessage> _messagesToElaborate = new List<IMessage>();

		public override void Initialize()
		{

		}

		public override IEnumerable<ICoroutineResult> OnCycle()
		{
			IMessage messageToElaborate;
			while (_messages.TryDequeue(out messageToElaborate))
			{
				_messagesToElaborate.Add(messageToElaborate);
			}
			foreach (var message in _messagesToElaborate)
			{
				if (_handlers.ContainsKey(message.GetType()))
				{
					var generic = typeof (IMessageHandler<IMessage>).MakeGenericType(message.GetType());
					var method = generic.GetMethod("Handle");
					if (message is IOneTimeMessage)
					{
						var handler = _handlers[message.GetType()].FirstOrDefault();
						if (handler != null)
						{
							HandleMessage(message, handler, method);
						}
					}
					else
					{
						foreach (var handler in _handlers[message.GetType()])
						{
							HandleMessage(message, handler, method);
						}
					}
				}
			}
			yield return CoroutineResult.Wait;
		}

		private void HandleMessage(IMessage message, IMessageHandler handler, MethodInfo method)
		{
			try
			{
				method.Invoke(handler, new object[] { message });
			}
			catch (Exception)
			{

			}
		}

		public override void OnEndOfCycle()
		{
			var now = DateTime.UtcNow;
			for (int index = _messagesToElaborate.Count-1; index >=0 ; index--)
			{
				var message = _messagesToElaborate[index];
				if (message.Timeout < now)
				{
					_messagesToElaborate.RemoveAt(index);
				}
			}
		}

		public void Post<T>(T message) where T : IMessage
		{
			_messages.Enqueue(message);
		}

		public void Send<T>(T message) where T : IMessage
		{
			_messages.Enqueue(message);
		}

		public void Register(ICoroutineThread coroutineThreadBase)
		{
			var typeName = typeof(IMessageHandler).Name;
			var type = coroutineThreadBase.GetType();
			foreach (var receiver in GetAllHandledMessageTypes(type, typeName))
			{
				if (!_handlers.ContainsKey(receiver))
				{
					_handlers.Add(receiver, new List<IMessageHandler>());
				}
				// ReSharper disable once SuspiciousTypeConversion.Global
				_handlers[receiver].Add((IMessageHandler)coroutineThreadBase);
			}
		}

		private static IEnumerable<Type> GetAllHandledMessageTypes(Type type, string typeName)
		{
			return type.GetInterfaces()
				.Where(t =>
				{
					if (!t.IsGenericType) return false;
					if (t.Name != typeName) return false;
					return true;
				})
				.Select(t => t.GetGenericArguments()[0]);
		}

		public void Unregister(ICoroutineThread coroutineThreadBase)
		{
			var typeName = typeof(IMessageHandler).Name;
			var type = coroutineThreadBase.GetType();
			foreach (var receiver in GetAllHandledMessageTypes(type, typeName))
			{
				if (!_handlers.ContainsKey(receiver))
				{
					continue;
				}
				// ReSharper disable once SuspiciousTypeConversion.Global
				_handlers[receiver].Remove((IMessageHandler)coroutineThreadBase);
				if (_handlers[receiver].Count == 0)
				{
					_handlers.Remove(receiver);
				}
			}
		}
	}
}
