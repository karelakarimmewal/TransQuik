using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using TranQuik.Model;

namespace TranQuik.Pages
{
    public partial class MenuModifier : Window
    {
        private MainWindow mainWindow; // Reference to MainWindow
        private ModelProcessing modelProcessing; // Reference to ModelProcessing
        private LocalDbConnector localDbConnector;
        private int buttonHeight;
        private int buttonWidth;
        private int columns;
        private int saleMode;
        private int ProductIDSelected;
        private List<ModifierGroup> modifierGroup;
        private List<ModifierMenu> modifierMenus;
        private bool isTouchEvent;



        public MenuModifier(MainWindow mainWindow, ModelProcessing modelProcessing, int productID)
        {
            this.mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
            this.modelProcessing = modelProcessing ?? throw new ArgumentNullException(nameof(modelProcessing));
            this.ProductIDSelected = productID;
            this.localDbConnector = new LocalDbConnector();
            columns = 5; // Assuming 5 columns
            buttonWidth = 150; // Adjust as needed based on the button size
            buttonHeight = 100; // Adjust as needed based on the button size
            saleMode = mainWindow.SaleMode;
            InitializeComponent();
            InitializeButtonHandlers();
            modifierGroup = GetModifierGroups(saleMode);
            CreateButtonsForModifierGroups(modifierGroup);
            // Retrieve the product details using the productID
            Product selectedProduct = modelProcessing.cartProducts.FirstOrDefault(p => p.ProductId == ProductIDSelected);

            if (selectedProduct != null)
            {
                quantityDisplay.Text = selectedProduct.Quantity.ToString();
            }
            else
            {
                // Handle the case where the product is not found
                quantityDisplay.Text = "1";
            }


            SizeToContent = SizeToContent.WidthAndHeight;
        }

        private void InitializeButtonHandlers()
        {
            addOnSave.Click += AddOnSave_Click;
            addOnSave.TouchDown += AddOnSave_TouchDown;

            addOnReset.Click += AddOnReset_Click;
            addOnReset.TouchDown += AddOnReset_TouchDown;
        }


