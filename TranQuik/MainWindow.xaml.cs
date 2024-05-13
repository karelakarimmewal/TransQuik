using System.Collections.Generic;
using System;
using System.Windows;
using System.Windows.Controls;
using TranQuik.Configuration;
using TranQuik.Model;
using System.Windows.Media;
using System.Windows.Documents;
using System.Linq;
using TranQuik.Pages;
using System.Windows.Media.Effects;
using MySql.Data.MySqlClient;

namespace TranQuik
{
    public partial class MainWindow : Window
    {
        // Cart Management
        private List<Product> cartProducts = new List<Product>(); // List to store products in the cart
        private List<string> productGroupNames;
        private List<int> productGroupIds;
        private int taxPercentage = 11; // Tax percentage
        private decimal discountPercentage = 0; // Discount percentage

        // Database Settings
        private LocalDbConnector localDbConnector;
        private ModelProcessing modelProcessing;
        private ProductDetails ProductDetails;

        // User Interface Elements
        private List<Button> productGroupButtons = new List<Button>(); // List of buttons for product groups
        //private SecondaryWindow secondaryWindow; // Secondary window reference

        // Application State
        public int SaleMode = 0; // Sale mode indicator
        private int currentIndex = 0; // Current index state
        private int batchSize = 15; // Batch size for data operations
        private int startIndex = 0; // Start index for data display
        private int endIndex = 0;
        private int visibleButtonCounts = 8; // Initial visible button count
        private int productButtonStartIndex = 0; // Start index for product buttons
        private int productButtonCount = 24; // Total count of product buttons
        private const int ButtonShiftAmount = 8; // Define the shift amount
        private const int ProductButtonShiftAmount = 24; // Define the shift amount


        // Payment and Display Data
        private List<int> payTypeIDs = new List<int>(); // List of payment type IDs
        private List<string> displayNames = new List<string>(); // List of display names
        private List<bool> isAvailableList = new List<bool>(); // List of availability statuses

        // Cart and Customer Management
        private Dictionary<DateTime, HeldCart> heldCarts = new Dictionary<DateTime, HeldCart>(); // Dictionary of held carts
        private int nextCustomerId = 1; // Next available customer ID

        private SaleModePop SaleModePop;

        public MainWindow()
        {
            // Load application settings (if needed)
            Config.LoadAppSettings();
            modelProcessing = new ModelProcessing(this);
            this.localDbConnector = new LocalDbConnector();
            InitializeComponent();

            // Handle the Loaded event to show SaleModePop after MainWindow is fully loaded
            Loaded += WindowLoaded;
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            SaleModePop saleModeWindow = new SaleModePop(this); // Pass reference to MainWindow
            saleModeWindow.Topmost = true;
            saleModeWindow.ShowDialog(); // Show SaleModePop window as modal
        }

