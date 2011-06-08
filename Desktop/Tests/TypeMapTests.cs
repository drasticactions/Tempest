﻿//
// TypeMapTests.cs
//
// Author:
//   Eric Maupin <me@ermau.com>
//
// Copyright (c) 2011 Eric Maupin
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Tempest.Tests
{
	[TestFixture]
	public class TypeMapTests
	{
		[Test]
		public void TryGetTypeIdNull()
		{
			var map = new TypeMap();
			int id;
			Assert.Throws<ArgumentNullException> (() => map.TryGetTypeId (null, out id));
		}

		[Test]
		public void First()
		{
			var map = new TypeMap();

			int id;
			Assert.IsTrue (map.TryGetTypeId (typeof (string), out id));
			Assert.AreEqual (0, id);
		}

		[Test]
		public void Repeated()
		{
			var map = new TypeMap();

			int id, id2;
			Assert.IsTrue (map.TryGetTypeId (typeof (string), out id));
			Assert.AreEqual (0, id);
			
			Assert.IsFalse (map.TryGetTypeId (typeof (string), out id2));
			Assert.AreEqual (id, id2);
		}

		[Test]
		public void Multiple()
		{
			var map = new TypeMap();

			int id, id2;
			Assert.IsTrue (map.TryGetTypeId (typeof (string), out id));
			Assert.AreEqual (0, id);

			Assert.IsFalse (map.TryGetTypeId (typeof (string), out id2));
			Assert.AreEqual (id, id2);

			Assert.IsTrue (map.TryGetTypeId (typeof (int), out id));
			Assert.AreNotEqual (id2, id);

			Assert.IsFalse (map.TryGetTypeId (typeof (int), out id2));
			Assert.AreEqual (id, id2);
		}

		[Test]
		public void GetNew()
		{
			var map = new TypeMap();

			int id;
			map.TryGetTypeId (typeof (string), out id);

			Assert.Contains (new KeyValuePair<Type, int> (typeof(string), 0), map.GetNewTypes().ToList());
		}

		[Test]
		public void GetNewMultiple()
		{
			var map = new TypeMap();

			int id;
			map.TryGetTypeId (typeof (string), out id);
			map.TryGetTypeId (typeof (int), out id);

			var newItems = map.GetNewTypes().ToList();
			Assert.Contains (new KeyValuePair<Type, int> (typeof(string), 0), newItems);
			Assert.Contains (new KeyValuePair<Type, int> (typeof(int), 1), newItems);
		}

		[Test]
		public void GetNewRepeated()
		{
			var map = new TypeMap();

			int id;
			map.TryGetTypeId (typeof (string), out id);

			var newItems = map.GetNewTypes().ToList();
			Assert.Contains (new KeyValuePair<Type, int> (typeof(string), 0), newItems);
			Assert.That (newItems, Is.Not.Contains (new KeyValuePair<Type, int> (typeof(string), 0)));
		}
	}
}