        public List<ModifierGroup> GetModifierGroups(int saleMode)
        {
            List<ModifierGroup> modifierGroups = new List<ModifierGroup>();

            try
            {
                using (MySqlConnection connection = localDbConnector.GetMySqlConnection())
                {
                    connection.Open();

                    string sqlQuery = @"
                    SELECT DISTINCT PD.ProductDeptID, PD.ProductDeptCode, PD.ProductDeptName
                    FROM Products P
                    JOIN ProductPrice PP ON P.ProductID = PP.ProductID
                    JOIN ProductDept PD ON PD.ProductDeptID = P.ProductDeptID
                    JOIN ProductGroup PG ON P.ProductGroupID = PG.ProductGroupID
                    WHERE P.ProductActivate = 1 AND PG.ProductGroupID = 11 AND PP.SaleMode = @SaleMode";

                    using (MySqlCommand command = new MySqlCommand(sqlQuery, connection))
                    {
                        // Add parameter for SaleMode
                        command.Parameters.AddWithValue("@SaleMode", saleMode);

                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ModifierGroup modifierGroup = new ModifierGroup();
                                modifierGroup.ModifierGroupID = reader["ProductDeptID"].ToString();
                                modifierGroup.ModifierGroupCode = reader["ProductDeptCode"].ToString();
                                modifierGroup.ModifierName = reader["ProductDeptName"].ToString();
                                modifierGroups.Add(modifierGroup);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions here (e.g., log the error)
                Console.WriteLine("Error retrieving modifier groups: " + ex.Message);
            }

            return modifierGroups;
        }


        public void CreateButtonsForModifierGroups(List<ModifierGroup> modifierGroups)
        {
            GridModifierModeGroup.Children.Clear(); // Assuming GridSaleMode is defined in XAML

            // Create a UniformGrid to contain the buttons
            UniformGrid uniformGrid = new UniformGrid
            {
                Columns = columns, // Set the number of columns in the grid (adjust as needed)
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(1) // Add margin to the grid for spacing
            };

            // Add the UniformGrid to GridSaleMode (assuming GridSaleMode is defined in XAML)
            GridModifierModeGroup.Children.Add(uniformGrid);

            foreach (var modifierGroup in modifierGroups)
            {
                // Create a new Button
                Button button = new Button
                {
                    Height = buttonHeight,
                    Width = buttonWidth,
                    Style = (Style)Application.Current.Resources["ButtonStyle"], // Apply custom ButtonStyle defined in XAML
                    Effect = (DropShadowEffect)Application.Current.Resources["DropShadowEffect"], // Apply DropShadowEffect if desired
                    Background = (Brush)Application.Current.Resources["AccentColor"], // Set the button's background color to AccentColor
                    Margin = new Thickness(1) // Add margin to the button for spacing
                };

                // Create a StackPanel for button content
                StackPanel stackPanel = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                // Add TextBlock to StackPanel (ModifierGroup Name)
                TextBlock textBlock = new TextBlock
                {
                    Text = modifierGroup.ModifierName, // Assuming ProductDeptName is the name property of ModifierGroup
                    Margin = new Thickness(0),
                    FontSize = 14,
                    VerticalAlignment = VerticalAlignment.Center
                };
                stackPanel.Children.Add(textBlock);

                // Add StackPanel to Button Content
                button.Content = stackPanel;

                // Add Click Event Handler with ModifierGroupID and ModifierGroupName parameters
                button.Click += (sender, e) => Button_Click(sender, e, modifierGroup.ModifierGroupID, modifierGroup.ModifierName);
                button.TouchDown += (sender, e) => Button_TouchDown(sender, e, modifierGroup.ModifierGroupID, modifierGroup.ModifierName);

                // Add Button to UniformGrid
                uniformGrid.Children.Add(button);
            }
        }

        private async void ModifierGroup_Click(object sender, RoutedEventArgs e, string modifierGroupID, string modifierName)
        {
            // Retrieve modifier menus for the selected modifier group asynchronously
            modifierMenus = await Task.Run(() => GetModifierMenus(modifierGroupID));

            // Check if modifierMenus is not null and not empty
            if (modifierMenus?.Count > 0)
            {
                // Add buttons for the retrieved modifier menus
                AddButtonsToGrid(modifierMenus);
            }
        }

        private async Task<List<ModifierMenu>> GetModifierMenus(string modifierGroupID)
        {
            List<ModifierMenu> modifierMenus = new List<ModifierMenu>();

            try
            {
                using (MySqlConnection connection = localDbConnector.GetMySqlConnection())
                {
                    await connection.OpenAsync();

                    string sqlQuery = @"
                                    SELECT PG.`ProductGroupCode`, P.`ProductDeptID`, PD.`ProductDeptCode`, PD.`ProductDeptName`, 
                                           P.`ProductCode`, P.`ProductName`, P.`ProductName2`, PP.`ProductPrice`, PP.`SaleMode`
                                    FROM Products P
                                    JOIN ProductPrice PP ON P.`ProductID` = PP.`ProductID`
                                    JOIN ProductDept PD ON PD.`ProductDeptID` = P.`ProductDeptID`
                                    JOIN ProductGroup PG ON P.`ProductGroupID` = PG.`ProductGroupID`
                                    WHERE P.`ProductActivate` = 1 AND PG.`ProductGroupID` = 11 AND PP.`SaleMode` = @SaleMode AND PD.`ProductDeptID` = @ModifierGroupID
                                    ORDER BY P.`ProductName`";

                    using (MySqlCommand command = new MySqlCommand(sqlQuery, connection))
                    {
                        command.Parameters.AddWithValue("@SaleMode", saleMode); // Specify the sale mode value
                        command.Parameters.AddWithValue("@ModifierGroupID", modifierGroupID);
                        Console.WriteLine($"Ini adalah Sale Mode {saleMode}");
                        Console.WriteLine($"Ini adalah ModifierGroup {modifierGroupID}");

                        using (MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                ModifierMenu modifierMenu = new ModifierMenu();
                                modifierMenu.ModifierMenuCode = reader["ProductCode"].ToString();
                                modifierMenu.ModifierMenuName = reader["ProductName"].ToString();
                                modifierMenu.ModifierMenuPrice = Convert.ToDecimal(reader["ProductPrice"]);
                                modifierMenu.ModifierMenuQuantity++;
                                modifierMenus.Add(modifierMenu);
                            }
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error retrieving modifier menus: " + ex.Message);
            }

            return modifierMenus;
        }

        private void AddButtonsToGrid(List<ModifierMenu> modifierMenus)
        {
            // Clear existing children in GridModifierModeMenu
            GridModifierModeMenu.Children.Clear();

            // Create a new Grid to contain the buttons
            Grid buttonGrid = new Grid();

            // Define fixed rows and columns for the button grid
            for (int i = 0; i < 3; i++) // 3 rows
            {
                buttonGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            }

            for (int j = 0; j < 5; j++) // 5 columns
            {
                buttonGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            }

            // Display buttons for the modifierMenus
            DisplayModifierMenus(buttonGrid, modifierMenus);
            // Add the button grid to GridModifierModeMenu
            GridModifierModeMenu.Children.Add(buttonGrid);
        }

        private void DisplayModifierMenus(Grid buttonGrid, List<ModifierMenu> modifierMenus)
        {
            // Clear existing children in the button grid
            buttonGrid.Children.Clear();

            for (int index = 0; index < modifierMenus.Count; index++)
            {
                int row = index / 5;
                int col = index % 5;

                // Retrieve ModifierMenu data
                ModifierMenu modifierMenu = modifierMenus[index];

                // Create a button for each ModifierMenu
                Button button = new Button
                {
                    Content = modifierMenu.ModifierMenuName,
                    FontSize = 12,
                    Height = 100,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(2),
                    Padding = new Thickness(10),
                    Background = Brushes.Azure,
                    Foreground = Brushes.Black,
                    IsEnabled = true
                };

                // Attach click event handler to the button
                button.Click += (sender, e) => Button_Click(sender, e, modifierMenu);
                button.TouchDown += (sender, e) => Button_TouchDown(sender, e, modifierMenu);
                

                // Apply DropShadowEffect to the button
                button.Effect = new DropShadowEffect();

                // Add the button to the button grid
                Grid.SetRow(button, row);
                Grid.SetColumn(button, col);
                buttonGrid.Children.Add(button);
            }
        }
        private void ModifierMenuButton_Click(object sender, RoutedEventArgs e, ModifierMenu modifierMenu)
        {
            int currentQuantity = 0;
            int MaxQuantity = modelProcessing.cartProducts
                                .Where(product => product.ProductId == ProductIDSelected)
                                .Select(product => product.Quantity)
                                .DefaultIfEmpty(0) // Handle the case where no matching product is found
                                .Max();
            // Check if a ChildItem corresponding to the ModifierMenu already exists
            ChildItem existingItem = mainWindow.childItemsSelected.FirstOrDefault(item =>
                item.Name == modifierMenu.ModifierMenuName &&
                item.Price == modifierMenu.ModifierMenuPrice);

            if (existingItem != null)
            {
                existingItem.Quantity++;
                if (existingItem.Quantity > MaxQuantity)
                {
                    existingItem.Quantity = MaxQuantity;
                }
                currentQuantity = existingItem.Quantity;
            }
            else
            {
                ChildItem childItem = new ChildItem(
                    modifierMenu.ModifierMenuName,
                    modifierMenu.ModifierMenuPrice,
                    modifierMenu.ModifierMenuQuantity,
                    true // Assuming StatusBar is a property of ChildItem
                );
                currentQuantity = modifierMenu.ModifierMenuQuantity;

                // Add the ChildItem to the mainWindow's childItemsSelected collection
                mainWindow.childItemsSelected.Add(childItem);
            }

            // Update the visual state of the button to reflect selection
            UpdateButtonVisualState(sender as Button, true, currentQuantity);
        }

        private void UpdateButtonVisualState(Button button, bool isSelected, int quantity)
        {
            // Update button appearance based on selection state
            if (isSelected)
            {
                button.Background = Brushes.LightSkyBlue;
            }
            else
            {
                button.Background = Brushes.Azure;
            }

            // Get the existing button content
            string buttonText = button.Content.ToString();

            // Find the position of "x" in the button content
            int indexOfX = buttonText.LastIndexOf("x");

            if (indexOfX != -1)
            {
                // If "x" is found, remove the previous quantity and update with the new one
                buttonText = buttonText.Substring(0, indexOfX + 1) + "" + quantity;
            }
            else
            {
                // If "x" is not found, append the quantity
                buttonText += $" x{quantity}";
            }

            // Update the button content
            button.Content = buttonText;
        }



        private void addOnSave_Click(object sender, RoutedEventArgs e)
        {

            this.Close();
        }

        private void addOnReset_Click(object sender, RoutedEventArgs e)
        {
            // Find the product with the specified product ID
            Product product = modelProcessing.cartProducts.FirstOrDefault(p => p.ProductId == ProductIDSelected);

            // If the product is found, remove its associated child items from mainWindow.childItemsSelected
            if (product != null)
            {
                // Iterate over the child items and remove those associated with the product
                product.ChildItems.Clear();
                mainWindow.childItemsSelected.Clear();
            }

            modelProcessing.UpdateCartUI();

            // Close the MenuModifier window
            this.Close();
        }

        private void quantityDisplay_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (modelProcessing == null)
            {
                Console.WriteLine("Error: modelProcessing is null.");
                return;
            }

            // Retrieve the product details using the productID
            Product selectedProduct = modelProcessing.cartProducts.FirstOrDefault(p => p.ProductId == ProductIDSelected);

            if (selectedProduct != null)
            {
                if (int.TryParse(quantityDisplay.Text, out int newQuantity))
                {
                    if (newQuantity >= 1)
                    {
                        selectedProduct.Quantity = newQuantity;
                    }
                    else
                    {
                        // If the new quantity is less than 1, reset to the last valid quantity
                        quantityDisplay.Text = "1";
                        selectedProduct.Quantity = 1;
                    }
                }
                modelProcessing.UpdateCartUI();

            }
        }


        private void HandleNumberButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                string number = button.Content.ToString();

                // If the textbox is empty, replace the default value with the clicked number
                if (string.IsNullOrEmpty(quantityDisplay.Text))
                {
                    // Validate that the first number is not '0'
                    if (number != "0")
                    {
                        quantityDisplay.Text = number;
                        mainWindow.paxTotal = int.Parse(quantityDisplay.Text);
                    }
                }
                else
                {
                    // Ensure that the input can start with '0'
                    if (number != "0" || (!quantityDisplay.Text.StartsWith("0") && quantityDisplay.Text.Length == 1))
                    {
                        quantityDisplay.Text += number;
                        mainWindow.paxTotal = int.Parse(quantityDisplay.Text);
                    }
                }
            }
        }

        private void HandleBackspaceButtonClick(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(quantityDisplay.Text))
            {
                quantityDisplay.Text = quantityDisplay.Text.Substring(0, quantityDisplay.Text.Length - 1);
            }
        }

