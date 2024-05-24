using Material.Icons;
using Mysqlx.Session;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace TranQuik.Model
{
    public class Customer
    {
        private static int lastCustomerId = 0;
        private static DateTime lastCreationDate = DateTime.MinValue;
        private static readonly string directoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        private static readonly string filePath = Path.Combine(directoryPath, "lcdatas.json");
        private static readonly byte[] key = Encoding.UTF8.GetBytes("0123456789abcdef0123456789abcdef"); // 32 bytes for AES-256
        private static readonly byte[] iv = Encoding.UTF8.GetBytes("abcdef9876543210"); // 16 bytes for AES

        public int CustomerId { get; private set; }
        public DateTime Time { get; private set; }

        public Customer(DateTime time)
        {
            LoadLastCustomerData();

            if (lastCreationDate.Date != time.Date)
            {
                lastCustomerId = 0;
                lastCreationDate = time.Date;
            }

            CustomerId = ++lastCustomerId;
            Time = time;

            SaveLastCustomerData();
        }

        private void SaveLastCustomerData()
        {
            var data = new { LastCustomerId = lastCustomerId, LastCreationDate = lastCreationDate };
            string jsonData = JsonConvert.SerializeObject(data);

            // Encrypt the data before writing to file
            byte[] encryptedData = EncryptStringToBytes_Aes(jsonData, key, iv);

            Directory.CreateDirectory(directoryPath);
            File.WriteAllBytes(filePath, encryptedData);
        }

        private void LoadLastCustomerData()
        {
            if (File.Exists(filePath))
            {
                byte[] encryptedData = File.ReadAllBytes(filePath);

                // Decrypt the data after reading from file
                string decryptedData = DecryptStringFromBytes_Aes(encryptedData, key, iv);

                var data = JsonConvert.DeserializeObject<dynamic>(decryptedData);
                lastCustomerId = data.LastCustomerId;
                lastCreationDate = data.LastCreationDate;
            }
        }

        private static byte[] EncryptStringToBytes_Aes(string plainText, byte[] key, byte[] iv)
        {
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException(nameof(plainText));
            if (key == null || key.Length <= 0)
                throw new ArgumentNullException(nameof(key));
            if (iv == null || iv.Length <= 0)
                throw new ArgumentNullException(nameof(iv));

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = iv;

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                        return msEncrypt.ToArray();
                    }
                }
            }
        }

        private static string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] key, byte[] iv)
        {
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException(nameof(cipherText));
            if (key == null || key.Length <= 0)
                throw new ArgumentNullException(nameof(key));
            if (iv == null || iv.Length <= 0)
                throw new ArgumentNullException(nameof(iv));

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = iv;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            return srDecrypt.ReadToEnd();
                        }
                    }
                }
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

    public class Cart
    {
        public static int lastCartId ; // Static field to track the last used CartID
        private static DateTime lastCreationDate; // Static field to track the date of the last cart creation
        private static readonly string directoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        private static readonly string cartFilePath = Path.Combine(directoryPath, "c.json"); // File to store cart data
        private static readonly string idFilePath = Path.Combine(directoryPath, "last_cart_id.txt"); // File to store lastCartId
        private static readonly byte[] key = Encoding.UTF8.GetBytes("0123456789abcdef0123456789abcdef"); // 32 bytes for AES-256
        private static readonly byte[] iv = Encoding.UTF8.GetBytes("abcdef9876543210"); // 16 bytes for AES
        private static bool encryptionEnabled = false; // Flag to enable/disable encryption

        public int CartID { get; set; }
        public int CustomerID { get; set; }
        public List<Product> Products { get; set; }
        public int SaleModes { get; set; }

        public Cart(int customerID, List<Product> products, int saleModes, bool isReset)
        {
            CustomerID = customerID;
            Products = products;
            SaleModes = saleModes;

            LoadDataFromFile(); // Load the lastCartId from file
            Console.WriteLine(lastCreationDate);
            // Incremental approach for the first cart of the day
            

            if (isReset)
            {
                // Reset the isReset flag
                isReset = false;

                // Increment the lastCartId to create a new CartID
                lastCartId++;
                // Create a new Cart instance with the new CartID
                CartID = lastCartId;
                SaveDataToFile(); // Save lastCartId to file
            }
            else
            {
                if (lastCreationDate.Date != DateTime.Today)
                {
                    lastCartId = 1;
                    lastCreationDate = DateTime.Today;
                }
                CartID = lastCartId;
                SaveLastCartData(); // Save last cart data to file
            }
        }

        // Method to load the lastCartId from file
        private static void LoadDataFromFile()
        {
            if (File.Exists(idFilePath))
            {
                string[] lines = File.ReadAllLines(idFilePath);
                if (lines.Length >= 2)
                {
                    if (int.TryParse(lines[0], out int id))
                    {
                        lastCartId = id;
                    }
                    if (DateTime.TryParse(lines[1], out DateTime date))
                    {
                        lastCreationDate = date;
                    }
                }
            }
        }

        // Method to save the lastCartId to file
        private static void SaveDataToFile()
        {
            try
            {
                Directory.CreateDirectory(directoryPath);
                string[] lines = { lastCartId.ToString(), DateTime.Now.ToString() };
                File.WriteAllLines(idFilePath, lines);
            }
            catch (Exception ex)
            {
                // Handle any exceptions that might occur during file write
                // For example:
                Console.WriteLine("Error saving data to file: " + ex.Message);
            }
        }

        private void SaveLastCartData()
        {
            var data = new
            {
                CartID,
                CustomerID,
                Products,
                SaleModes
            };

            string jsonData = JsonConvert.SerializeObject(data) + Environment.NewLine;

            if (encryptionEnabled)
            {
                // Encrypt the data before writing to file
                byte[] encryptedData = EncryptStringToBytes_Aes(jsonData, key, iv);

                Directory.CreateDirectory(directoryPath);

                // Use FileStream with FileMode.Append to append data to the file
                using (var fileStream = new FileStream(cartFilePath, FileMode.Append))
                {
                    fileStream.Write(encryptedData, 0, encryptedData.Length);
                }
            }
            else
            {
                // Save data without encryption
                File.AppendAllText(cartFilePath, jsonData);
            }
        }

        // Method to load last cart data from file
        private void LoadLastCartData()
        {
            if (File.Exists(cartFilePath))
            {
                byte[] encryptedData = File.ReadAllBytes(cartFilePath);

                if (encryptionEnabled)
                {
                    // Decrypt the data after reading from file
                    string decryptedData = DecryptStringFromBytes_Aes(encryptedData, key, iv);

                    var data = JsonConvert.DeserializeObject<dynamic>(decryptedData);
                    lastCartId = data.CartID;
                    lastCreationDate = DateTime.Today; // Assuming there's no date stored in the encrypted data
                }
                else
                {
                    // Read data without decryption
                    string jsonData = File.ReadAllText(cartFilePath);
                    var data = JsonConvert.DeserializeObject<dynamic>(jsonData);
                    lastCartId = data.CartID;
                    lastCreationDate = DateTime.Today; // Assuming there's no date stored in the unencrypted data
                }
            }
        }

        // Methods for encryption and decryption
        private static byte[] EncryptStringToBytes_Aes(string plainText, byte[] key, byte[] iv)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = iv;

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                        return msEncrypt.ToArray();
                    }
                }
            }
        }

        private static string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] key, byte[] iv)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = iv;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            return srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
        }

        public static void ToggleEncryption(bool enableEncryption)
        {
            encryptionEnabled = enableEncryption;
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
