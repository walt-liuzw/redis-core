﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace CSRedis
{
    /// <summary>
    /// Connection链接池
    /// </summary>
    public partial class ConnectionPool {
        private List<RedisConnection> AllConnections = new List<RedisConnection>();
		public Queue<RedisConnection> FreeConnections = new Queue<RedisConnection>();
		public Queue<ManualResetEvent> GetConnectionQueue = new Queue<ManualResetEvent>();
		private static object _lock = new object();
		private static object _lock_GetConnectionQueue = new object();
		private string _ip;
		private int _port, _poolsize;
		public event EventHandler Connected;

		public ConnectionPool(string ip, int port, int poolsize = 50) {
			_ip = ip;
			_port = port;
			_poolsize = poolsize;
		}

		public RedisConnection GetConnection() {
			RedisConnection conn = null;
			if (FreeConnections.Count > 0)
				lock (_lock)
					if (FreeConnections.Count > 0)
						conn = FreeConnections.Dequeue();
			if (conn == null && AllConnections.Count < _poolsize) {
				lock (_lock)
					if (AllConnections.Count < _poolsize) {
						conn = new RedisConnection();
						AllConnections.Add(conn);
					}
				if (conn != null) {
					conn.Pool = this;
					conn.Client = new RedisClient(new IPEndPoint(IPAddress.Parse(_ip), _port));
					conn.Client.Connected += Connected;
				}
			}
			if (conn == null) {
				ManualResetEvent wait = new ManualResetEvent(false);
				lock (_lock_GetConnectionQueue)
					GetConnectionQueue.Enqueue(wait);
				if (wait.WaitOne(TimeSpan.FromSeconds(10)))
					return GetConnection();
				throw new Exception("CSRedis.ConnectionPool.GetConnection 连接池获取超时（10秒）");
			}
			conn.ThreadId = Thread.CurrentThread.ManagedThreadId;
			conn.LastActive = DateTime.Now;
			Interlocked.Increment(ref conn.UseSum);
			return conn;
		}

		public void ReleaseConnection(RedisConnection conn) {
			lock (_lock)
				FreeConnections.Enqueue(conn);

			if (GetConnectionQueue.Count > 0) {
				ManualResetEvent wait = null;
				lock (_lock_GetConnectionQueue)
					if (GetConnectionQueue.Count > 0)
						wait = GetConnectionQueue.Dequeue();
				if (wait != null) wait.Set();
			}
		}
	}

	public class RedisConnection : IDisposable {
		public RedisClient Client;
		public DateTime LastActive;
		public long UseSum;
		internal int ThreadId;
		internal ConnectionPool Pool;

		public void Dispose() {
			if (Pool != null) Pool.ReleaseConnection(this);
		}
	}
}