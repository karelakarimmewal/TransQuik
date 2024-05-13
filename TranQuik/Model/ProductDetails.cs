using MySql.Data.MySqlClient;
using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.IO;

namespace TranQuik.Model
{
    public class ProductDetails 
    {
        private LocalDbConnector localDbConnector;
        private MainWindow mainWindow;

        public ProductDetails(LocalDbConnector localDbConnector, MainWindow mainWindow)
        {
            this.localDbConnector = localDbConnector;
            this.mainWindow = mainWindow;
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

            // Check if the image exists
            if (File.Exists(imagePath))
            {
                // Load the image
                BitmapImage image = new BitmapImage(new Uri(imagePath, UriKind.RelativeOrAbsolute));
                Image img = new Image
                {
                    Source = image,
                    Width = 100,
                    Height = 100,
                };
                stackPanel.Children.Add(img);
            }

            // Add text to the stack panel
            TextBlock textBlock = new TextBlock
            {
                Text = product.ProductName,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 3)
            };
            stackPanel.Children.Add(textBlock);

            // Set the content of the button to the stack panel
            productButton.Content = stackPanel;

            return productButton;
        }

        private void ProductButton_Click(object sender, RoutedEventArgs e)
        {
            // Handle button click event
            Button clickedButton = (Button)sender;
            Product product = (Product)clickedButton.Tag;

            // Implement logic for handling the click on the product button
        }
    }
}
