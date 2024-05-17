using Material.Icons;
using Material.Icons.WPF;
using MySql.Data.MySqlClient;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Effects;
using TranQuik.Configuration;
using TranQuik.Model;
using TranQuik.Pages;

namespace TranQuik
{
    public partial class SaleModePop : Window
    {
        private MainWindow mainWindow; // Reference to MainWindow
        private LocalDbConnector localDbConnector;
        private List<int> getSalesModeList= new List<int>();
        private List<int> salesModeList = new List<int>();
        private List<SaleMode> saleModes;
        private int buttonHeight;
        private int buttonWidth;
        private int columns;
        private bool isHoldBillList; // Declare the field without initialization
        public int CustomerID;
        public DateTime TimeNow;

        public SaleModePop(MainWindow mainWindow)
        {
            InitializeComponent();
            this.mainWindow = mainWindow; // Store reference to MainWindow
            this.localDbConnector = new LocalDbConnector();
            columns = 5; // Assuming 3 columns
            buttonWidth = 150; // Adjust as needed based on the button size
            buttonHeight = 110; // Adjust as needed based on the button size

            GetPayTypeList(AppSettings.ComputerID);
            saleModes = GetSaleModes(); // Retrieve SaleMode data from the database
            
            CreateButtonsForSalesModes(saleModes); // Create buttons based on retrieved SaleMode data
                                                   // Calculate the required size of the window based on the button layout
            int rowCount = (int)Math.Ceiling((double)saleModes.Count / columns);
            double totalWidth = columns * buttonWidth;
            double totalHeight = rowCount * buttonHeight;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            SizeToContent = SizeToContent.WidthAndHeight;
        }
        public void GetPayTypeList(int computerID)
        {
            Console.WriteLine($"Computer ID: {computerID}");

            string query = "SELECT SaleModeList FROM computername WHERE ComputerID = @ComputerID";

            using (MySqlConnection connection = localDbConnector.GetMySqlConnection())
            {
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@ComputerID", computerID);

                try
                {
                    connection.Open();
                    object result = command.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        string salesModeListString = result.ToString();

                        // Split the string into individual IDs and convert to integer array
                        string[] salesModeIDsArray = salesModeListString.Split(',');
                        foreach (string salesModeIDStr in salesModeIDsArray)
                        {
                            if (int.TryParse(salesModeIDStr, out int salesModeID))
                            {
                                getSalesModeList.Add(salesModeID);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("SaleModeList not found for the specified ComputerID.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error retrieving SaleModeList: {ex.Message}");
                }
            }
        }

        public List<SaleMode> GetSaleModes()
        {
            List<SaleMode> saleModes = new List<SaleMode>();

            string query = "SELECT SaleModeID, SaleModeName, ReceiptHeaderText, NOTinPayTypeList, PrefixText, PrefixQueue FROM SaleMode";

            using (MySqlConnection connection = localDbConnector.GetMySqlConnection())
            {
                MySqlCommand command = new MySqlCommand(query, connection);

                try
                {
                    connection.Open();
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            SaleMode saleMode = new SaleMode
                            {
                                SaleModeID = reader.GetInt32("SaleModeID"),
                                SaleModeName = reader.GetString("SaleModeName"),
                                ReceiptHeaderText = reader.GetString("ReceiptHeaderText"),
                                NotInPayTypeList = reader.GetString("NOTinPayTypeList"),
                                PrefixText = reader.GetString("PrefixText"),
                                PrefixQueue = reader.GetString("PrefixQueue")
                            };

                            saleModes.Add(saleMode);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error retrieving SaleMode data: {ex.Message}");
                }
            }

            return saleModes;
        }

        public void CreateButtonsForSalesModes(List<SaleMode> saleModes)
        {
            GridSaleMode.Children.Clear();
            SaleModeIconMapper iconMapper = new SaleModeIconMapper();
            isHoldBillList = mainWindow.heldCarts.Any();

            // Create a UniformGrid to contain the buttons
            UniformGrid uniformGrid = new UniformGrid
            {
                Columns = columns, // Set the number of columns in the grid (adjust as needed)
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(3) // Add margin to the grid for spacing
            };

            // Add the UniformGrid to GridSaleMode (assuming GridSaleMode is defined in XAML)
            GridSaleMode.Children.Add(uniformGrid);

            foreach (var saleMode in saleModes)
            {
                if (!getSalesModeList.Contains(saleMode.SaleModeID))
                {
                    continue; // Skip if saleModeID is not in salesModeList
                }

                // Determine the MaterialIconKind based on SaleModeID using SaleModeIconMapper
                MaterialIconKind iconKind = iconMapper.GetIconForSaleMode(saleMode.SaleModeID);

                // Determine the background color based on SaleModeID using SaleModeIconMapper
                Brush backgroundColor = iconMapper.GetColorForSaleMode(saleMode.SaleModeID);

                // Create a new Button
                Button button = new Button
                {
                    Height = buttonHeight,
                    Width = buttonWidth,
                    Style = (Style)Application.Current.Resources["ButtonStyle"],
                    Effect = (DropShadowEffect)Application.Current.Resources["DropShadowEffect"],
                    Background = backgroundColor ?? Brushes.Orange, // Set the button's background color (default to Orange if null)
                    Margin = new Thickness(1) // Add margin to the button for spacing
                };

                // Create StackPanel for content
                StackPanel stackPanel = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                // Add Icon to StackPanel based on iconKind
                MaterialIcon icon = new MaterialIcon
                {
                    Kind = iconKind,
                    Height = 24,
                    VerticalAlignment = VerticalAlignment.Center
                };
                stackPanel.Children.Add(icon);

                // Add TextBlock to StackPanel (SaleMode Name)
                TextBlock textBlock = new TextBlock
                {
                    Text = saleMode.SaleModeName,
                    Margin = new Thickness(0),
                    FontSize = 14,
                    VerticalAlignment = VerticalAlignment.Center
                };
                stackPanel.Children.Add(textBlock);

                // Add StackPanel to Button Content
                button.Content = stackPanel;

                // Add Click Event Handler with SaleModeID and SaleModeName parameters
                button.Click += (sender, e) => Button_Click(sender, e, saleMode.SaleModeID, saleMode.SaleModeName);

                // Add Button to UniformGrid
                uniformGrid.Children.Add(button);
            }
            Brush backgroundColors = isHoldBillList ? (Brush)Application.Current.Resources["AccentColor"] : Brushes.SlateGray;
            // Add "Hold Bill List" Button
            Button holdBillListButton = new Button
            {
                Width = buttonWidth,
                Height = buttonHeight,
                Style = (Style)Application.Current.Resources["ButtonStyle"],
                Effect = (DropShadowEffect)Application.Current.Resources["DropShadowEffect"],
                Background = backgroundColors,
                Margin = new Thickness(1),
                IsEnabled = isHoldBillList,
             };

            // Create a StackPanel to hold the icon and text
            StackPanel holdBillListContent = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Add MaterialIcon to the StackPanel
            MaterialIcon holdBillListIcon = new MaterialIcon
            {
                Kind = MaterialIconKind.CartCheck,
                Height = 24,
                VerticalAlignment = VerticalAlignment.Center
            };
            holdBillListContent.Children.Add(holdBillListIcon);

            // Add TextBlock for button text below the icon
            TextBlock holdBillListText = new TextBlock
            {
                Text = "Hold Bill List",
                Margin = new Thickness(0),
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center
            };
            holdBillListContent.Children.Add(holdBillListText);

            // Set the StackPanel as the content of the button
            holdBillListButton.Content = holdBillListContent;

            // Handle click event for Hold Bill List button
            holdBillListButton.Click += (sender, e) => HoldBillListButton_Click(sender, e);

            // Add the button to the UniformGrid
            uniformGrid.Children.Add(holdBillListButton);


            // Add "Exit Application" Button
            Button exitButton = new Button
            {
                Width = buttonWidth,
                Height = buttonHeight,
                Style = (Style)Application.Current.Resources["ButtonStyle"],
                Effect = (DropShadowEffect)Application.Current.Resources["DropShadowEffect"],
                Background = (Brush)Application.Current.Resources["AccentColor"], // Use AccentColor as the background
                Margin = new Thickness(1)
            };

            // Create a StackPanel to hold the icon and text
            StackPanel exitButtonContent = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Add MaterialIcon to the StackPanel
            MaterialIcon exitIcon = new MaterialIcon
            {
                Kind = MaterialIconKind.ExitToApp,
                Height = 24,
                VerticalAlignment = VerticalAlignment.Center
            };
            exitButtonContent.Children.Add(exitIcon);

            // Add TextBlock for button text below the icon
            TextBlock exitText = new TextBlock
            {
                Text = "Exit Application",
                Margin = new Thickness(0),
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center
            };
            exitButtonContent.Children.Add(exitText);

            // Set the StackPanel as the content of the button
            exitButton.Content = exitButtonContent;

            // Handle click event for Exit Application button
            exitButton.Click += (sender, e) => ExitButton_Click(sender, e);

            // Add the button to the UniformGrid
            uniformGrid.Children.Add(exitButton);


        }

        public void CreateButtonsForHeldCarts(Dictionary<DateTime, HeldCart> heldCarts)
        {
            GridSaleMode.Children.Clear();

            // Create a UniformGrid to contain the buttons
            UniformGrid uniformGrid = new UniformGrid
            {
                Columns = 5, // Set the number of columns to 5 (adjust as needed)
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(3) // Add margin to the grid for spacing
            };

            // Add the UniformGrid to the parent element (e.g., GridSaleMode with the actual parent control)
            GridSaleMode.Children.Add(uniformGrid);

            foreach (var heldCart in heldCarts)
            {
                // Create a new Button for the held cart
                Button button = new Button
                {
                    Height = buttonHeight,
                    Width = buttonWidth,
                    Style = (Style)Application.Current.Resources["ButtonStyle"],
                    Effect = (DropShadowEffect)Application.Current.Resources["DropShadowEffect"],
                    Background = (Brush)Application.Current.Resources["AccentColor"], // Set the button's background color
                    Margin = new Thickness(1) // Add margin to the button for spacing
                };

                // Create StackPanel for content
                StackPanel stackPanel = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                // Add Icon to StackPanel (customize the icon based on heldCart properties)
                MaterialIcon icon = new MaterialIcon
                {
                    Kind = MaterialIconKind.Person,
                    Height = 24,
                    VerticalAlignment = VerticalAlignment.Center
                };
                stackPanel.Children.Add(icon);

                // Add TextBlock to StackPanel (customize the text based on heldCart properties)
                TextBlock textBlock = new TextBlock
                {
                    Text = $"Customer: {heldCart.Value.CustomerId}\n{heldCart.Value.TimeStamp}",
                    Margin = new Thickness(0),
                    FontSize = 14,
                    VerticalAlignment = VerticalAlignment.Center
                };
                stackPanel.Children.Add(textBlock);

                // Add StackPanel to Button Content
                button.Content = stackPanel;

                // Add Click Event Handler for the held cart button
                button.Click += (sender, e) => HeldCartButton_Click(sender, e, heldCart.Key, heldCart.Value);

                // Add the Button to the UniformGrid
                uniformGrid.Children.Add(button);
            }

            // Create and add the "Back" button to the UniformGrid
            Button backButton = new Button
            {
                Content = "Back",
                Height = buttonHeight,
                Width = buttonWidth,
                Style = (Style)Application.Current.Resources["ButtonStyle"],
                Effect = (DropShadowEffect)Application.Current.Resources["DropShadowEffect"],
                Background = (Brush)Application.Current.Resources["AccentColor"],
                Margin = new Thickness(1)
            };

            // Add Click Event Handler for the "Back" button
            backButton.Click += (sender, e) => HandleBackButtonClick(sender, e);

            // Add the "Back" button to the UniformGrid
            uniformGrid.Children.Add(backButton);
        }



        private void HandleBackButtonClick(object sender, RoutedEventArgs e)
        {
            Log.ForContext("LogType", "ApplicationLog").Information($"Handle Back Button Clicked");
            CreateButtonsForSalesModes(saleModes);
        }
        private void HoldBillListButton_Click(object sender, RoutedEventArgs e)
        {
            Log.ForContext("LogType", "ApplicationLog").Information($"Hold List Button Clicked");
            CreateButtonsForHeldCarts(mainWindow.heldCarts);
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            ShutDownPopup shutDownPopup = new ShutDownPopup();
            shutDownPopup.Topmost = true;
            shutDownPopup.ShowDialog();
        }

        private void Button_Click(object sender, RoutedEventArgs e, int saleModeID, string saleModeName)
        {
            NewCustomer();
            Log.ForContext("LogType", "TransactionLog").Information($"Transaction {mainWindow.CustomerID} ({saleModeName}-{saleModeID})");
            SetSaleModeAndClose(saleModeID, saleModeName);
        }

        private void NewCustomer()
        {
            if (!mainWindow.isNew)
            {
                return;
            }
            Customer customer = new Customer(DateTime.Now);
            mainWindow.CustomerID = customer.CustomerId;
            mainWindow.CustomerTime = customer.Time;
        }

        private void HeldCartButton_Click(object sender, RoutedEventArgs e, DateTime timeStamp, HeldCart heldCart)
        {

            mainWindow.HoldBill(heldCart.CartProducts);
            mainWindow.CustomerID = heldCart.CustomerId;
            mainWindow.CustomerTime = heldCart.TimeStamp;
            mainWindow.SaleMode = heldCart.SalesMode;
            mainWindow.salesModeText.Text = heldCart.SalesModeName;
            this.Close();
        }

        private void SetSaleModeAndClose(int saleMode, string buttonName)
        {

            if (mainWindow != null)
            {
                mainWindow.SaleMode = saleMode; // Set SaleMode property of MainWindow
                mainWindow.salesModeSee.Background = (Brush)Application.Current.FindResource("AccentColor");
                mainWindow.salesModeText.Text = buttonName;
            }
            mainWindow.StatusCondition.Text = buttonName;
            mainWindow.ProductGroupLoad();
            this.Close(); 
        }
    }
}
