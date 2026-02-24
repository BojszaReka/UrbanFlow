using System.Windows;
using Urbanflow.src.backend.models;
using Urbanflow.src.frontend.pages;

namespace Urbanflow.src.frontend.windows
{
	/// <summary>
	/// Interaction logic for SidebarWindow.xaml
	/// </summary>
	public partial class SidebarWindow : Window
	{
		private Workflow workflow;

		private const string TabSelectedButtonResourceKey = "TabSelectedButton";
		private const string TabUnselectedButtonResourceKey = "TabUnselectedButton";

		public SidebarWindow(in Workflow workflow)
		{
			InitializeComponent();
			this.workflow = workflow;
			MainFrame.Content = new GraphPage(workflow);
		}

		private void UnselectTabButtons()
		{
			btn_tabgraph.Style = Application.Current.Resources[TabUnselectedButtonResourceKey] as Style;
			btn_tabai.Style = Application.Current.Resources[TabUnselectedButtonResourceKey] as Style;
			btn_tabmap.Style = Application.Current.Resources[TabUnselectedButtonResourceKey] as Style;
			btn_tabnetwork.Style = Application.Current.Resources[TabUnselectedButtonResourceKey] as Style;
		}

		private void OpenGraphView(object sender, RoutedEventArgs e)
		{
			MainFrame.Content = new GraphPage(workflow);
			UnselectTabButtons();
			btn_tabgraph.Style = Application.Current.Resources[TabSelectedButtonResourceKey] as Style;
		}

		private void OpenAiView(object sender, RoutedEventArgs e)
		{
			MainFrame.Content = new OptimizationPage(workflow);
			UnselectTabButtons();
			btn_tabai.Style = Application.Current.Resources[TabSelectedButtonResourceKey] as Style;
		}

		private void OpenMapView(object sender, RoutedEventArgs e)
		{
			MainFrame.Content = new MapPage(workflow);
			UnselectTabButtons();
			btn_tabmap.Style = Application.Current.Resources[TabSelectedButtonResourceKey] as Style;
		}

		private void OpenNetworkView(object sender, RoutedEventArgs e)
		{
			MainFrame.Content = new NetworkPage(workflow);
			UnselectTabButtons();
			btn_tabnetwork.Style = Application.Current.Resources[TabSelectedButtonResourceKey] as Style;
		}

		private void BackToWelcome_Click(object sender, RoutedEventArgs e)
		{
			MainWindow mainWindow = new();
			mainWindow.Show();
			this.Close();
		}
	}
}
