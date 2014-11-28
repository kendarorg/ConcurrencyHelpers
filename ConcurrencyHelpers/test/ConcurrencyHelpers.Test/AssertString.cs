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
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConcurrencyHelpers.Test
{
	public static class AssertString
	{
		public static void AreEqual(string expected, string actual)
		{
			expected = expected.Replace("\r\n", "\n");
			actual = actual.Replace("\r\n", "\n");
			if (expected.Length == actual.Length)
			{
				for (var i = 0; i < expected.Length; i++)
				{
					var expectedChar = (int)expected[i];
					var actualChar = (int)actual[i];
					if (actualChar != expectedChar)
					{
						break;
					}
				}
				return;
			}
			throw new AssertFailedException(string.Format("AssertString.AreEqual Failed:\nExpected <{0}>\nActual  <{1}>", expected,
				actual));
		}

		public static string UTF8ToAscii(string text)
		{
			var utf8 = Encoding.UTF8;
			Byte[] encodedBytes = utf8.GetBytes(text);
			Byte[] convertedBytes = Encoding.Convert(Encoding.UTF8, Encoding.ASCII, encodedBytes);

			return Encoding.ASCII.GetString(convertedBytes);
		}
	}
}
