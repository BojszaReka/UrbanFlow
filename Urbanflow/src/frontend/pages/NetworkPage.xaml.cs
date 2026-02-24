using System.Windows;
using System.Windows.Controls;
using Urbanflow.src.backend.models;
using Urbanflow.src.backend.models.gtfs;
using Urbanflow.src.backend.models.util;
using Urbanflow.src.backend.services;

namespace Urbanflow.src.frontend.pages
{
	/// <summary>
	/// Interaction logic for NetworkPage.xaml
	/// </summary>
	public partial class NetworkPage : Page
	{
		private readonly Workflow workflow;

		public NetworkPage(in Workflow workflow)
		{
			InitializeComponent();
			this.workflow = workflow;
		}

		private void LoadRouteNames()
		{
			Result<HashSet<Route>> result = workflow.GetAllRoutes();
			if (result.IsFailure)
			{
				MessageBox.Show(result.Error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			HashSet<Route> routes = result.Value;

			RouteNamesComboBox.Items.Clear();

			foreach (var route in routes)
			{
				string displayName;

				// Build a nice display name
				if (!string.IsNullOrWhiteSpace(route.ShortName) && !string.IsNullOrWhiteSpace(route.LongName))
					displayName = $"Járat {route.ShortName}: {route.LongName}";
				else if (!string.IsNullOrWhiteSpace(route.ShortName))
					displayName = route.ShortName;
				else
					displayName = route.LongName ?? "Unknown Route";

				RouteNamesComboBox.Items.Add(new ComboBoxItem { Content = displayName });
			}

			// Select first item if exists
			if (RouteNamesComboBox.Items.Count > 0)
				RouteNamesComboBox.SelectedIndex = 0;
		}
	}
}
