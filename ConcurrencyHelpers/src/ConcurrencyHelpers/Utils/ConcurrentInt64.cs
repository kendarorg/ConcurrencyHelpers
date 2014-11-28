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
using System.Globalization;
using System.Threading;

namespace ConcurrencyHelpers.Utils
{
	/// <summary>
	/// Class to wrap the interlocking logic
	/// </summary>
	public class ConcurrentInt64
	{
		private Int64 _value;

		public ConcurrentInt64(long value = 0)
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

		public static explicit operator Int64(ConcurrentInt64 value)
		{
			return value.Value;
		}

		public static implicit operator ConcurrentInt64(Int64 value)
		{
			return new ConcurrentInt64(value);
		}

		public static implicit operator ConcurrentInt64(Int32 value)
		{
			return new ConcurrentInt64(value);
		}

		public static ConcurrentInt64 operator +(ConcurrentInt64 left, ConcurrentInt64 right)
		{
			ConcurrentInt64 add = left;
			add.Value = left.Value + right.Value;
			return add;
		}

		public static ConcurrentInt64 operator +(ConcurrentInt64 value)
		{
			return value;
		}

		public static ConcurrentInt64 operator ++(ConcurrentInt64 value)
		{
			Interlocked.Increment(ref value._value);
			return value;
		}

		public static ConcurrentInt64 operator --(ConcurrentInt64 value)
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