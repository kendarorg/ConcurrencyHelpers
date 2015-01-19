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
