using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using TranQuik.Configuration;
using TranQuik.Model;
using TranQuik.Pages;

namespace TranQuik
{
    public partial class MainWindow : Window
    {
        // Cart Management

        List<int> payTypeIDsList = new List<int>();
        private List<int> productGroupIds;
        private int taxPercentage = 11; // Tax percentage
        private decimal discountPercentage = 0; // Discount percentage

        // Database Settings
        private LocalDbConnector localDbConnector;
        private ModelProcessing modelProcessing;
        private ProductDetails ProductDetails;

        // User Interface Elements
        public List<Button> productGroupButtons = new List<Button>(); // List of buttons for product groups
        //private SecondaryWindow secondaryWindow; // Secondary window reference

        // Application State
        public int SaleMode = 0; // Sale mode indicator
        private int currentIndex = 0; // Current index state
        private int batchSize = 15; // Batch size for data operations
        public int startIndex = 0; // Start index for data display
        public int endIndex = 0;
        public int visibleButtonCounts = 8; // Initial visible button count
        public int productButtonStartIndex = 0; // Start index for product buttons
        public int productButtonCount = 24; // Total count of product buttons
        private const int ButtonShiftAmount = 8; // Define the shift amount
        private const int ProductButtonShiftAmount = 24; // Define the shift amount


        // Payment and Display Data
        private List<int> payTypeIDs = new List<int>(); // List of payment type IDs
        private List<string> displayNames = new List<string>(); // List of display names
        private List<bool> isAvailableList = new List<bool>(); // List of availability statuses
        public SecondaryMonitor secondaryMonitor;

        // Cart and Customer Management
        private Dictionary<DateTime, HeldCart> heldCarts = new Dictionary<DateTime, HeldCart>(); // Dictionary of held carts
        private int nextCustomerId = 1; // Next available customer ID

        private SaleModePop SaleModePop;

        public MainWindow()
        {
            // Load application settings (if needed)
            Rect workingArea = SystemParameters.WorkArea;
            Config.LoadAppSettings();
            modelProcessing = new ModelProcessing(this);
            this.localDbConnector = new LocalDbConnector();
            InitializeComponent();
            GetPayTypeList((Properties.Settings.Default._ComputerID));
            // Handle the Loaded event to show SaleModePop after MainWindow is fully loaded
            Loaded += WindowLoaded;
            
            if (workingArea.Width <= 1038)
            {
                this.WindowState = WindowState.Maximized;
            }
            VatNumber.Text = $"{modelProcessing.vatDesp}";

            if (Properties.Settings.Default._AppSecMonitor)
            {
                secondaryMonitor = new SecondaryMonitor(modelProcessing);
                secondaryMonitor.Topmost = true;
                secondaryMonitor.Show(); // Show the SecondaryWindow
            }
        }

        public void SaleModeView()
        {
            SaleModePop saleModeWindow = new SaleModePop(this); // Pass reference to MainWindow
            saleModeWindow.Topmost = true;
            saleModeWindow.ShowDialog(); // Show SaleModePop window as modal
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            SaleModeView();
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

        private void ButtonUp_Click(object sender, RoutedEventArgs e)
        {
            if (startIndex > 0)
            {
                // Decrement startIndex by ButtonShiftAmount to shift the visible range downwards
                startIndex = Math.Max(0, startIndex - ButtonShiftAmount);
                modelProcessing.UpdateVisibleButtons();
            }
        }

        private void ButtonDown_Click(object sender, RoutedEventArgs e)
        {
            if (startIndex + visibleButtonCounts < productGroupButtons.Count)
            {
                // Increment startIndex by ButtonShiftAmount to shift the visible range upwards
                startIndex = Math.Min(productGroupButtons.Count - visibleButtonCounts, startIndex + ButtonShiftAmount);
                modelProcessing.UpdateVisibleButtons();
            }
        }

        private void ScrollProductGroupsUp_Click(object sender, RoutedEventArgs e)
        {
            if (productButtonStartIndex > 0)
            {
                // Decrement productButtonStartIndex by ProductButtonShiftAmount to shift the visible range upwards
                productButtonStartIndex = Math.Max(0, productButtonStartIndex - ProductButtonShiftAmount);
                modelProcessing.UpdateVisibleProductGroupButtons();
            }
        }

        private void ScrollProductGroupsDown_Click(object sender, RoutedEventArgs e)
        {
            if (productButtonStartIndex + productButtonCount < MainContentProduct.Children.Count)
            {
                // Increment productButtonStartIndex by ProductButtonShiftAmount to shift the visible range downwards
                productButtonStartIndex = Math.Min(MainContentProduct.Children.Count - productButtonCount, productButtonStartIndex + ProductButtonShiftAmount);
                modelProcessing.UpdateVisibleProductGroupButtons();
            }
        }

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
                    foreach (var product in modelProcessing.cartProducts)
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
                        modelProcessing.cartProducts.Add(selectedProduct);
                    }