        public void ProductGroupLoad()
        {
            // Check if SaleMode is greater than zero
            if (SaleMode > 0)
            {
                try
                {
                    // Load product group names and IDs
                    List<string> productGroupNames;
                    List<int> productGroupIds;
                    modelProcessing.GetProductGroupNamesAndIds(out productGroupNames, out productGroupIds);
                    // Create buttons for each product group and add them to the WrapPanel
                    for (int i = 0; i < productGroupNames.Count; i++)
                    {
                        Button button = new Button
                        {
                            Content = productGroupNames[i],
                            Height = 50,
                            Width = 99,
                            FontWeight = FontWeights.Bold,
                            BorderThickness = new Thickness(0),
                            Tag = productGroupIds[i],
                            Foreground = (SolidColorBrush)FindResource("FontColor"),
                            Background = Brushes.Azure,
                            Effect = (System.Windows.Media.Effects.Effect)FindResource("DropShadowEffect"),
                            Style = (Style)FindResource("ButtonStyle")
                        };

                        button.Click += GroupClicked;
                        ProductGroupName.Children.Add(button);
                        productGroupButtons.Add(button);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

            }
        }
        private void GroupClicked(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                int productGroupId = Convert.ToInt32(button.Tag);
                MainContentProduct.Visibility = Visibility.Visible;
                PayementProcess.Visibility = Visibility.Collapsed;
                modelProcessing.LoadProductDetails(productGroupId);
            }
        }

        private void UpdateVisibleButtons()
        {
            // Determine the endIndex based on startIndex and visibleButtonCount
            endIndex = Math.Min(startIndex + visibleButtonCounts, productGroupButtons.Count);

            for (int i = 0; i < productGroupButtons.Count; i++)
            {
                // Check if the current button index is within the visible range
                if (i >= startIndex && i < endIndex)
                {
                    // Show the button if it's within the visible range
                    productGroupButtons[i].Visibility = Visibility.Visible;
                }
                else
                {
                    // Hide the button if it's outside the visible range
                    productGroupButtons[i].Visibility = Visibility.Collapsed;
                }
            }
        }

        

        private void ButtonUp_Click(object sender, RoutedEventArgs e)
        {
            if (startIndex > 0)
            {
                // Decrement startIndex by ButtonShiftAmount to shift the visible range downwards
                startIndex = Math.Max(0, startIndex - ButtonShiftAmount);
                UpdateVisibleButtons();
            }
        }

        private void ButtonDown_Click(object sender, RoutedEventArgs e)
        {
            if (startIndex + visibleButtonCounts < productGroupButtons.Count)
            {
                // Increment startIndex by ButtonShiftAmount to shift the visible range upwards
                startIndex = Math.Min(productGroupButtons.Count - visibleButtonCounts, startIndex + ButtonShiftAmount);
                UpdateVisibleButtons();
            }
        }


        private void UpdateVisibleProductGroupButtons()
        {
            // Determine the endIndex based on productButtonStartIndex and productButtonCount
            int endIndex = Math.Min(productButtonStartIndex + productButtonCount, MainContentProduct.Children.Count);
            Console.WriteLine(MainContentProduct.Children.Count);
            for (int i = 0; i < MainContentProduct.Children.Count; i++)
            {
                // Check if the current button index is within the visible range
                if (i >= productButtonStartIndex && i < endIndex)
                {
                    // Show the button if it's within the visible range
                    MainContentProduct.Children[i].Visibility = Visibility.Visible;
                }
                else
                {
                    // Hide the button if it's outside the visible range
                    MainContentProduct.Children[i].Visibility = Visibility.Collapsed;
                }
            }
        }

        private void ScrollProductGroupsUp_Click(object sender, RoutedEventArgs e)
        {
            if (productButtonStartIndex > 0)
            {
                // Decrement productButtonStartIndex by ProductButtonShiftAmount to shift the visible range upwards
                productButtonStartIndex = Math.Max(0, productButtonStartIndex - ProductButtonShiftAmount);
                UpdateVisibleProductGroupButtons();
            }
        }

        private void ScrollProductGroupsDown_Click(object sender, RoutedEventArgs e)
        {
            if (productButtonStartIndex + productButtonCount < MainContentProduct.Children.Count)
            {
                // Increment productButtonStartIndex by ProductButtonShiftAmount to shift the visible range downwards
                productButtonStartIndex = Math.Min(MainContentProduct.Children.Count - productButtonCount, productButtonStartIndex + ProductButtonShiftAmount);
                UpdateVisibleProductGroupButtons();
            }
        }

        // Assuming you have a ListView bound to a collection of Product objects

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
    if (sender is ListView listView && listView.SelectedItem != null)
    {
        dynamic selectedItem = listView.SelectedItem;

        if (selectedItem != null)
        {
            int productId = selectedItem.ProductId;
            string productName = selectedItem.ProductName;
            decimal productPrice = Convert.ToDecimal(selectedItem.ProductPrice);
            int quantity = selectedItem.Quantity;

            // Create a new instance of Product
            Product selectedProduct = new Product(productId, productName, productPrice)
            {
                Quantity = quantity // Set the quantity based on the selected item
            };

            // Add child items to the selected product
            selectedProduct.ChildItems.Add("AD Mayo");
            selectedProduct.ChildItems.Add("AD Chip");

            // Display details of the selected product using MessageBox
            MessageBox.Show($"Selected Product: {selectedProduct.ProductName}\n" +
                            $"Price: {selectedProduct.ProductPrice:C}\n" +
                            $"Quantity: {selectedProduct.Quantity}\n" +
                            $"Total Price: {(selectedProduct.ProductPrice * selectedProduct.Quantity):C}");

            // Add the selected product back to the cartProducts list
            // You may want to replace the existing product with the updated one
            bool productFound = false;
            foreach (var product in cartProducts)
            {
                if (product.ProductId == selectedProduct.ProductId)
                {
                    productFound = true;
                    // Update the existing product with the updated one
                    product.Quantity = selectedProduct.Quantity;
                    product.ChildItems = selectedProduct.ChildItems;
                    break;
                }
            }

            if (!productFound)
            {
                // Add the selected product to cartProducts if it's not already in the list
                cartProducts.Add(selectedProduct);
            }

            // Output cartProducts to the console for debugging
            Console.WriteLine("Cart Products:");
            foreach (var product in cartProducts)
            {
                Console.WriteLine($"Product ID: {product.ProductId}");
                Console.WriteLine($"Product Name: {product.ProductName}");
                Console.WriteLine($"Product Price: {product.ProductPrice:C}");
                Console.WriteLine($"Quantity: {product.Quantity}");
                Console.WriteLine($"Status: {(product.Status ? "Active" : "Inactive")}");

                Console.WriteLine("Child Items:");
                foreach (var childItem in product.ChildItems)
                {
                    Console.WriteLine($"- {childItem}");
                }

                Console.WriteLine(); // Blank line for separation
            }

            // Refresh the UI to reflect changes (if needed)
            modelProcessing.UpdateCartUI();
        }
    }
}

