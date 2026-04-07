using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using static Urbanflow.src.backend.models.util.Log;

namespace Urbanflow.src.backend.services
{
	public class OptimizationLoggerService
	{
		private static OptimizationLoggerService _instance;
		public static OptimizationLoggerService Instance => _instance ??= new OptimizationLoggerService();

		public ObservableCollection<LogEntry> Logs { get; } = new();

		private OptimizationLoggerService() { }

		public void Log(string message, LogLevel level = LogLevel.Info)
		{
			var entry = new LogEntry
			{
				Message = message,
				Level = level
			};

			// Ensure UI thread update
			Application.Current.Dispatcher.Invoke(() =>
			{
				Logs.Add(entry);
			});
		}

		public void Clear()
		{
			Logs.Clear();
		}
	}
}
