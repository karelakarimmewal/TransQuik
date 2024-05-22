using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using TranQuik.Model;

namespace TranQuik.Pages
{
    /// <summary>
    /// Interaction logic for TransactionStatus.xaml
    /// </summary>
    public partial class TransactionStatus : Window
    {
        private ModelProcessing modelProcessing;

        public TransactionStatus(bool TStatus, ModelProcessing modelProcessing)
        {
            InitializeComponent();

            // Store the reference to ModelProcessing
            this.modelProcessing = modelProcessing;

            // Call the method to handle transaction status
            HandleTransactionStatus(TStatus);
        }

        private void HandleTransactionStatus(bool TStatus)
        {
            switch (TStatus)
            {
                case true:
                    // Transaction is successful
                    SuccessCase();
                    break;
                case false:
                    // Transaction has failed
                    FailedCase();
                    break;
                default:
                    // Handle other cases if needed
                    break;
            }
        }

        private void SuccessCase()
        {
            transactionStatus.Text = "Transaction Success";
            transactionStatus.Foreground = (Brush)Application.Current.FindResource("SuccessColor");
        }

        private void FailedCase()
        {
            transactionStatus.Text = "Transaction Failed";
            transactionStatus.Foreground = (Brush)Application.Current.FindResource("ErrorColor");
        }

        private async void doneButton(object sender, RoutedEventArgs e)
        {
            // Close the window asynchronously
            await CloseWindowAsync();
        }

        private async Task CloseWindowAsync()
        {
            // Close the window
            this.Close();

            // Wait a moment for the window to close
            await Task.Delay(100); // Adjust delay time as needed

            // Reset the UI
            modelProcessing.ResetUI();
        }
    }
}