        private void shutDownTrigger(object sender, RoutedEventArgs e)
        {
            // Create and show the ShutDownPopup window
            ShutDownPopup shutDownPopup = new ShutDownPopup();
            shutDownPopup.Topmost = true;
            shutDownPopup.ShowDialog();
        }

        private void payCashButton(object sender, RoutedEventArgs e)
        {
            // Clear all child elements (buttons) from the MainContentProduct wrap panel
            MainContentProduct.Children.Clear();
            PayementProcess.Visibility = Visibility.Visible;
            MainContentProduct.Visibility = Visibility.Hidden;
            CalculatorShowed.Visibility = Visibility.Visible;
            Calculating();
        }

        private void PayButton_Click(object sender, RoutedEventArgs e)
        {
            MainContentProduct.Children.Clear();
            PayementProcess.Visibility = Visibility.Visible;
            MainContentProduct.Visibility = Visibility.Hidden;
            CalculatorShowed.Visibility = Visibility.Hidden;
            Calculating();
            AddButtonGridToPaymentMethod();
        }


        private void Calculating()
        {
            // Parse the text values into double (assuming they represent numbers)
            if (double.TryParse(displayText.Text, out double currentTextValue) &&
                double.TryParse(GrandTotalCalculator.Text, out double grandTotalValue))
            {
                // Perform the subtraction
                double returnAmount = currentTextValue - grandTotalValue;

                // Update the TotalReturnCalculator with the calculated value
                TotalReturnCalculator.Text = returnAmount.ToString("#,0");
            }
            else
            {
                // Handle parsing failure or invalid input (e.g., non-numeric text)
                MessageBox.Show("Invalid input. Please enter valid numeric values.");
            }
        }

