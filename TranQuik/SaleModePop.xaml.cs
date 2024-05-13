using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TranQuik
{
    public partial class SaleModePop : Window
    {
        private MainWindow mainWindow; // Reference to MainWindow

        public SaleModePop(MainWindow mainWindow)
        {
            InitializeComponent();
            this.mainWindow = mainWindow; // Store reference to MainWindow
        }

        private void SetSaleModeAndClose(int saleMode, string buttonName)
        {
            if (mainWindow != null)
            {
                mainWindow.SaleMode = saleMode; // Set SaleMode property of MainWindow

                // Find the button by name within the mainWindow's visual tree
                Button clickedButton = mainWindow.FindName(buttonName) as Button;
                if (clickedButton != null)
                {
                    // Example: Set background color of the clicked button based on a static resource
                    clickedButton.Background = (Brush)Application.Current.FindResource("AccentColor");
                }
            }

            mainWindow.StatusCondition.Text = buttonName;
            mainWindow.ProductGroupLoad();
            this.Close(); // Close SaleModePop window
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SetSaleModeAndClose(1, "DineIn"); // Set SaleMode to 1 and close window
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            SetSaleModeAndClose(2, "TakeAway"); // Set SaleMode to 2 and close window
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            SetSaleModeAndClose(3, "DriveThru"); // Set SaleMode to 3 and close window
        }
    }

}
