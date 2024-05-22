using Microsoft.CSharp.RuntimeBinder;
using MySql.Data.MySqlClient;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;

using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using TranQuik.Configuration;
using TranQuik.Model;
using TranQuik.Pages;


namespace TranQuik
{
    public partial class MainWindow : Window
    {
        // Cart Management
        List<int> payTypeIDsList = new List<int>();

        // Database Settings
        private LocalDbConnector localDbConnector;
        private ModelProcessing modelProcessing;
        private TransactionStatus transactionStatus;
        public HeldCartManager heldCartManager;

        // User Interface Elements
        public List<Button> productGroupButtons = new List<Button>(); // List of buttons for product groups

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
        public bool isNew = true;


        // Payment and Display Data
        private List<int> payTypeIDs = new List<int>(); // List of payment type IDs
        private List<string> displayNames = new List<string>(); // List of display names
        private List<bool> isAvailableList = new List<bool>(); // List of availability statuses
        public List<ChildItem> childItemsSelected = new List<ChildItem>();
        public SecondaryMonitor secondaryMonitor;

        // Cart and Customer Management
        public Dictionary<DateTime, HeldCart> heldCarts = new Dictionary<DateTime, HeldCart>(); // Dictionary of held carts

        public int OrderID { get; set; }
        public int paxTotal { get; set; }
        public DateTime CustomerTime { get; set; }

        public MainWindow()
        {
            // Load application settings (if needed)
            
            Rect workingArea = SystemParameters.WorkArea;
            Config.LoadAppSettings();
            this.localDbConnector = new LocalDbConnector();
            this.heldCartManager = new HeldCartManager();
            heldCarts = heldCartManager.LoadHeldCarts();
            InitializeComponent();
            GetPayTypeList((Properties.Settings.Default._ComputerID));
            Loaded += WindowLoaded;
            modelProcessing = new ModelProcessing(this, this.secondaryMonitor);
            if (workingArea.Width <= 1038)
            {
                this.WindowState = WindowState.Maximized;
            }
            VatNumber.Text = $"{modelProcessing.vatDesp}";

            if (Properties.Settings.Default._AppSecMonitor)
            {
                secondaryMonitor = new SecondaryMonitor(modelProcessing);
                secondaryMonitor.Topmost = true;
                secondaryMonitor.ShowInTaskbar = false;
                secondaryMonitor.Show(); // Show the SecondaryWindow
            }
        }

        public void SaleModeView()
        {
            SaleModePop saleModeWindow = new SaleModePop(this, this.modelProcessing); // Pass reference to MainWindow
            saleModeWindow.Topmost = true;
            saleModeWindow.ShowInTaskbar = false;
            saleModeWindow.ShowDialog(); // Show SaleModePop window as modal
        }

