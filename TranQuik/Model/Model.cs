using Material.Icons;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace TranQuik.Model
{
    public class Customer
    {
        private static int lastCustomerId = 0; // Static field to track the last used CustomerId
        private static DateTime lastCreationDate = DateTime.MinValue; // Static field to track the date of the last customer creation

        public int CustomerId { get; private set; } // CustomerId property
        public DateTime Time { get; private set; } // Time property

        // Constructor to initialize a new Customer instance
        public Customer(DateTime time)
        {
            LoadLastCustomerData(); // Load last customer data from file

            // Check if the current date is different from the date when the last customer was created
            if (lastCreationDate.Date != time.Date)
            {
                lastCustomerId = 0; // Reset lastCustomerId to 0 for a new day
                lastCreationDate = time.Date; // Update the last creation date
            }

            CustomerId = ++lastCustomerId; // Increment and assign the new CustomerId
            Time = time; // Set the Time property

            SaveLastCustomerData(); // Save last customer data to file
        }

        // Method to save last customer data to file
        private void SaveLastCustomerData()
        {
            var data = new { LastCustomerId = lastCustomerId, LastCreationDate = lastCreationDate };
            string jsonData = JsonConvert.SerializeObject(data);
            File.WriteAllText("last_customer_data.json", jsonData);
        }

        // Method to load last customer data from file
        private void LoadLastCustomerData()
        {
            if (File.Exists("last_customer_data.json"))
            {
                string jsonData = File.ReadAllText("last_customer_data.json");
                var data = JsonConvert.DeserializeObject<dynamic>(jsonData);
                lastCustomerId = data.LastCustomerId;
                lastCreationDate = data.LastCreationDate;
            }
        }
    }

    public class Product
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; }
        public decimal ProductPrice { get; set; }
        public int Quantity { get; set; } = 1; // Default quantity is 1
        public bool Status { get; set; } // Status property

        public List<ChildItem> ChildItems { get; set; } // List of child items for the product

        public bool HasChildItems()
        {
            return ChildItems != null && ChildItems.Count > 0;
        }

        public Product(int productId, string productName, decimal productPrice)
        {
            ProductId = productId;
            ProductName = productName;
            ProductPrice = productPrice;
            Status = true; // Default status is Active
            ChildItems = new List<ChildItem>(); // Initialize child items list
        }
    }

    public class ChildItem
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public bool Status { get; set; }
        public ChildItem(string name, decimal price, int quantity, bool status)
        {
            Name = name;
            Quantity = quantity;
            Price = price;
            Status = status;
        }
    }

    public class SaleMode
    {
        public int SaleModeID { get; set; }
        public string SaleModeName { get; set; }
        public string ReceiptHeaderText { get; set; }
        public string NotInPayTypeList { get; set; }
        public string PrefixText { get; set; }
        public string PrefixQueue { get; set; }
    }

    public class ModifierGroup
    {
        public string ModifierGroupID { get; set; }
        public string ModifierGroupCode { get; set; }
        public string ModifierName { get; set; }
    }

    public class ModifierMenu
    {
        public string ModifierMenuCode { get; set; }
        public string ModifierMenuName { get; set; }
        public decimal ModifierMenuPrice { get; set; }
        public int ModifierMenuQuantity { get; set; } = 0;
    }

    public class HeldCart
    {
        public int CustomerId { get; set; }
        public DateTime TimeStamp { get; set; }
        public List<Product> CartProducts { get; set; }
        public int SalesMode { get; set; } // New property for SalesMode
        public string SalesModeName { get; set; }

        public HeldCart(int customerId, DateTime timeStamp, List<Product> cartProducts, int salesMode, string salesModeName)
        {
            CustomerId = customerId;
            TimeStamp = timeStamp;
            CartProducts = cartProducts;
            SalesMode = salesMode; // Assign SalesMode
            SalesModeName = salesModeName;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Customer: {CustomerId}");
            sb.AppendLine($"TimeStamp: {TimeStamp}");
            sb.AppendLine($"SalesMode: {SalesMode}"); // Include SalesMode in the string representation
            sb.AppendLine($"SalesModeName: {SalesModeName}"); // Include SalesMode in the string representation
            sb.AppendLine("CartProduct:");
            foreach (var product in CartProducts)
            {
                sb.AppendLine($"{product.ProductName}");
            }
            return sb.ToString();
        }
    }


    public class SaleModeIconMapper
    {
        // Dictionary to store SaleModeID to MaterialIconKind and Brush mappings
        private Dictionary<int, (MaterialIconKind, Brush)> iconMappings = new Dictionary<int, (MaterialIconKind, Brush)>
    {
        { 2, (MaterialIconKind.Motorbike, (Brush)Application.Current.FindResource("AccentColor")) },             // Example color: Red
        { 9, (MaterialIconKind.Car, (Brush)Application.Current.FindResource("SuccessColor")) },                   // Example color: Blue
        { 1, (MaterialIconKind.Restaurant, (Brush)Application.Current.FindResource("PrimaryButtonColor")) },           // Example color: Green
        { 3, (MaterialIconKind.PackageDelivered, (Brush)Application.Current.FindResource("ErrorColor")) },    // Example color: Orange
        { 10, (MaterialIconKind.Shopping,(Brush)Application.Current.FindResource("ButtonEnabledColor1")) },           // Example color: Purple
        { 11, (MaterialIconKind.Shopping, (Brush)Application.Current.FindResource("ButtonEnabledColor1")) },           // Example color: Yellow
        { 12, (MaterialIconKind.Shopping,(Brush)Application.Current.FindResource("ButtonEnabledColor1")) },             // Example color: Cyan
        { 13, (MaterialIconKind.Shopping, (Brush)Application.Current.FindResource("ButtonEnabledColor1")) },           // Example color: Magenta
        { 14, (MaterialIconKind.Shopping, (Brush)Application.Current.FindResource("ButtonEnabledColor1")) }             // Example color: Brown
    };

        // Method to retrieve the MaterialIconKind for a given SaleModeID
        public MaterialIconKind GetIconForSaleMode(int saleModeID)
        {
            // Check if the SaleModeID exists in the iconMappings dictionary
            if (iconMappings.ContainsKey(saleModeID))
            {
                return iconMappings[saleModeID].Item1; // Return the MaterialIconKind
            }
            else
            {
                // Default to MaterialIconKind.Restaurant if SaleModeID is not found
                return MaterialIconKind.Restaurant;
            }
        }

        // Method to retrieve the Brush (color) for a given SaleModeID
        public Brush GetColorForSaleMode(int saleModeID)
        {
            // Check if the SaleModeID exists in the iconMappings dictionary
            if (iconMappings.ContainsKey(saleModeID))
            {
                return iconMappings[saleModeID].Item2; // Return the Brush (color)
            }
            else
            {
                // Default color (if SaleModeID is not found, return a fallback color)
                return Brushes.Orange; // Example fallback color: Black
            }
        }
    }

}
