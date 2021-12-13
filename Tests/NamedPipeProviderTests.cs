using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using NUnit.Framework;

namespace Tempest.Tests
{
	[TestFixture]
    [Ignore("Not Implemented")]
	public class NamedPipeProviderTests
		: ConnectionProviderTests
	{

		protected override MessageTypes MessageTypes
		{
			get { throw new NotImplementedException(); }
		}

        protected override Target Target => throw new NotImplementedException();

        protected override IConnectionProvider SetUp()
		{
			throw new NotImplementedException();
		}

        protected override IConnectionProvider SetUp(IEnumerable<Protocol> protocols)
        {
            throw new NotImplementedException();
        }

        protected override IClientConnection SetupClientConnection()
        {
            throw new NotImplementedException();
        }

        protected override IClientConnection SetupClientConnection(IEnumerable<Protocol> protocols)
        {
            throw new NotImplementedException();
        }
    }
}
