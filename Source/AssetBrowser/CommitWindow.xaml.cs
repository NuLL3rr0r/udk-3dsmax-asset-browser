using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AssetBrowser
{
    /// <summary>
    /// Interaction logic for CommitWindow.xaml
    /// </summary>
    public partial class CommitWindow : Window
    {
        public bool IsAccepted
        {
            get;
            private set;
        }

        public string Message
        {
            get
            {
                return messageTextBox.Text;
            }
        }

        public CommitWindow()
        {
            IsAccepted = false;

            InitializeComponent();
        }

        private void commitButton_Click(object sender, RoutedEventArgs e)
        {
            IsAccepted = true;
            Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            IsAccepted = false;
            Close();
        }

        private void messageTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(messageTextBox.Text.Trim()))
            {
                if (!commitButton.IsEnabled)
                    commitButton.IsEnabled = true;
            }
            else
            {
                if (commitButton.IsEnabled)
                    commitButton.IsEnabled = false;
            }
        }
    }
}
