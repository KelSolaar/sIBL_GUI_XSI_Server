using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Collections;

namespace XSI.TCP
{
	// Class to keep track of each client connected to the server, and to
	// provide the implementation for sending/receiving data to the remote
	// sIBL_GUI_XSI_Server.
	public class ConnectionState
	{
		internal Socket m_cnx;
		internal Server m_server;
		internal ServiceProvider m_provider;
		internal byte[] m_buffer;

		// The IP Address of the remote host.
		public EndPoint RemoteEndPoint
		{
			get{ return m_cnx.RemoteEndPoint; }
		}

		// Returns the number of bytes waiting to be read.
		public int AvailableData
		{
			get{ return m_cnx.Available; }
		}

		// Returns true if the socket is connected.
		public bool Connected
		{
			get{ return m_cnx.Connected; }
		}

		// Reads data from the socket and returns the number of bytes read.
		public int ReadFrom(byte[] in_buffer, int in_offset, int in_count)
		{
			try
			{
				if (m_cnx.Available > 0)
				{
					return m_cnx.Receive(	in_buffer,
											in_offset,
											in_count,
											SocketFlags.None);
				}
				else
				{
					return 0;
				}
			}
			catch
			{
				return 0;
			}
		}

		// Sends Data to the remote host.
		public bool WriteTo(byte[] in_buffer, int in_offset, int in_count)
		{
			try
			{
				m_cnx.Send(in_buffer, in_offset, in_count, SocketFlags.None);
				return true;
			}
			catch
			{
				return false;
			}
		}

		// Terminates the connection with the remote host.
		public void EndConnection()
		{
			if(m_cnx != null && m_cnx.Connected)
			{
				m_cnx.Shutdown(SocketShutdown.Both);
				m_cnx.Close();
			}
			m_server.DropConnection(this);
		}
}

	// Incoming connections service provider
	public abstract class ServiceProvider: ICloneable
	{
		// Provides a new instance of the object.
		public virtual object Clone()
		{
			throw new Exception("Must be implemented by derived classes.");
		}

		// Gets executed when the server accepts a new connection.
		public abstract void OnAcceptConnection(ConnectionState state);

		// Gets executed when the server detects incoming data.
		// This method is called only if OnAcceptConnection has already finished.
		public abstract void OnReceiveData(ConnectionState state);

		// Gets executed when the server needs to shutdown the connection.
		public abstract void OnDropConnection(ConnectionState state);
	}

	// TCP server implementation
	public class Server
	{
		private int m_port;
		private Socket m_listener;
		private ServiceProvider m_provider;
		private ArrayList m_connections;
		private int m_maxConnections;

		private AsyncCallback OnConnectionReady;
		private WaitCallback OnAcceptConnection;
		private AsyncCallback OnReceivedDataReady;

		// Initializes server. To start accepting connections call Start method.
		public Server(ServiceProvider in_provider, int in_port, int in_maxcnx)
		{
			m_provider = in_provider;
			m_port = in_port;
			m_maxConnections = in_maxcnx;
			m_listener = new Socket(	AddressFamily.InterNetwork,
										SocketType.Stream,
										ProtocolType.Tcp);
			m_connections = new ArrayList();
			OnConnectionReady = new AsyncCallback(OnConnectionReadyCallback);
			OnAcceptConnection = new WaitCallback(OnAcceptConnectionCallback);
			OnReceivedDataReady = new AsyncCallback(OnReceivedDataReadyCallback);
		}

		// Start accepting connections.
		// A false return value tell you that the port is not available.
		public bool Start(String in_host)
		{
			try
			{
				m_listener.Bind(new IPEndPoint(IPAddress.Parse(in_host), m_port));
				m_listener.Listen(100); // max number of connections allowed
				m_listener.BeginAccept(OnConnectionReady, null);
				return true;
			}
			catch
			{
				return false;
			}
		}

		// Callback function: A new connection is waiting.
		private void OnConnectionReadyCallback(IAsyncResult in_ar)
		{
			lock (this)
			{
				if (m_listener == null) return;

				Socket conn = m_listener.EndAccept(in_ar);
				if (m_connections.Count >= m_maxConnections)
				{
					string msg = "sIBL_GUI_XSI_Server: Max number of connections reached.";
					conn.Send(Encoding.UTF8.GetBytes(msg), 0, msg.Length, SocketFlags.None);
					conn.Shutdown(SocketShutdown.Both);
					conn.Close();
				}
				else
				{
					//Start a new connection
					ConnectionState st = new ConnectionState();
					st.m_cnx = conn;
					st.m_server = this;
					st.m_provider = (ServiceProvider)m_provider.Clone();
					st.m_buffer = new byte[4];
					m_connections.Add(st);

					//Queue the rest of the job to be executed later
					ThreadPool.QueueUserWorkItem(OnAcceptConnection, st);
				}
				//Resume the listening callback loop
				m_listener.BeginAccept(OnConnectionReady, null);
			}
		}

		// Executes OnAcceptConnection method from the service provider.
		private void OnAcceptConnectionCallback(object in_state)
		{
			ConnectionState st = in_state as ConnectionState;
			try
			{
				st.m_provider.OnAcceptConnection(st);
			}
			catch
			{
				// report error to provider
			}

			//Starts the ReceiveData callback loop
			if (st.m_cnx.Connected)
			{
				st.m_cnx.BeginReceive(	st.m_buffer, 0, 0,
										SocketFlags.None,
										OnReceivedDataReady, st);
			}
		}

		// Executes OnReceiveData method from the service provider.
		private void OnReceivedDataReadyCallback(IAsyncResult in_ar)
		{
			ConnectionState st = in_ar.AsyncState as ConnectionState;
			st.m_cnx.EndReceive(in_ar);

			// Consider the following condition as a signal that the
			// remote host dropped the connection.
			if (st.m_cnx.Available == 0)
			{
				DropConnection(st);
			}
			else
			{
				try
				{
					st.m_provider.OnReceiveData(st);
				}
				catch
				{
					// report error to the provider
				}

				// Resume ReceivedData callback loop
				if (st.m_cnx.Connected)
				{
					st.m_cnx.BeginReceive(	st.m_buffer, 0, 0,
											SocketFlags.None,
											OnReceivedDataReady, st);
				}
			}
		}

		// Shutdown the server
		public void Stop()
		{
			lock (this)
			{
				m_listener.Close();
				m_listener = null;

				//Close all active connections
				foreach (object obj in m_connections)
				{
					ConnectionState st = obj as ConnectionState;
					try
					{
						st.m_provider.OnDropConnection(st);
					}
					catch
					{
						//some error in the provider
					}
					st.m_cnx.Shutdown(SocketShutdown.Both);
					st.m_cnx.Close();
				}
				m_connections.Clear();
			}
		}

		// Removes a connection from the list
		internal void DropConnection(ConnectionState in_st)
		{
			lock (this)
			{
				in_st.m_cnx.Shutdown(SocketShutdown.Both);
				in_st.m_cnx.Close();
				if (m_connections.Contains(in_st))
				{
					m_connections.Remove(in_st);
				}
			}
		}

		public int MaxConnections
		{
			get
			{
				return m_maxConnections;
			}
			set
			{
				m_maxConnections = value;
			}
		}

		public int CurrentConnections
		{
			get
			{
				lock (this) { return m_connections.Count; }
			}
		}
	}

} // XSI.TCP namespace