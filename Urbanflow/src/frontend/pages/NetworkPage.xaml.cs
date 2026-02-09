using System.Windows.Controls;
using Urbanflow.src.backend.models;
using Urbanflow.src.backend.models.gtfs;
using Urbanflow.src.backend.services;

namespace Urbanflow.src.frontend.pages
{
	/// <summary>
	/// Interaction logic for NetworkPage.xaml
	/// </summary>
	public partial class NetworkPage : Page
	{
		private readonly Workflow workflow;

		public NetworkPage(Workflow workflow)
		{
			InitializeComponent();
			this.workflow = workflow;
		}

		private void LoadRouteNames()
		{

			List<Route> routes = GtfsManagerService.GetRoutesForWorkflow(workflow.Id);

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