        private void HandleDeleteItemClick(object sender, RoutedEventArgs e)
        {
            if (modelProcessing == null)
            {
                Console.WriteLine("Error: modelProcessing is null.");
                return;
            }

            // Retrieve the product details using the productID
            Product selectedProduct = modelProcessing.cartProducts.FirstOrDefault(p => p.ProductId == ProductIDSelected);

            if (selectedProduct != null)
            {
                // Set the product status to False
                selectedProduct.Status = false;
                modelProcessing.UpdateCartUI();
                this.Close();
            }
            else
            {
                Console.WriteLine("Error: Product not found.");
            }
        }



        private void Button_TouchDown(object sender, TouchEventArgs e, string modifierGroupID, string modifierName)
        {
            isTouchEvent = true;
            ModifierGroup_Click(sender, new RoutedEventArgs(), modifierGroupID, modifierName);
        }

        private void Button_TouchDown(object sender, TouchEventArgs e, ModifierMenu modifierMenu)
        {
            isTouchEvent = true;
            ModifierMenuButton_Click(sender, new RoutedEventArgs(), modifierMenu);
        }

        private void Button_Click(object sender, RoutedEventArgs e, string modifierGroupID, string modifierName)
        {
            if (isTouchEvent)
            {
                isTouchEvent = false; // Reset the flag
                return;
            }

            ModifierGroup_Click(sender, e, modifierGroupID, modifierName);
        }

