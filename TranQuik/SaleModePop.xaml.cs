using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Material.Icons;
using Material.Icons.WPF;
using MySql.Data.MySqlClient;
using TranQuik.Configuration;
using TranQuik.Model;

namespace TranQuik
{
    public partial class SaleModePop : Window
    {
        private MainWindow mainWindow; // Reference to MainWindow
        private LocalDbConnector localDbConnector;
        private List<int> getSalesModeList= new List<int>();
        private List<int> salesModeList = new List<int>();
        private int buttonHeight;
        private int buttonWidth;
        private int columns;

        public SaleModePop(MainWindow mainWindow)
        {
            InitializeComponent();
            this.mainWindow = mainWindow; // Store reference to MainWindow
            this.localDbConnector = new LocalDbConnector();
            columns = 5; // Assuming 3 columns
            buttonWidth = 150; // Adjust as needed based on the button size
            buttonHeight = 110; // Adjust as needed based on the button size

            GetPayTypeList(AppSettings.ComputerID);
            List<SaleMode> saleModes = GetSaleModes(); // Retrieve SaleMode data from the database
            CreateButtonsForSalesModes(saleModes); // Create buttons based on retrieved SaleMode data
                                                   // Calculate the required size of the window based on the button layout
            int rowCount = (int)Math.Ceiling((double)saleModes.Count / columns);
            double totalWidth = columns * buttonWidth;
            double totalHeight = rowCount * buttonHeight;

            // Set the window size based on the calculated button layout size
            this.Width = totalWidth + 3; // Add extra width for margins and spacing
            this.Height = totalHeight + 3 ; // Add extra height for title bar and spacing
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
            SaleModeIconMapper iconMapper = new SaleModeIconMapper();

            // Create a UniformGrid to contain the buttons
            UniformGrid uniformGrid = new UniformGrid
            {
                Columns = columns, // Set the number of columns in the grid (adjust as needed)
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0) // Add margin to the grid for spacing
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
        }

        private void Button_Click(object sender, RoutedEventArgs e, int saleModeID, string saleModeName)
        {
            SetSaleModeAndClose(saleModeID, saleModeName);
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
