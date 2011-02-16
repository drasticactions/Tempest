﻿//
// NetworkClientConnection.cs
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
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Threading;
using Tempest.InternalProtocol;

namespace Tempest.Providers.Network
{
	public sealed class NetworkClientConnection
		: NetworkConnection, IClientConnection
	{
		public NetworkClientConnection (Protocol protocol)
			: base (new [] { protocol })
		{
		}

		public NetworkClientConnection (IEnumerable<Protocol> protocols)
			: base (protocols)
		{
		}

		public event EventHandler<ClientConnectionEventArgs> Connected;
		public event EventHandler<ClientConnectionEventArgs> ConnectionFailed;

		public void Connect (EndPoint endpoint, MessageTypes messageTypes)
		{
			if (endpoint == null)
				throw new ArgumentNullException ("endpoint");
			if ((messageTypes & MessageTypes.Unreliable) == MessageTypes.Unreliable)
				throw new NotSupportedException();

			SocketAsyncEventArgs args;
			bool connected;

			while (this.pendingAsync > 0)
				Thread.Sleep (0);

			lock (this.stateSync)
			{
				if (IsConnected)
					throw new InvalidOperationException ("Already connected");

				this.connecting = true;
				RemoteEndPoint = endpoint;

				args = new SocketAsyncEventArgs();
				args.RemoteEndPoint = endpoint;
				args.Completed += ConnectCompleted;

				this.reliableSocket = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

				Interlocked.Increment (ref this.pendingAsync);
				connected = !this.reliableSocket.ConnectAsync (args);
			}

			if (connected)
				ConnectCompleted (this.reliableSocket, args);
		}

		private void ConnectCompleted (object sender, SocketAsyncEventArgs e)
		{
			if (e.SocketError != SocketError.Success)
			{
				this.connecting = false;
				Disconnect (true, DisconnectedReason.ConnectionFailed);
				OnConnectionFailed (new ClientConnectionEventArgs (this));
				Interlocked.Decrement (ref this.pendingAsync);
				return;
			}

			e.Completed -= ConnectCompleted;
			e.Completed += ReliableReceiveCompleted;
			e.SetBuffer (this.rmessageBuffer, 0, this.rmessageBuffer.Length);
			this.rreader = new BufferValueReader (this.rmessageBuffer);

			bool recevied;
			lock (this.stateSync)
			{
				if (!IsConnected)
				{
					Interlocked.Decrement (ref this.pendingAsync);
					return;
				}

				Interlocked.Increment (ref this.pendingAsync);
				recevied = !this.reliableSocket.ReceiveAsync (e);
			}

			if (recevied)
				ReliableReceiveCompleted (this.reliableSocket, e);

			OnConnected (new ClientConnectionEventArgs (this));
			this.connecting = false;
			Interlocked.Decrement (ref this.pendingAsync);
			//Send (new ConnectMessage { Protocols = this.protocols.Values });
		}

		private int pingFrequency;
		private Timer activityTimer;

		protected override void OnTempestMessageReceived (MessageEventArgs e)
		{
			switch (e.Message.MessageType)
			{
				case (ushort)TempestMessageType.Ping:
					var ping = (PingMessage)e.Message;
					if (this.pingFrequency == 0)
					{
						if (this.activityTimer != null)
							this.activityTimer.Dispose();

						if (ping.Interval != 0)
							this.activityTimer = new Timer (ActivityCallback, null, ping.Interval, ping.Interval);
					}
					else if (ping.Interval != this.pingFrequency)
						this.activityTimer.Change (ping.Interval, ping.Interval);
					
					this.pingFrequency = ((PingMessage)e.Message).Interval;
					break;

				case (ushort)TempestMessageType.Connected:
					var msg = (ConnectedMessage)e.Message;
					this.protocols = this.protocols.Values.Intersect (msg.EnabledProtocols).ToDictionary (p => p.id);

					OnConnected (new ClientConnectionEventArgs (this));
					Interlocked.Decrement (ref this.pendingAsync);
					break;
			}

			base.OnTempestMessageReceived(e);
		}

		private void ActivityCallback (object state)
		{
			if (DateTime.Now.Subtract (this.lastReceived).TotalMilliseconds > this.pingFrequency * 2)
				Disconnect (true);
		}

		private void OnConnected (ClientConnectionEventArgs e)
		{
			var connected = Connected;
			if (connected != null)
				connected (this, e);
		}

		private void OnConnectionFailed (ClientConnectionEventArgs e)
		{
			var handler = this.ConnectionFailed;
			if (handler != null)
				handler (this, e);
		}
	}
}