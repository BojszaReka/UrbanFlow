using System.Windows;
using Microsoft.Win32;

namespace Urbanflow.src.frontend.dialogs
{
	/// <summary>
	/// Interaction logic for AddCityDialog.xaml
	/// </summary>
	public partial class AddCityDialog : Window
	{
		public string CityName => CityNameTextBox.Text;
		public string SelectedFolder => FolderTextBox.Text;
		public string Description => CityDescriptiponTextBox.Text;

		public AddCityDialog()
		{
			InitializeComponent();
		}

		private void SelectFolder_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new OpenFileDialog
			{
				CheckFileExists = false,
				CheckPathExists = true,
				FileName = "Select Folder" // Dummy file name to show folder selection
			};
			if (dialog.ShowDialog() == true)
			{
				var folderPath = System.IO.Path.GetDirectoryName(dialog.FileName);
				FolderTextBox.Text = folderPath;
			}
		}

		private void OK_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			Close();
		}

		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}
	}
}
