using System.Windows;
using System.Windows.Controls;
using Urbanflow.src.backend.models;
using Urbanflow.src.backend.services;
using Urbanflow.src.backend.test_automater;
using Urbanflow.src.frontend.dialogs;

namespace Urbanflow.src.frontend.pages
{
	/// <summary>
	/// Interaction logic for WelcomePage.xaml
	/// </summary>
	public partial class WelcomePage : Page
	{
		public WelcomePage()
		{
			InitializeComponent();
			LoadCitiesIntoComboBox();

			runtest();
		}

		private async void AddCity_Click(object sender, RoutedEventArgs e)
		{
			AddCityDialog dialog = new();

			if (dialog.ShowDialog() == true)
			{
				string city = dialog.CityName;
				string folder = dialog.SelectedFolder;
				string description = dialog.Description;

				var result = await MenuManagerService.AddCity(city, description, folder);
				if (result.IsFailure)
				{
					MessageBox.Show($"A(z) '{city}' város feldolgozása sikertelen, hiba: {result.Error}");
				}
				else
				{
					LoadCitiesIntoComboBox();

					MessageBox.Show(
					$"A(z) '{city}' város sikeresen feldolgozásra került");
				}
			}
		}
		private void LoadCitiesIntoComboBox()
		{
			// Remove all items except the hint
			for (int i = CityComboBox.Items.Count - 1; i >= 0; i--)
			{
				if (CityComboBox.Items[i] != HintItem)
					CityComboBox.Items.RemoveAt(i);
			}

			var result = MenuManagerService.GetCityNames();
			if (result.IsFailure)
			{
				MessageBox.Show($"A városok betöltése sikertelen, hiba: {result.Error}");
			}
			else
			{
				List<string> cities = result.Value;

				foreach (string city in cities)
				{
					CityComboBox.Items.Add(new ComboBoxItem
					{
						Content = city
					});
				}

				// Keep the hint selected
				HintItem.IsSelected = true;
			}
		}


		private void CityComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// If nothing selected → disable button
			if (CityComboBox.SelectedItem == null)
			{
				ContinueButton.IsEnabled = false;
				return;
			}


			if (CityComboBox.SelectedItem is ComboBoxItem selected && !selected.IsEnabled)
			{
				return;
			}

			// Otherwise, a real item is selected → enable button
			ContinueButton.IsEnabled = true;
		}

		private void ContinueButton_Click(object sender, RoutedEventArgs e)
		{
			if (CityComboBox.SelectedItem is ComboBoxItem item)
			{
				string? selectedCity = item.Content?.ToString();
				if (selectedCity is not null)
				{
					// Navigate to next page and pass the city
					WorkflowPage workflowPage = new(selectedCity);

					// Get MainWindow and navigate
					if (Application.Current.MainWindow is MainWindow mainWindow && mainWindow.MainFrame != null)
					{
						mainWindow.MainFrame.Navigate(workflowPage);
					}
				}
				else
				{
					throw new InvalidOperationException("Selected city is null.");
				}
			}
		}

		private void runtest()
		{
			Main m = new Main();
			m.RunGeneticAlgorithm();
		}
	}
}
