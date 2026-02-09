using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Urbanflow.src.frontend.dialogs
{
	/// <summary>
	/// Interaction logic for EditWorkflowDialog.xaml
	/// </summary>
	public partial class EditWorkflowDialog : Window
	{
		public string WorkflowName => NameBox.Text;
		public string WorkflowDescription => DescriptionBox.Text;

		public EditWorkflowDialog(string name, string description)
		{
			InitializeComponent();
			NameBox.Text = name;
			DescriptionBox.Text = description;
		}

		private void Ok_Click(object sender, RoutedEventArgs e)
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
