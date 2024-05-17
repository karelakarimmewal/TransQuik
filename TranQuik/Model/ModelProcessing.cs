using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TranQuik.Pages;

namespace TranQuik.Model
{
    public class ModelProcessing
    {
        private LocalDbConnector localDbConnector;
        private MainWindow mainWindow;
        private  SecondaryMonitor secondaryMonitor;
        public List<Product> cartProducts = new List<Product>();
        public decimal productVATPercent;
        public string vatDesp;

        public ModelProcessing(MainWindow mainWindow)
        {
            this.localDbConnector = new LocalDbConnector(); // Instantiate LocalDbConnector
            this.mainWindow = mainWindow; // Assign the MainWindow instance
            this.secondaryMonitor = mainWindow.secondaryMonitor;
            GetProductVATInfo();
        }

        public void GetProductGroupNamesAndIds(out List<string> productGroupNames, out List<int> productGroupIds)
        {
            productGroupNames = new List<string>();
            productGroupIds = new List<int>();

            try
            {
                using (MySqlConnection connection = localDbConnector.GetMySqlConnection())
                {
                    string query = @"
                                SELECT DISTINCT PD.ProductDeptID, PD.ProductDeptName
                                FROM ProductDept PD
                                JOIN products P ON PD.ProductDeptID = P.ProductDeptID
                                WHERE PD.ProductDeptActivate = 1 AND P.ProductTypeID IN (0, 1, 2, 7) ";


                    MySqlCommand command = new MySqlCommand(query, connection);
                    connection.Open();
                    MySqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        string groupName = reader["ProductDeptName"].ToString();
                        int groupId = Convert.ToInt32(reader["ProductDeptID"]);

                        productGroupNames.Add(groupName);
                        productGroupIds.Add(groupId);
                    }
                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving product group data from the database.", ex);
            }
        }
        public void LoadProductDetails(int productGroupId)
        {
            string query = (productGroupId == -1)
                ? @"
                    SELECT P.`ProductDeptID`, PD.`ProductDeptName`, P.`ProductCode`, P.`ProductName`, P.`ProductName2`, PP.`ProductPrice`, PP.`SaleMode`
                    FROM Products P
                    JOIN ProductPrice PP ON P.`ProductID` = PP.`ProductID`
                    JOIN ProductDept PD ON PD.`ProductDeptID` = P.`ProductDeptID`
                    WHERE P.`ProductActivate` = 1 AND PP.`SaleMode` = @SaleModeID
                    ORDER BY P.`ProductName`;"
                            : @"
                    SELECT P.`ProductDeptID`, PD.`ProductDeptName`, P.`ProductCode`, P.`ProductName`, P.`ProductName2`, PP.`ProductPrice`, PP.`SaleMode`
                    FROM Products P
                    JOIN ProductPrice PP ON P.`ProductID` = PP.`ProductID`
                    JOIN ProductDept PD ON PD.`ProductDeptID` = P.`ProductDeptID`
                    WHERE P.`ProductActivate` = 1 AND PP.`SaleMode` = @SaleModeID AND PD.ProductDeptID = @ProductDeptID
                    ORDER BY P.`ProductName`;";

            using (MySqlConnection connection = localDbConnector.GetMySqlConnection())
            {
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@SaleModeID", mainWindow.SaleMode);

                if (productGroupId != -1)
                    command.Parameters.AddWithValue("@ProductDeptID", productGroupId);

                connection.Open();
                MySqlDataReader reader = command.ExecuteReader();

                mainWindow.MainContentProduct.Children.Clear(); // Clear existing product buttons

                while (reader.Read())
                {
                    string productName = reader["ProductName"].ToString();
                    int productId = Convert.ToInt32(reader["ProductCode"]);
                    decimal productPrice = Convert.ToDecimal(reader["ProductPrice"]);

                    // Create product instance
                    Product product = new Product(productId, productName, productPrice);

                    // Determine the image path
                    string imgFolderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    string imagePath = Path.Combine(imgFolderPath, "Image", $"{productName}.jpg");

                    // Create the product button
                    Button productButton = CreateProductButton(product, imagePath);

                    productButton.Click += ProductButton_Click;

                    // Add the product button to the wrap panel
                    mainWindow.MainContentProduct.Children.Add(productButton);
                }

                reader.Close();
            }
        }

