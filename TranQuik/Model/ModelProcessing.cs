﻿using MySql.Data.MySqlClient;
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

namespace TranQuik.Model
{
    public class ModelProcessing
    {
        private LocalDbConnector localDbConnector;
        private MainWindow mainWindow;
        private List<Product> cartProducts = new List<Product>();
        private int taxPercentage = 11; // Tax percentage
        private decimal discountPercentage = 0; // Discount percentage


        public ModelProcessing(MainWindow mainWindow)
        {
            this.localDbConnector = new LocalDbConnector(); // Instantiate LocalDbConnector
            this.mainWindow = mainWindow; // Assign the MainWindow instance
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
                        SELECT PD.ProductDeptID, PD.ProductDeptName
                        FROM ProductDept PD
                        WHERE PD.ProductDeptActivate = 1";

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

            // Create and configure the decrease button

            FrameworkElementFactory decreaseButtonFactory = new FrameworkElementFactory(typeof(Button));
            decreaseButtonFactory.SetValue(Button.ContentProperty, "-");
            decreaseButtonFactory.SetValue(Button.FontWeightProperty, FontWeights.Bold);
            decreaseButtonFactory.SetValue(Button.WidthProperty, 20.0);
            decreaseButtonFactory.SetValue(Button.MarginProperty, new Thickness(5));
            decreaseButtonFactory.SetBinding(Button.CommandParameterProperty, new Binding("ProductId"));
            decreaseButtonFactory.AddHandler(Button.ClickEvent, new RoutedEventHandler(DecreaseQuantity));
            decreaseButtonFactory.SetValue(Button.StyleProperty, Application.Current.FindResource("DecreaseButtonStyle"));

            // Create and configure the delete button
            FrameworkElementFactory deleteButtonFactory = new FrameworkElementFactory(typeof(Button));
            deleteButtonFactory.SetValue(Button.ContentProperty, "DEL");
            deleteButtonFactory.SetValue(Button.FontWeightProperty, FontWeights.Bold);
            deleteButtonFactory.SetValue(Button.WidthProperty, 20.0);
            deleteButtonFactory.SetValue(Button.MarginProperty, new Thickness(5));
            deleteButtonFactory.SetBinding(Button.CommandParameterProperty, new Binding("ProductId"));
            deleteButtonFactory.AddHandler(Button.ClickEvent, new RoutedEventHandler(DeleteFromCart));
            deleteButtonFactory.SetValue(Button.StyleProperty, Application.Current.FindResource("DeleteButtonStyle"));


            // Add buttons to the stack panel
            stackPanelFactory.AppendChild(decreaseButtonFactory);
            stackPanelFactory.AppendChild(deleteButtonFactory);

            // Set the stack panel as the visual tree of the DataTemplate
            actionCellTemplate.VisualTree = stackPanelFactory;

            // Add the Action column to the GridView
            gridView.Columns.Add(new GridViewColumn { Header = "Action", CellTemplate = actionCellTemplate, Width = 80, HeaderContainerStyle = headerColumnStyle });

            // Set the ListView's View to the created GridView
            mainWindow.cartGridListView.View = gridView;



            int index = 1;

            // Loop through each product in the cart and add corresponding UI elements
            foreach (Product product in cartProducts)
            {
                decimal totalProductPrice = product.ProductPrice * product.Quantity; // Total price for the product
                totalQuantity += product.Quantity;
                TextDecorationCollection textDecorations = new TextDecorationCollection();
                // Determine background and foreground colors based on product status
                Brush rowBackground = product.Status ? Brushes.Transparent : Brushes.Red;
                Brush rowForeground = product.Status ? Brushes.Black : Brushes.White; // Foreground color for canceled (false) products
                if (product.Status)
                {
                    totalPrice += totalProductPrice; // Only add to totalPrice if status is true
                }

                // Add item to ListView
                mainWindow.cartGridListView.Items.Add(new
                {
                    Index = index,
                    ProductName = product.ProductName,
                    ProductPrice = product.ProductPrice.ToString("#,0"), // Format ProductPrice without currency symbol
                    Quantity = product.Quantity,
                    TotalPrice = totalProductPrice.ToString("#,0"),
                    ChildItems = product.ChildItems,
                    ProductId = product.ProductId,
                    Background = rowBackground,
                    Foreground = rowForeground // Set foreground color based on product status
                });
                index++;
            }

            // Update displayed total prices
            // Define and apply the custom item container style for ListViewItems
            Style itemContainerStyle = new Style(typeof(ListViewItem));
            itemContainerStyle.Setters.Add(new Setter(ListViewItem.BackgroundProperty, new Binding("Background")));
            itemContainerStyle.Setters.Add(new Setter(ListViewItem.ForegroundProperty, new Binding("Foreground")));
            mainWindow.cartGridListView.ItemContainerStyle = itemContainerStyle;

            mainWindow.subTotal.Text = $"{totalPrice:C0}";
            mainWindow.total.Text = (totalPrice + (totalPrice * taxPercentage / 100)).ToString("#,0");
            mainWindow.GrandTextBlock.Text = $"{totalPrice + (totalPrice * taxPercentage / 100):C0}";
            mainWindow.totalQty.Text = totalQuantity.ToString("0.00");
            mainWindow.GrandTotalCalculator.Text = $"{(totalPrice + (totalPrice * taxPercentage / 100)).ToString("#,0")}";

            bool hasItemsInCart = cartProducts.Any();
            // Enable or disable the PayButton based on whether there are items in cartProducts
            mainWindow.PayButton.IsEnabled = hasItemsInCart;
            //secondaryWindow.CartProducts = cartProducts;
            //secondaryWindow.TaxPercentage = taxPercentage;
            //secondaryWindow.Discount = discountPercentage;
            //secondaryWindow.UpdateCartUI();

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

                        productToDecrease.Status = false;
                        productToDecrease.Quantity = 0;
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

                    // Debug: Print confirmation
                    Console.WriteLine($"Product with ID {productId} marked as canceled.");

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
                    Console.WriteLine($"ProductId: {product.ProductId}, ProductName: {product.ProductName}, Quantity: {product.Quantity}, Status: {product.Status}");
                }
            }
        }




    }
}
