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
using System.Globalization;
using System.Threading;

namespace ConcurrencyHelpers.Utils
{
	/// <summary>
	/// Class to wrap the interlocking logic
	/// </summary>
	public class CounterInt64
	{
		private Int64 _value;

		public CounterInt64(long value = 0)
		{
			Value = value;
		}

		public Int64 Value
		{
			get { return Interlocked.Read(ref _value); }
			set { Interlocked.Exchange(ref _value, value); }
		}

		public Int64 GetAndReset()
		{
			return Interlocked.Exchange(ref _value, 0);
		}

		public Int64 GetAndSet(Int64 value)
		{
			return Interlocked.Exchange(ref _value, value);
		}

		public Int64 CompareExchange(Int64 compareWith, Int64 exchangeWith)
		{
			return Interlocked.CompareExchange(ref _value, exchangeWith, compareWith);
		}

		public Int64 Increment()
		{
			return Interlocked.Increment(ref _value);
		}

		public Int64 Decrement()
		{
			return Interlocked.Decrement(ref _value);
		}

		public static explicit operator Int64(CounterInt64 value)
		{
			return value.Value;
		}

		public static implicit operator CounterInt64(Int64 value)
		{
			return new CounterInt64(value);
		}

		public static implicit operator CounterInt64(Int32 value)
		{
			return new CounterInt64(value);
		}

		public static CounterInt64 operator +(CounterInt64 left, CounterInt64 right)
		{
			CounterInt64 add = left;
			add.Value = left.Value + right.Value;
			return add;
		}

		public static CounterInt64 operator +(CounterInt64 value)
		{
			return value;
		}

		public static CounterInt64 operator ++(CounterInt64 value)
		{
			Interlocked.Increment(ref value._value);
			return value;
		}

		public static CounterInt64 operator --(CounterInt64 value)
		{
			Interlocked.Decrement(ref value._value);
			return value;
		}

		public override string ToString()
		{
			return Value.ToString(CultureInfo.InvariantCulture);
		}
	}
}