        private void AddButtonGridToPaymentMethod()
        {
            // Clear existing children in PaymentMethod
            PaymentMethod.Children.Clear();

            // Create a new Grid to contain the buttons
            Grid buttonGrid = new Grid();

            // Define rows and columns for the button grid
            for (int i = 0; i < 3; i++) // 3 rows
            {
                buttonGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            }

            for (int j = 0; j < 5; j++) // 5 columns
            {
                buttonGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            }

            // Connect to MySQL database and retrieve data
            string query = "SELECT PayTypeID, DisplayName, IsAvailable FROM paytype";

            using (MySqlConnection connection = localDbConnector.GetMySqlConnection())
            {
                MySqlCommand command = new MySqlCommand(query, connection);
                connection.Open();
                MySqlDataReader reader = command.ExecuteReader();

                // Populate lists with PayTypeID, DisplayName, and IsAvailable
                while (reader.Read())
                {
                    int payTypeID = reader.GetInt32(0); // PayTypeID (index 0)
                    string displayName = reader.GetString(1); // DisplayName (index 1)
                    bool isAvailable = reader.GetBoolean(2); // IsAvailable (index 2)

                    payTypeIDs.Add(payTypeID);
                    displayNames.Add(displayName);
                    isAvailableList.Add(isAvailable);
                }

                reader.Close();
            }

            // Display buttons for the current batch
            DisplayCurrentBatch(buttonGrid);

            // Add the button grid to the PaymentMethod grid
            PaymentMethod.Children.Add(buttonGrid);
        }
        private void DisplayCurrentBatch(Grid buttonGrid)
        {
            // Clear existing children in the button grid
            buttonGrid.Children.Clear();

            int startIndex = currentIndex;
            int endIndex = Math.Min(currentIndex + batchSize, payTypeIDs.Count);

            for (int index = startIndex; index < endIndex; index++)
            {
                int row = (index - startIndex) / 5;
                int col = (index - startIndex) % 5;

                // Retrieve PayTypeID, DisplayName, and IsAvailable values
                int payTypeID = payTypeIDs[index];
                string displayName = displayNames[index];
                bool isAvailable = isAvailableList[index];

                // Determine the button's background color based on IsAvailable
                Brush backgroundColor = isAvailable ? Brushes.Azure : (Brush)FindResource("DisabledButtonColor");
                Brush ForeGroundColor = isAvailable ? Brushes.Black : (Brush)FindResource("FontColor");


                Button button = new Button
                {
                    Content = displayName,
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(2),
                    Padding = new Thickness(10),
                    Background = backgroundColor,
                    Foreground = ForeGroundColor,
                    IsEnabled = isAvailable
                };

                // Attach click event handler to the button
                button.Click += (sender, e) => Button_Clicks(sender, e);


                // Apply DropShadowEffect to the button
                button.Effect = FindResource("DropShadowEffect") as DropShadowEffect;

                // Add the button to the button grid
                Grid.SetRow(button, row);
                Grid.SetColumn(button, col);
                buttonGrid.Children.Add(button);
            }

            // Add "Prev" button to the last row, first column
            Button prevButton = new Button
            {
                Content = "<-",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(2),
                Padding = new Thickness(10),
                Background = (Brush)FindResource("AccentColor"),
                Foreground = Brushes.White
            };
            prevButton.Effect = FindResource("DropShadowEffect") as DropShadowEffect;
            prevButton.Click += PrevButton_Click;
            Grid.SetRow(prevButton, 2); // Last row (index 2)
            Grid.SetColumn(prevButton, 0); // First column
            buttonGrid.Children.Add(prevButton);

            // Add "Next" button to the last row, last column
            Button nextButton = new Button
            {
                Content = "->",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(2),
                Padding = new Thickness(10),
                Background = (Brush)FindResource("AccentColor"),
                Foreground = Brushes.White,
            };
            nextButton.Effect = FindResource("DropShadowEffect") as DropShadowEffect;
            nextButton.Click += NextButton_Click;
            Grid.SetRow(nextButton, 2); // Last row (index 2)
            Grid.SetColumn(nextButton, 4); // Last column
            buttonGrid.Children.Add(nextButton);
        }
        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            currentIndex += batchSize;
            DisplayCurrentBatch((Grid)PaymentMethod.Children[0]); // Assuming first child is the button grid
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            currentIndex -= batchSize;
            if (currentIndex < 0)
                currentIndex = 0;

            DisplayCurrentBatch((Grid)PaymentMethod.Children[0]); // Assuming first child is the button grid
        }

        private void Button_Clicks(object sender, RoutedEventArgs e)
        {
            // Handle button click event here
            if (sender is Button button)
            {
                // Get the content (DisplayName) of the clicked button
                string displayName = button.Content?.ToString();

                // Find the index of the DisplayName in the displayNames list
                int index = displayNames.IndexOf(displayName);

                if (index != -1 && index < payTypeIDs.Count)
                {
                    // Retrieve the corresponding PayTypeID
                    int payTypeID = payTypeIDs[index];

                    //secondaryWindow.Payment.Text = displayName;
                    // Display the message box with the DisplayName and PayTypeID

                    if (payTypeID == 10011)
                    {
                        // Call QRIS method
                        //QRIS();
                    }
                    MessageBox.Show($"Waiting For Payment Using: {displayName}\nPayTypeID: {payTypeID}");
                }
                else
                {
                    MessageBox.Show("Invalid button content", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        
    }
}
