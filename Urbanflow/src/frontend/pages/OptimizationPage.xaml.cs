using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Urbanflow.src.backend.models;
using Urbanflow.src.backend.models.ga;
using Urbanflow.src.backend.services;
using static Urbanflow.src.backend.models.util.Log;

namespace Urbanflow.src.frontend.pages
{
	/// <summary>
	/// Interaction logic for OptimizationPage.xaml
	/// </summary>
	public partial class OptimizationPage : Page
	{
		private readonly Workflow workflow;

		private OptimizationSettings settings = new();
		private string descriptor = "";
		private CancellationTokenSource _gaCts;
		public ObservableCollection<LogEntry> Logs => OptimizationLoggerService.Instance.Logs;

		public OptimizationPage(Workflow workflow)
		{
			InitializeComponent();
			this.workflow = workflow;

			DataContext = this;
			SetGAStatusLabel("Standby", "default");
		}

		private void btn_SaveConfig_Click(object sender, RoutedEventArgs e)
		{
			settings = new()
			{
				PopulationSize = (int)num_PopulationSize.Value,
				IterationNumber = (int)num_IterationNumber.Value,
				UserOptimizationParameters = new OptimizationParameters
				{
					Genome_RouteCount = (int)num_Genome_RouteCount.Value,
					Genome_HubNumberInRoute = (int)num_Genome_HubCount.Value,
					Genome_AllowOneWayRoutes = (bool)num_Genome_AllowOneWay.IsChecked,

					Fitness_RedundancyPercentParameter = (int)num_Fitness_Redundancy.Value,
					Fitness_RouteLengthParameter = (int)num_Fitness_Length.Value,
					Fitness_MaximalAllowedChangeParameter = (int)num_Fitness_Changes.Value,
					Fitness_FleetCapacityParameter = (int)num_Fitness_Fleet.Value,
					Fitness_MaximumWaitingMinutesParameter = (int)num_Fitness_MinWait.Value,
					Fitness_MinimalWaitingMinutesParameter = (int)num_Fitness_MaxWait.Value,
					Fitness_MaximumTravelTimeParameter = (int)num_Fitness_MaxTravelTime.Value
				}
			};

			OptimizationLoggerService.Instance.Log("Settings saved...", LogLevel.Success);
		}

		private async void btn_RunGA_Click(object sender, RoutedEventArgs e)
		{
			OptimizationLoggerService.Instance.Clear();
			_gaCts?.Cancel();
			_gaCts = new CancellationTokenSource();
			var token = _gaCts.Token;

			SetGAStatusLabel("Running...", "orange");
			OptimizationLoggerService.Instance.Log("Collecting data for Genetic Algorithm...");

			try
			{
				var result = await Task.Run(() =>
				{
					token.ThrowIfCancellationRequested();

					workflow.SetNetworkinformationFromInnerGtfsFeed();
					token.ThrowIfCancellationRequested();

					workflow.CreateGAOptimizationService(settings);
					token.ThrowIfCancellationRequested();

					return workflow.UserRunGAWithLogging(descriptor, token);
				}, token);

				if (result.IsFailure)
				{
					SetGAStatusLabel("Error", "red");
					OptimizationLoggerService.Instance.Log($"GA failed: {result.Error}", LogLevel.Error);
				}
				else
				{
					SetGAStatusLabel("Finished", "green");
					OptimizationLoggerService.Instance.Log("GA finished successfully", LogLevel.Success);
					OptimizationLoggerService.Instance.Log($"Best fitness: {result.Value.FitnessValuesPerGenerations.Last()}", LogLevel.Success);
					workflow.SetBestGenome(result.Value.BestGeneratedGenomes);
				}
			}
			catch (OperationCanceledException)
			{
				SetGAStatusLabel("Cancelled", "red");
				OptimizationLoggerService.Instance.Log("GA cancelled", LogLevel.Warning);
			}
		}

		//Green, grey, red, yellow
		private void SetGAStatusLabel(string text, string color)
		{
			lbl_GAstatus.Text = text;
			switch (color) {
				case "red":
					lbl_GAstatus.Foreground = Brushes.Red;
					circle_GAstatus.Fill = Brushes.Red;
					break;
				case "orange":
					lbl_GAstatus.Foreground = Brushes.Orange;
					circle_GAstatus.Fill = Brushes.Orange;
					break;
				case "yellow":
					lbl_GAstatus.Foreground = Brushes.Yellow;
					circle_GAstatus.Fill = Brushes.Yellow;
					break;
				case "green":
					lbl_GAstatus.Foreground = Brushes.Green;
					circle_GAstatus.Fill = Brushes.Green;
					break;
				default:
					lbl_GAstatus.Foreground = Brushes.Black;
					circle_GAstatus.Fill = Brushes.Black;
					break;			
			}
		}

		private void btn_Cancel_Click(object sender, RoutedEventArgs e)
		{
			_gaCts?.Cancel();
		}
	}
}