        private void Button_Click(object sender, RoutedEventArgs e, ModifierMenu modifierMenu)
        {
            if (isTouchEvent)
            {
                isTouchEvent = false; // Reset the flag
                return;
            }

            ModifierMenuButton_Click(sender, e, modifierMenu);
        }
        private void AddOnSave_TouchDown(object sender, TouchEventArgs e)
        {
            isTouchEvent = true;

            addOnSave_Click(sender, new RoutedEventArgs());
        }

        private void AddOnSave_Click(object sender, RoutedEventArgs e)
        {
            if (isTouchEvent)
            {
                isTouchEvent = false; // Reset the flag
                return;
            }
            addOnSave_Click(sender, new RoutedEventArgs());
        }

        private void AddOnReset_TouchDown(object sender, TouchEventArgs e)
        {
            isTouchEvent = true;
            addOnReset_Click(sender, new RoutedEventArgs());
        }

        private void AddOnReset_Click(object sender, RoutedEventArgs e)
        {
            if (isTouchEvent)
            {
                isTouchEvent = false; // Reset the flag
                return;
            }
            addOnReset_Click(sender, new RoutedEventArgs());
        }

        private void NumberButton_TouchDown(object sender, TouchEventArgs e)
        {
            isTouchEvent = true;
            HandleNumberButtonClick(sender, e);
        }

        private void NumberButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isTouchEvent)
            {
                HandleNumberButtonClick(sender,e);
            }
            isTouchEvent = false; // Reset the flag
        }
        private void BackspaceButton_TouchDown(object sender, TouchEventArgs e)
        {
            isTouchEvent = true;
            HandleBackspaceButtonClick(sender, e);
        }

        private void BackspaceButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isTouchEvent)
            {
                HandleBackspaceButtonClick(sender, e);
            }
            isTouchEvent = false; // Reset the flag
        }
        private void deleteItem_TouchDown(object sender, TouchEventArgs e)
        {
            isTouchEvent = true;
            HandleDeleteItemClick(sender, e);
        }

        private void deleteItem_Click(object sender, RoutedEventArgs e)
        {
            if (!isTouchEvent)
            {
                HandleDeleteItemClick(sender, e);
            }
            isTouchEvent = false; // Reset the flag
        }


    }
}