        private Button CreateProductButton(Product product, string imagePath)
        {
            // Check if image creation is allowed based on application setting
            bool allowImage = bool.Parse(Properties.Settings.Default["_AppAllowImage"].ToString());

            // Create product button
            Button productButton = new Button
            {
                Height = 118,
                Width = 100, // Set fixed width
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0.8),
                Tag = product // Assign product instance to Tag property
            };

            // Create a StackPanel to hold the image and text
            StackPanel stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Add text to the stack panel
            TextBlock textBlock = new TextBlock
            {
                Text = product.ProductName,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 3)
            };
            stackPanel.Children.Add(textBlock);

            // Check if image creation is allowed
            if (allowImage && File.Exists(imagePath))
            {
                // Load the image
                BitmapImage image = new BitmapImage(new Uri(imagePath, UriKind.RelativeOrAbsolute));
                Image img = new Image
                {
                    Source = image,
                    Width = 100,
                    Height = 100,
                };
                stackPanel.Children.Insert(0, img); // Insert image at the beginning
            }

            // Set the content of the button to the stack panel
            productButton.Content = stackPanel;

            return productButton;
        }


        private void ProductButton_Click(object sender, RoutedEventArgs e)
        {
            // Handle button click event
            Button clickedButton = (Button)sender;
            Product product = (Product)clickedButton.Tag as Product;
            if (product != null)
            {
                Console.WriteLine($"Product added to cart: {product.ProductId}, {product.ProductName}, Price: {product.ProductPrice}");
                AddToCart(product);
            }
            // Implement logic for handling the click on the product button
        }

        private void AddToCart(Product product)
        {
            // Check if the product is already in the cart
            Product existingProduct = cartProducts.FirstOrDefault(p => p.ProductId == product.ProductId);
            if (existingProduct != null)
            {
                // Increase the quantity if the product is already in the cart
                product.Status = true;
                existingProduct.Quantity++;
            }
            else
            {
                // Add the product with quantity 1 if it's not already in the cart
                product.Status = true;
                product.Quantity = 1;
                cartProducts.Add(product);
            }

            // Update cart UI
            UpdateCartUI();
        }

        public void UpdateCartUI()
        {
            // Clear existing items in the ListView
            mainWindow.cartGridListView.Items.Clear();
            decimal totalPrice = 0;
            int totalQuantity = 0;

            // Create a GridView
            GridView gridView = new GridView();

            // Define a style for the header columns
            Style headerColumnStyle = new Style(typeof(GridViewColumnHeader));
            headerColumnStyle.Setters.Add(new Setter(GridViewColumnHeader.BackgroundProperty, Brushes.LightGray)); // Set background color
            headerColumnStyle.Setters.Add(new Setter(GridViewColumnHeader.ForegroundProperty, Brushes.Black)); // Set foreground color
            headerColumnStyle.Setters.Add(new Setter(GridViewColumnHeader.FontWeightProperty, FontWeights.Bold)); // Set font weight
            headerColumnStyle.Setters.Add(new Setter(GridViewColumnHeader.HorizontalContentAlignmentProperty, HorizontalAlignment.Center)); // Horizontally center the header content

            Style cellContentStyle = new Style(typeof(TextBlock));
            cellContentStyle.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center)); // Horizontally center the cell content
            cellContentStyle.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center)); // Horizontally center the cell content

            // Define column headers
            var indexColumnTemplate = new DataTemplate(typeof(TextBlock));
            var indexTextBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
            indexTextBlockFactory.SetBinding(TextBlock.TextProperty, new Binding("Index"));
            indexTextBlockFactory.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center); // Center the text in cells
            indexTextBlockFactory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center); // Center the text in cells
            indexColumnTemplate.VisualTree = indexTextBlockFactory;

            var quantityColumnTemplate = new DataTemplate(typeof(TextBlock));
            var quantityTextBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
            quantityTextBlockFactory.SetBinding(TextBlock.TextProperty, new Binding("Quantity"));
            quantityTextBlockFactory.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center); // Center the text in cells
            quantityTextBlockFactory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center); // Center the text in cells
            quantityColumnTemplate.VisualTree = quantityTextBlockFactory;

            gridView.Columns.Add(new GridViewColumn { Header = "#", DisplayMemberBinding = new Binding("Index"), Width = 25, HeaderContainerStyle = headerColumnStyle, CellTemplate = indexColumnTemplate });
            gridView.Columns.Add(new GridViewColumn { Header = "Product", DisplayMemberBinding = new Binding("ProductName"), Width = 70, HeaderContainerStyle = headerColumnStyle });
            gridView.Columns.Add(new GridViewColumn { Header = "Price", DisplayMemberBinding = new Binding("ProductPrice"), Width = 70, HeaderContainerStyle = headerColumnStyle });
            gridView.Columns.Add(new GridViewColumn { Header = "Qty", DisplayMemberBinding = new Binding("Quantity"), Width = 25, HeaderContainerStyle = headerColumnStyle, CellTemplate = quantityColumnTemplate });
            gridView.Columns.Add(new GridViewColumn { Header = "Total", DisplayMemberBinding = new Binding("TotalPrice"), Width = 68, HeaderContainerStyle = headerColumnStyle });

            // Add bottom border to each row
            var listViewItemStyle = new Style(typeof(ListViewItem));
            listViewItemStyle.Setters.Add(new Setter(ListViewItem.BorderBrushProperty, Brushes.LightGray));
            listViewItemStyle.Setters.Add(new Setter(ListViewItem.BorderThicknessProperty, new Thickness(0, 0, 0, 1)));

            // Apply the style to the ListView
            mainWindow.cartGridListView.ItemContainerStyle = listViewItemStyle;


            // Create a DataTemplate for the Action column
            DataTemplate actionCellTemplate = new DataTemplate();
            FrameworkElementFactory stackPanelFactory = new FrameworkElementFactory(typeof(StackPanel));
            stackPanelFactory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

            //Create and configure the decrease button
            FrameworkElementFactory decreaseButtonFactory = new FrameworkElementFactory(typeof(Button));
            decreaseButtonFactory.SetValue(Button.ContentProperty, "-");
            decreaseButtonFactory.SetValue(Button.FontSizeProperty, 12.0); // Set the font size to 12 points
                                                                           // Assuming you have access to the Product instance
            
            decreaseButtonFactory.SetValue(Button.FontWeightProperty, FontWeights.Bold);
            decreaseButtonFactory.SetValue(Button.WidthProperty, 50.0);
            decreaseButtonFactory.SetValue(Button.MarginProperty, new Thickness(5));
            decreaseButtonFactory.SetBinding(Button.CommandParameterProperty, new Binding("ProductId"));
            decreaseButtonFactory.AddHandler(Button.ClickEvent, new RoutedEventHandler(DecreaseQuantity));
            decreaseButtonFactory.SetValue(Button.StyleProperty, Application.Current.FindResource("DecreaseButtonStyle"));

            // Create and configure the delete button
            //FrameworkElementFactory deleteButtonFactory = new FrameworkElementFactory(typeof(Button));
            //deleteButtonFactory.SetValue(Button.ContentProperty, "DEL");
            //deleteButtonFactory.SetValue(Button.FontWeightProperty, FontWeights.Bold);
            //deleteButtonFactory.SetValue(Button.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            //deleteButtonFactory.SetValue(Button.WidthProperty, 75.0);
            //deleteButtonFactory.SetValue(Button.MarginProperty, new Thickness(5));
            //deleteButtonFactory.SetBinding(Button.CommandParameterProperty, new Binding("ProductId"));
            //deleteButtonFactory.AddHandler(Button.ClickEvent, new RoutedEventHandler(DeleteFromCart));
            //deleteButtonFactory.SetValue(Button.StyleProperty, Application.Current.FindResource("DeleteButtonStyle"));


            // Add buttons to the stack panel
            stackPanelFactory.AppendChild(decreaseButtonFactory);
            //stackPanelFactory.AppendChild(deleteButtonFactory);

            // Set the stack panel as the visual tree of the DataTemplate
            actionCellTemplate.VisualTree = stackPanelFactory;

            // Add the Action column to the GridView
            gridView.Columns.Add(new GridViewColumn { Header = "Action", CellTemplate = actionCellTemplate, Width = 75, HeaderContainerStyle = headerColumnStyle });

            // Set the ListView's View to the created GridView
            mainWindow.cartGridListView.View = gridView;

            int index = 1;

            // Loop through each product in the cart and add corresponding UI elements
            foreach (Product product in cartProducts)
            {
                decimal totalProductPrice = product.ProductPrice * product.Quantity; // Total price for the product
                totalQuantity += product.Quantity;

                // Determine background and foreground colors based on product status
                Brush rowBackground = product.Status ? Brushes.Transparent : Brushes.Red;
                Brush rowForeground = product.Status ? Brushes.Black : Brushes.White;

                if (product.Status)
                {
                    if (product.ChildItems != null && product.ChildItems.Any())
                    {
                        foreach (ChildItem childItem in product.ChildItems)
                        {
                            totalProductPrice += (childItem.Price * childItem.Quantity);
                        }
                    }
                    totalPrice += totalProductPrice; // Only add to totalPrice if status is true
                }


                // Add the main product to the ListView
                mainWindow.cartGridListView.Items.Add(new
                {
                    Index = index,
                    ProductName = product.ProductName,
                    ProductPrice = product.ProductPrice.ToString("#,0"), // Format ProductPrice without currency symbol
                    Quantity = product.Quantity,
                    TotalPrice = totalProductPrice.ToString("#,0"),
                    ProductId = product.ProductId,
                    Background = rowBackground,
                    Foreground = rowForeground
                });

                // Add child items if they exist
                if (product.ChildItems != null && product.ChildItems.Any())
                {
                    foreach (ChildItem childItem in product.ChildItems)
                    {
                        // Add each child item to the ListView
                        mainWindow.cartGridListView.Items.Add(new
                        {
                            Index = "-", // Indent child items with a dash for visual separation
                            ProductName = childItem.Name,
                            ProductPrice = childItem.Price.ToString("#,0"),
                            Quantity = childItem.Quantity,
                            Background = Brushes.LightGray, // Inherit parent's background color
                            Foreground = rowForeground // Inherit parent's foreground color
                        });
                    }
                }


                index++; // Increment the index for the next item
                
            }
            // Update displayed total prices
            // Define and apply the custom item container style for ListViewItems
            Style itemContainerStyle = new Style(typeof(ListViewItem));
            itemContainerStyle.Setters.Add(new Setter(ListViewItem.BackgroundProperty, new Binding("Background")));
            itemContainerStyle.Setters.Add(new Setter(ListViewItem.ForegroundProperty, new Binding("Foreground")));
            mainWindow.cartGridListView.ItemContainerStyle = itemContainerStyle;

            mainWindow.subTotal.Text = $"{totalPrice:C0}";
            mainWindow.total.Text = (totalPrice + (totalPrice * productVATPercent / 100)).ToString("#,0");
            mainWindow.VATModeText.Text = $"{(totalPrice * productVATPercent / 100).ToString("#,0")}";
            mainWindow.GrandTextBlock.Text = $"{totalPrice + (totalPrice * productVATPercent / 100):C0}";
            mainWindow.totalQty.Text = totalQuantity.ToString("0.00");
            mainWindow.GrandTotalCalculator.Text = $"{(totalPrice + (totalPrice * productVATPercent / 100)).ToString("#,0")}";
            

            bool hasItemsInCart = cartProducts.Any();
            // Enable or disable the PayButton based on whether there are items in cartProducts
            mainWindow.PayButton.IsEnabled = hasItemsInCart;
            mainWindow.HoldButton.IsEnabled = hasItemsInCart;
            mainWindow.ClearButton.IsEnabled = hasItemsInCart;
            mainWindow.UpdateSecondayMonitor();

            if (mainWindow.cartGridListView.Items.Count > 0)
            {
                // Scroll into the last item
                mainWindow.cartGridListView.ScrollIntoView(mainWindow.cartGridListView.Items[mainWindow.cartGridListView.Items.Count - 1]);
            }
        }

        private void DecreaseQuantity(object sender, RoutedEventArgs e)
        {
            // Retrieve the product ID from the command parameter
            if (e.Source is Button button && button.CommandParameter is int productId)
            {
                // Find the product in the cart
                Product productToDecrease = cartProducts.FirstOrDefault(p => p.ProductId == productId);
                if (productToDecrease != null)
                {
                    // Decrease the quantity
                    if (productToDecrease.Quantity > 1)
                    {
                        productToDecrease.Quantity--;
                    }
                    else
                    {
                        // If the quantity is already 1, you may choose to remove the product instead
                        productToDecrease.Quantity = 1;
                    }

                    // Update cart UI
                    UpdateCartUI();
                }
            }
        }

        private void DeleteFromCart(object sender, RoutedEventArgs e)
        {
            // Retrieve the product ID from the command parameter
            if (e.Source is Button button && button.CommandParameter is int productId)
            {
                // Find the product in the cart
                Product productToCancel = cartProducts.FirstOrDefault(p => p.ProductId == productId);
                if (productToCancel != null)
                {
                    // Set the status of the product to "Canceled"
                    productToCancel.Status = false;
                    productToCancel.Quantity = 0;

                    // Cancel child items associated with the product
                    if (productToCancel.ChildItems != null && productToCancel.ChildItems.Any())
                    {
                        foreach (ChildItem childItem in productToCancel.ChildItems)
                        {
                            childItem.Status = false;
                            childItem.Quantity = 0;
                        }
                    }

                    // Debug: Print confirmation
                    Console.WriteLine($"Product with ID {productId} and associated child items marked as canceled.");

                    // Update cart UI to reflect the status change
                    UpdateCartUI();
                }
                else
                {
                    // Debug: Print message if product not found
                    Console.WriteLine($"Product with ID {productId} not found in the cart.");
                }

                // Debug: Print details of cartProducts
                Console.WriteLine("Cart Products:");
                foreach (var product in cartProducts)
                {
                    Console.WriteLine($"ProductId: {product.ProductId}, ProductName: {product.ProductName}, Quantity: {product.Quantity}, Status: {(product.Status ? "Active" : "Canceled")}");
                    if (product.ChildItems != null && product.ChildItems.Any())
                    {
                        Console.WriteLine("Child Items:");
                        foreach (var childItem in product.ChildItems)
                        {
                            Console.WriteLine($"- {childItem.Name}, Price: {childItem.Price:C}, Quantity: {childItem.Quantity}, Status: {(childItem.Status ? "Active" : "Canceled")}");
                        }
                    }
                }
            }
        }

        public void UpdateVisibleButtons()
        {
            // Determine the endIndex based on startIndex and visibleButtonCount
            mainWindow.endIndex = Math.Min(mainWindow.startIndex + mainWindow.visibleButtonCounts, mainWindow.productGroupButtons.Count);

            for (int i = 0; i < mainWindow.productGroupButtons.Count; i++)
            {
                // Check if the current button index is within the visible range
                if (i >= mainWindow.startIndex && i < mainWindow.endIndex)
                {
                    // Show the button if it's within the visible range
                    mainWindow.productGroupButtons[i].Visibility = Visibility.Visible;
                }
                else
                {
                    // Hide the button if it's outside the visible range
                    mainWindow.productGroupButtons[i].Visibility = Visibility.Collapsed;
                }
            }
        }

        public void UpdateVisibleProductGroupButtons()
        {
            // Determine the endIndex based on productButtonStartIndex and productButtonCount
            int endIndex = Math.Min(mainWindow.productButtonStartIndex + mainWindow.productButtonCount, mainWindow.MainContentProduct.Children.Count);
            Console.WriteLine(mainWindow.MainContentProduct.Children.Count);
            for (int i = 0; i < mainWindow.MainContentProduct.Children.Count; i++)
            {
                // Check if the current button index is within the visible range
                if (i >= mainWindow.productButtonStartIndex && i < endIndex)
                {
                    // Show the button if it's within the visible range
                    mainWindow.MainContentProduct.Children[i].Visibility = Visibility.Visible;
                }
                else
                {
                    // Hide the button if it's outside the visible range
                    mainWindow.MainContentProduct.Children[i].Visibility = Visibility.Collapsed;
                }
            }
        }

        public void Calculating()
        {
            // Parse the text values into double (assuming they represent numbers)
            if (double.TryParse(mainWindow.displayText.Text, out double currentTextValue) &&
                double.TryParse(mainWindow.GrandTotalCalculator.Text, out double grandTotalValue))
            {
                // Perform the subtraction
                double returnAmount = currentTextValue - grandTotalValue;

                // Update the TotalReturnCalculator with the calculated value
                mainWindow.TotalReturnCalculator.Text = returnAmount.ToString("#,0");
            }
            else
            {
                // Handle parsing failure or invalid input (e.g., non-numeric text)
                MessageBox.Show("Invalid input. Please enter valid numeric values.");
            }
        }
        public void UpdateFormattedDisplay()
        {
            // Get the raw numerical value from the displayed text
            if (double.TryParse(mainWindow.displayText.Text.Replace(".", ""), out double number))
            {
                // Format the number with dots as thousands separators
                mainWindow.displayText.Text = number.ToString("#,##0");
            }
            else
            {
                // Invalid input handling (e.g., if parsing fails)
                mainWindow.displayText.Text = "0";
            }
        }

        public void GetProductVATInfo()
        {
            string query = "SELECT ProductVATPercent, VATDesp FROM ProductVAT WHERE ProductVATCode = 'V'";

            try
            {
                using (MySqlConnection connection = localDbConnector.GetMySqlConnection())
                {
                    MySqlCommand command = new MySqlCommand(query, connection);
                    connection.Open();

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            productVATPercent = reader.GetDecimal("ProductVATPercent");
                            vatDesp = reader.GetString("VATDesp");
                            
                        }
                        else
                        {
                            Console.WriteLine("No data found for ProductVATCode = 'V'.");
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"MySQL Error: {ex.Message}");
                Console.WriteLine($"Error Code: {ex.ErrorCode}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving ProductVAT information: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }

        public void ResetUI()
        {
            // Clear the display after processing the input
            mainWindow.isNew = true;
            mainWindow.displayText.Text = "0";
            cartProducts.Clear();
            UpdateCartUI();
            Calculating();
            mainWindow.PayementProcess.Visibility = Visibility.Collapsed;
            mainWindow.MainContentProduct.Visibility = Visibility.Hidden;
            mainWindow.SaleMode = 0;
            mainWindow.SaleModeView();
            //secondaryWindow.ResetUI();
        }


    }
}