        public void ModifierMenuView(int productID)
        {
            MenuModifier menuModifier = new MenuModifier(this, modelProcessing, productID); // Pass reference to MainWindow
            menuModifier.Topmost = true;
            menuModifier.ShowInTaskbar = false;
            menuModifier.ShowDialog(); // Show SaleModePop window as modal
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
                            Style = (System.Windows.Style)FindResource("ButtonStyle")
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
                try
                {
                    dynamic selectedItem = listView.SelectedItem;

                    if (selectedItem != null)
                    {
                        // Extract product details from the selected item
                        int productId = selectedItem.ProductId;
                        string productName = selectedItem.ProductName;
                        decimal productPrice = Convert.ToDecimal(selectedItem.ProductPrice);

                        int quantity = selectedItem.Quantity;

                        // Create a new instance of Product with extracted details
                        Product selectedProduct = new Product(productId, productName, productPrice)
                        {
                            Quantity = quantity // Set the quantity based on the selected item
                        };
                        // Search for the product in cartProducts by ProductId
                        Product foundProduct = modelProcessing.cartProducts.FirstOrDefault(p => p.ProductId == productId);

                        if (foundProduct != null)
                        {
                            // Output the ChildItems of the found product
                            if (foundProduct.ChildItems != null && foundProduct.ChildItems.Any())
                            {
                                foreach (var childItem in foundProduct.ChildItems)
                                {
                                    childItemsSelected.Add(childItem);
                                }
                            }
                            else
                            {
                                Console.WriteLine("No Child Items");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Product with ID {productId} not found in cartProducts");
                        }

                        ModifierButton.Background = (Brush)Application.Current.FindResource("PrimaryButtonColor");
                        ModifierButton.IsEnabled = true;

                        // Open the function or command to retrieve child items (modifiers)
                        ModifierMenuView(productId);

                        // Add retrieved child items to the selected product's ChildItems collection
                        if (childItemsSelected != null && childItemsSelected.Any())
                        {
                            foreach (var item in childItemsSelected)
                            {
                                selectedProduct.ChildItems.Add(item);
                            }
                        }

                        // Update the existing product in cartProducts or add it if not found
                        bool productFound = false;
                        foreach (var product in modelProcessing.cartProducts)
                        {
                            if (product.ProductId == selectedProduct.ProductId)
                            {
                                productFound = true;
                                // Update the existing product with the updated quantity and child items
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

                        // Output cartProducts to the console for debugging (optional)
                        Console.WriteLine("Cart Products:");
                        foreach (var product in modelProcessing.cartProducts)
                        {
                            Console.WriteLine($"Product ID: {product.ProductId}");
                            Console.WriteLine($"Product Name: {product.ProductName}");
                            Console.WriteLine($"Product Price: {product.ProductPrice:C}");
                            Console.WriteLine($"Quantity: {product.Quantity}");

                            Console.WriteLine("Child Items Add:");
                            foreach (var childItem in product.ChildItems)
                            {
                                Console.WriteLine($"- {childItem.Name} ({childItem.Quantity} x {childItem.Price:C})");
                            }
                            Console.WriteLine(); // Blank line for separation
                        }

                        // Refresh the UI to reflect changes (if needed)
                        modelProcessing.UpdateCartUI();
                    }
                    else
                    {
                        ModifierButton.Background = Brushes.SlateGray;
                    }
                    childItemsSelected.Clear();
                }
                catch (RuntimeBinderException ex)
                {
                    Console.WriteLine($"Error accessing properties: {ex.Message}");
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
            AddButtonGridToPaymentMethod();
            Log.ForContext("LogType", "TransactionLog").Information($"Cart for Order ID: {OrderID} Payment Process, Cash");
        }

        private void PayButton_Click(object sender, RoutedEventArgs e)
        {
            MainContentProduct.Children.Clear();
            PayementProcess.Visibility = Visibility.Visible;
            MainContentProduct.Visibility = Visibility.Hidden;
            CalculatorShowed.Visibility = Visibility.Visible;
            modelProcessing.Calculating();
            AddButtonGridToPaymentMethod();
            Log.ForContext("LogType", "TransactionLog").Information($"Cart for Order ID: {OrderID} Payment Process, Selecting.....");
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
            Log.ForContext("LogType", "ApplicationLog").Information($"Pay Type Button Clicked");
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
                    modelProcessing.isPaymentChange = payTypeID.ToString();
                    secondaryMonitor.Payment.Text = displayName;

                    if (payTypeID == 1)
                    {
                        Button simulateButton = new Button();  // Create a new button instance (this could be any UI element)
                        RoutedEventArgs args = new RoutedEventArgs();  // Create new instance of RoutedEventArgs
                        payCashButton(simulateButton, args);
                    }
                    else if (payTypeID == 10011)
                    {
                        // Call QRIS method
                        modelProcessing.qrisProcess(payTypeID.ToString(), displayName);
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
            Log.ForContext("LogType", "ApplicationLog").Information($"Hold Button Clicked");
            // Create a deep copy of the current cart products
            List<Product> currentCartProducts = new List<Product>(modelProcessing.cartProducts);

            if (heldCarts.ContainsKey(CustomerTime))
            {
                // Retrieve the existing HeldCart for the given CustomerTime
                HeldCart existingHeldCart = heldCarts[CustomerTime];

                existingHeldCart.SalesModeName = salesModeText.Text;
                // Update the existing HeldCart with the new products
                existingHeldCart.CartProducts = currentCartProducts;

                // Notify the user that the cart has been updated
                Log.ForContext("LogType", "TransactionLog").Information($"Cart for Order ID: {existingHeldCart.CustomerId} at {existingHeldCart.TimeStamp} has been updated.");
            }
            else
            {
                // Create a new HeldCart instance
                HeldCart heldCart = new HeldCart(OrderID, CustomerTime, currentCartProducts, SaleMode, salesModeText.Text);

                // Add the held cart to the dictionary with the timestamp as the key
                heldCarts.Add(CustomerTime, heldCart);

                // Notify the user that the cart has been held
                Log.ForContext("LogType", "TransactionLog").Information($"Cart has been held for Order ID: {OrderID} at {CustomerTime}");
            }

            // Reset the UI
            heldCartManager.SaveHeldCarts(heldCarts);
            modelProcessing.ResetUI();
            DisplayHeldCartsInConsole();
        }

        private void DisplayHeldCarts()
        {
            Console.WriteLine("Held Carts:");
            foreach (var kvp in heldCarts)
            {
                Console.WriteLine($"Timestamp: {kvp.Key}");
                Console.WriteLine($"Order ID: {kvp.Value.CustomerId}");
                Console.WriteLine("Cart Products:");
                foreach (var product in kvp.Value.CartProducts)
                {
                    Console.WriteLine($"- {product.ProductName}");
                }
                Console.WriteLine(); // Add a blank line between each held cart entry
            }
        }

        public void HoldBill(List<Product> cartProducts)
        {
            modelProcessing.cartProducts = cartProducts;
            modelProcessing.UpdateCartUI();
        }
         
        public void UpdateSecondayMonitor()
        {
            if (secondaryMonitor != null)
            {
                secondaryMonitor.UpdateCartUI();
            }
        }

        public void QrisDone(string transactionStatus, string PayTypeID, string PayTypeName)
        {
            if (secondaryMonitor != null)
            {
                switch (transactionStatus)
                {
                    case "Settlement":

                        secondaryMonitor.imageLoader.Source = null;
                        secondaryMonitor.imageLoader.Height = 0;
                        secondaryMonitor.imageLoader.Width = 0;
                        TransactionDone(PayTypeID, PayTypeName);
                        break;

                    case "Cancel":
                        secondaryMonitor.imageLoader.Source = null;
                        secondaryMonitor.imageLoader.Height = 0;
                        secondaryMonitor.imageLoader.Width = 0;
                        break;

                    case "Expire":
                        // Actions to perform when QRIS transaction has expired

                        // Add other actions as needed
                        break;

                    default:
                        // Handle unknown status or other statuses if needed
                        MessageBox.Show("Unknown Transaction Status!");
                        break;
                }
            }            
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
            // Iterate through each product in cartProducts and set its Status to false
            modelProcessing.cartProducts.ForEach(product => product.Status = false);

            // Update the cart UI to reflect the changes
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
                        Log.ForContext("LogType", "TransactionLog").Information($"Cart for Order ID: {OrderID}. Insufficient funds. Please enter more money to proceed.");
                        MessageBox.Show("Insufficient funds. Please enter more money to proceed.");
                        return; // Exit the method without further processing
                    }
                    else
                    {
                        // Proceed with the rest of the processing
                        Log.ForContext("LogType", "TransactionLog").Information($"Cart for Order ID: {OrderID} Cash transaction Successfully return value is {returnAmount}");
                        TransactionDone("0", "Cash");
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

        public void Print(string PayTypeID, string PayTypeName, string ReceiptNumber)
        {
            // Get the base directory of the application
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

            // Create a new directory named "ReceiptLogs" within the base directory
            string receiptLogsDirectory = Path.Combine(baseDirectory, "ReceiptLogs");
            if (!Directory.Exists(receiptLogsDirectory))
            {
                Directory.CreateDirectory(receiptLogsDirectory);
            }

            string filePath = Path.Combine(receiptLogsDirectory, $"{ReceiptNumber}.pdf");

            // Create a new PrintDocument
            var doc = new PrintDocument();
            PrintController printController = new StandardPrintController();

            // Attach the event handler for printing content
            doc.PrintPage += (sender, e) => ProvideContent(sender, e, PayTypeID, PayTypeName, ReceiptNumber);

            // Calculate the required height based on the content
            int requiredHeight = CalculateRequiredHeight(); // Implement this method to calculate required height

            // Set the paper size to 80mm x requiredHeight
            var paperSize = new PaperSize("Receipt", 315, requiredHeight);
            doc.DefaultPageSettings.PaperSize = paperSize;

            // Set margins
            doc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 5); // Adjust margins as needed

            // Set the printer name (optional)
            doc.PrinterSettings.PrinterName = "Microsoft Print to PDF";

            // Set the file name and save location
            doc.PrinterSettings.PrintToFile = true;
            doc.PrinterSettings.PrintFileName = filePath;

            doc.PrintController = printController;
            // Print the document
            doc.Print();
            transactionStatus = new TransactionStatus(true, modelProcessing);
            transactionStatus.Show();
        }

        private int CalculateRequiredHeight()
        {
            // Calculate the number of lines
            int numberOfLines = GetNumberOfLines();
            int lineHeight = 12; // Assuming each line of text is 12 pixels high
            int margin = 40; // Add some margin for the top and bottom of the receipt

            // Calculate the required height
            return numberOfLines * lineHeight + margin;
        }

        private int GetNumberOfLines()
        {
            // Sample method to calculate the number of lines in the content
            // This will depend on how you are generating your content

            // For demonstration, let's assume each item and its details take up 2 lines
            int numberOfLines = 0;
            foreach (var item in modelProcessing.cartProducts)
            {
                numberOfLines += 2; // Item name and details
                if (item.HasChildItems())
                {
                    foreach (var childItem in item.ChildItems)
                    {
                        numberOfLines += 1; // Child item details
                    }
                }
            }

            // Add extra lines for headers, footers, and any additional information
            numberOfLines += 10; // Example value

            return numberOfLines;
        }

        public void ProvideContent(object sender, PrintPageEventArgs e, string PayTypeID, string PayTypeName, string ReceiptNumber)
        {
            string receiptNumber = ReceiptNumber;
            int colWidth = 52;
            // Parse the tendered amount
            decimal total = 0; // Initialize total variable
            var receiptItems = modelProcessing.cartProducts; // Use cartProducts instead of Order.orders

            const int FIRST_COL_WIDTH = 20;
            const int SECOND_COL_WIDTH = 8;
            const int THIRD_COL_WIDTH = 12;
            const int FOURTH_COL_WIDTH = 12;

            var sb = new StringBuilder();

            // Replace with your business name and details
            sb.AppendLine(new string('=', colWidth));
            sb.AppendLine("             AWESOME RECEIPT TESTING         ");
            sb.AppendLine(new string('=', colWidth));
            sb.AppendLine($"Receipt Number : {receiptNumber}");
            sb.AppendLine($"Date           : {DateTime.Now}");
            sb.AppendLine($"Cashier        : ");
            sb.AppendLine(new string('=', colWidth));
            sb.AppendLine("RK Grand Boulevard, Kabupaten Tangerang, Banten 15710");
            sb.AppendLine("Vat Reg. No. : Your VAT Reg No.");
            sb.AppendLine("TEL          : Your Phone Number");
            sb.AppendLine($"Payment Type : {PayTypeName}");
            sb.AppendLine(new string('=', colWidth));
            sb.AppendLine();
            sb.AppendLine($"{"ITEM".PadRight(FIRST_COL_WIDTH)}{"QTY".PadLeft(SECOND_COL_WIDTH)}{"PRICE".PadLeft(THIRD_COL_WIDTH)}{"TOTAL".PadLeft(FOURTH_COL_WIDTH)}");
            sb.AppendLine(new string('-', colWidth));

            foreach (var item in receiptItems)
            {
                if (!item.Status)
                {
                    continue;
                }
                string itemName = item.ProductName.Length > FIRST_COL_WIDTH ? item.ProductName.Substring(0, FIRST_COL_WIDTH - 1) : item.ProductName;

                sb.Append(itemName.PadRight(FIRST_COL_WIDTH));
                sb.Append(item.Quantity.ToString().PadLeft(SECOND_COL_WIDTH));
                sb.Append(item.ProductPrice.ToString("N0").PadLeft(THIRD_COL_WIDTH));
                sb.AppendLine((item.ProductPrice * item.Quantity).ToString("N0").PadLeft(FOURTH_COL_WIDTH));

                total += item.ProductPrice * item.Quantity; // Add item's amount to total

                // Check if the product has child items
                if (item.HasChildItems())
                {
                    foreach (var childItem in item.ChildItems)
                    {
                        string childItemName = childItem.Name.Length > FIRST_COL_WIDTH - 2 ? childItem.Name.Substring(0, FIRST_COL_WIDTH - 2) : childItem.Name;

                        sb.Append((" - " + childItemName).PadRight(FIRST_COL_WIDTH));
                        sb.Append(childItem.Quantity.ToString().PadLeft(SECOND_COL_WIDTH));
                        sb.Append(childItem.Price.ToString("N0").PadLeft(THIRD_COL_WIDTH));
                        sb.AppendLine((childItem.Price * childItem.Quantity).ToString("N0").PadLeft(FOURTH_COL_WIDTH));

                        total += childItem.Price * childItem.Quantity; // Add child item's amount to total
                    }
                }
            }
            decimal vatAmount = total * modelProcessing.productVATPercent / 100;
            decimal grandTotal = total + (total * modelProcessing.productVATPercent / 100);

            decimal tendered = 0;
            if (PayTypeID == "0")
            {
                if (!decimal.TryParse(displayText.Text, out decimal parsedTendered))
                {
                    // Handle parsing error, maybe show a message to the user
                    return;
                }
                tendered = Math.Round(parsedTendered);
            }
            else
            {
                tendered = grandTotal;
            }
            decimal change = tendered - grandTotal;
            sb.AppendLine(new string('-', colWidth));
            sb.AppendLine($"{"Sub Total:".PadLeft(FIRST_COL_WIDTH + SECOND_COL_WIDTH + THIRD_COL_WIDTH)}{total.ToString("N0").PadLeft(FOURTH_COL_WIDTH)}");
            sb.AppendLine($"{$"{modelProcessing.vatDesp}:".PadLeft(FIRST_COL_WIDTH + SECOND_COL_WIDTH + THIRD_COL_WIDTH)}{vatAmount.ToString("N0").PadLeft(FOURTH_COL_WIDTH)}");
            sb.AppendLine(new string('=', colWidth));
            sb.AppendLine($"{"Bill Total:".PadLeft(FIRST_COL_WIDTH + SECOND_COL_WIDTH + THIRD_COL_WIDTH)}{grandTotal.ToString("C0").PadLeft(FOURTH_COL_WIDTH)}");
            sb.AppendLine($"{"Cash:".PadLeft(FIRST_COL_WIDTH + SECOND_COL_WIDTH + THIRD_COL_WIDTH)}{tendered.ToString("C0").PadLeft(FOURTH_COL_WIDTH)}");
            sb.AppendLine($"{"Change:".PadLeft(FIRST_COL_WIDTH + SECOND_COL_WIDTH + THIRD_COL_WIDTH)}{change.ToString("C0").PadLeft(FOURTH_COL_WIDTH)}");
            sb.AppendLine(new string('=', colWidth));

            string printText = sb.ToString(); // Convert StringBuilder to string

            // Draw text on the Graphics object
            e.Graphics.DrawString(printText, new System.Drawing.Font(System.Drawing.FontFamily.GenericMonospace, 9, System.Drawing.FontStyle.Bold),
                                  new System.Drawing.SolidBrush(System.Drawing.Color.Black), 0, 0);
        }


        public static string GenerateReceiptNumber(string CustomerID)
        {
            string computerID = (Properties.Settings.Default._ComputerID).ToString();
            string kodeStore = "SOC";
            // Get the current date and time
            DateTime now = DateTime.Now;

            // Format the date as YYYYMMDD
            string datePart = now.ToString("yyyyMMdd");

            // Format the time as HHMMSS
            string timePart = now.ToString("HHmmss");

            // Concatenate all parts to form the receipt number
            string receiptNumber = $"{kodeStore}{computerID}{datePart}{timePart}{CustomerID}";

            return receiptNumber;
        }

        private void TransactionDone(string PayTypeID, string PayTypeName)
        {
            if (heldCarts.ContainsKey(CustomerTime))
            {
                heldCarts.Remove(CustomerTime);
                heldCartManager.SaveHeldCarts(heldCarts);
            }
            string ReceiptNumber = GenerateReceiptNumber(OrderID.ToString());

            Print(PayTypeID, PayTypeName, ReceiptNumber);
        }

        private void salesModeSee_Click(object sender, RoutedEventArgs e)
        {
            isNew = false;
            Log.ForContext("LogType", "TransactionLog").Information($"Order ID {OrderID} changed Sales Mode");
            SaleModeView();
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            ProductGroupLoad();
        }
        public void UpdateQRCodeImage(byte[] imageData, string transactionQrUrl)
        {
            // Display the QR code image
            BitmapImage bitmapImage = new BitmapImage();
            using (MemoryStream stream = new MemoryStream(imageData))
            {
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
            }


            secondaryMonitor.imageLoader.Width = 200;
            secondaryMonitor.imageLoader.Height = 200;
            secondaryMonitor.imageLoader.Stretch = Stretch.Fill;
            secondaryMonitor.imageLoader.Source = bitmapImage;

            // Optionally handle image click event to copy URL to clipboard
            secondaryMonitor.imageLoader.MouseLeftButtonDown += (sender, e) =>
            {
                Clipboard.SetText(transactionQrUrl);
            };
        }
    }
}
