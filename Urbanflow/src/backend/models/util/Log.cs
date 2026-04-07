using System;
using System.Collections.Generic;
using System.Text;

namespace Urbanflow.src.backend.models.util
{
	public class Log
	{
		public enum LogLevel
		{
			Info,
			Success,
			Warning,
			Error
		}

		public class LogEntry
		{
			public DateTime Timestamp { get; set; } = DateTime.Now;
			public LogLevel Level { get; set; }
			public string Message { get; set; }

			public string FormattedMessage =>
				$"[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level.ToString().ToUpper()}] {Message}";
		}
	}
}