                    // Output cartProducts to the console for debugging
                    Console.WriteLine("Cart Products:");
                    foreach (var product in modelProcessing.cartProducts)
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
            modelProcessing.Calculating();
        }

        private void PayButton_Click(object sender, RoutedEventArgs e)
        {
            MainContentProduct.Children.Clear();
            PayementProcess.Visibility = Visibility.Visible;
            MainContentProduct.Visibility = Visibility.Hidden;
            CalculatorShowed.Visibility = Visibility.Hidden;
            modelProcessing.Calculating();
            AddButtonGridToPaymentMethod();
        }


        // Sample method to retrieve and convert payTypeList from local MySQL database
        private int[] GetPayTypeList(int computerID)
        {
            Console.WriteLine($"Computer ID: {computerID}");

            // Define SQL query to retrieve PayTypeList based on ComputerID
            string query = "SELECT PayTypeList FROM computername WHERE ComputerID = @ComputerID";

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
                        string payTypeListString = result.ToString();

                        // Split the string into individual IDs and convert to integer array
                        string[] payTypeIDsArray = payTypeListString.Split(',');
                        foreach (string payTypeIDStr in payTypeIDsArray)
                        {
                            if (int.TryParse(payTypeIDStr, out int payTypeID))
                            {
                                payTypeIDsList.Add(payTypeID);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("PayTypeList not found for the specified ComputerID.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error retrieving PayTypeList: {ex.Message}");
                    // Handle any database connection or query execution exceptions
                }
            }

            return payTypeIDsList.ToArray();
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

                // Check if the payTypeID is available in the list
                if (payTypeIDsList.Contains(payTypeID))
                {
                    // Determine the button's background color based on IsAvailable
                    Brush backgroundColor = isAvailable ? Brushes.Azure : (Brush)FindResource("DisabledButtonColor");
                    Brush foregroundColor = isAvailable ? Brushes.Black : (Brush)FindResource("FontColor");

                    Button button = new Button
                    {
                        Content = displayName,
                        FontSize = 12,
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(2),
                        Padding = new Thickness(10),
                        Background = backgroundColor,
                        Foreground = foregroundColor,
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

                    if (payTypeID == 1)
                    {
                        Button simulateButton = new Button();  // Create a new button instance (this could be any UI element)
                        RoutedEventArgs args = new RoutedEventArgs();  // Create new instance of RoutedEventArgs
                        payCashButton(simulateButton, args);
                    }
                    else if (payTypeID == 10011)
                    {
                        // Call QRIS method
                        //QRIS();
                    }
                    //MessageBox.Show($"Waiting For Payment Using: {displayName}\nPayTypeID: {payTypeID}");
                }
                else
                {
                    MessageBox.Show("Invalid button content", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void HoldButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the current timestamp
            DateTime timeStamp = DateTime.Now;
            Console.WriteLine("CLicked");

            // Create a deep copy of the current cart products
            List<Product> currentCartProducts = new List<Product>(modelProcessing.cartProducts);

            // Generate a new customer ID
            int customerId = nextCustomerId++;

            // Create a new HeldCart instance
            HeldCart heldCart = new HeldCart(customerId, timeStamp, currentCartProducts);

            // Add the held cart to the dictionary with the timestamp as the key
            heldCarts.Add(timeStamp, heldCart);

            // Notify the user that the cart has been held
            MessageBox.Show($"Cart has been held for Customer ID: {customerId} at {timeStamp}. You can access it later.");

            // Clear the cart products after holding
            modelProcessing.cartProducts.Clear();

            // Update the cart UI
            modelProcessing.UpdateCartUI();
            DisplayHeldCartsInConsole();
        }

        private void DisplayHeldCarts()
        {
            Console.WriteLine("Held Carts:");
            foreach (var kvp in heldCarts)
            {
                Console.WriteLine($"Timestamp: {kvp.Key}");
                Console.WriteLine($"Customer ID: {kvp.Value.CustomerId}");
                Console.WriteLine("Cart Products:");
                foreach (var product in kvp.Value.CartProducts)
                {
                    Console.WriteLine($"- {product.ProductName}");
                }
                Console.WriteLine(); // Add a blank line between each held cart entry
            }
        }

        public void UpdateSecondayMonitor()
        {
            secondaryMonitor.UpdateCartUI();
        }

        // Call this method to display the held carts in the console
        private void DisplayHeldCartsInConsole()
        {
            Console.WriteLine("Displaying Held Carts...");
            DisplayHeldCarts();
            Console.WriteLine("End of Held Carts");
        }

        private void ClearList_Click(object sender, RoutedEventArgs e)
        {
            modelProcessing.cartProducts.Clear();
            modelProcessing.UpdateCartUI();
        }

        private void ModifierButton_Click(object sender, RoutedEventArgs e)
        {

        }
















        private void Number_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                string buttonText = button.Content.ToString();

                // Append the clicked number or dot to the displayed text
                if (buttonText == ".")
                {
                    // Check if dot is already present in the display text
                    if (!displayText.Text.Contains("."))
                    {
                        displayText.Text += buttonText; // Append dot if not already present
                    }
                }
                else
                {
                    // Append the clicked number to the display text
                    displayText.Text += buttonText;
                }

                // Update the displayed text with formatting (thousands separators)
                modelProcessing.Calculating();
                modelProcessing.UpdateFormattedDisplay();
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            displayText.Text = "0";
            modelProcessing.Calculating();
        }

        private void Backspace_Click(object sender, RoutedEventArgs e)
        {
            if (displayText.Text.Length > 0)
                displayText.Text = displayText.Text.Substring(0, displayText.Text.Length - 1);
            modelProcessing.Calculating();
        }

        private void Enter_Click(object sender, RoutedEventArgs e)
        {
            string inputText = displayText.Text;

            // Parse the entered text into a double value
            if (double.TryParse(inputText, out double enteredAmount))
            {
                // Calculate the grand total value from the UI element
                if (double.TryParse(GrandTotalCalculator.Text, out double grandTotalValue))
                {
                    // Calculate the return amount
                    double returnAmount = enteredAmount - grandTotalValue;

                    if (returnAmount < 0)
                    {
                        // Display a message indicating insufficient funds
                        MessageBox.Show("Insufficient funds. Please enter more money to proceed.");
                        return; // Exit the method without further processing
                    }
                    else
                    {
                        // Proceed with the rest of the processing
                        MessageBox.Show($"Entered value: {enteredAmount}");

                        modelProcessing.ResetUI();
                    }
                }
                else
                {
                    MessageBox.Show("Invalid grand total value. Please check the total amount.");
                }
            }
            else
            {
                MessageBox.Show("Invalid input. Please enter a valid number.");
            }
        }

        
    }
}
