using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranQuik.Model
{
    public class ChildItem
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
        // Add additional properties as needed
    }

    public class Product
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal ProductPrice { get; set; }
        public int Quantity { get; set; } = 1; // Default quantity is 1
        public bool Status { get; set; } // Status property

        public List<string> ChildItems { get; set; } // List of child items for the product

        public Product(int productId, string productName, decimal productPrice)
        {
            ProductId = productId;
            ProductName = productName;
            ProductPrice = productPrice;
            Status = true; // Default status is Active
            ChildItems = new List<string>(); // Initialize child items list
        }
    }




    public class HeldCart
    {
        public int CustomerId { get; set; }
        public DateTime TimeStamp { get; set; }
        public List<Product> CartProducts { get; set; }

        public HeldCart(int customerId, DateTime timeStamp, List<Product> cartProducts)
        {
            CustomerId = customerId;
            TimeStamp = timeStamp;
            CartProducts = cartProducts;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Customer: {CustomerId}");
            sb.AppendLine($"TimeStamp: {TimeStamp}");
            sb.AppendLine("CartProduct:");
            foreach (var product in CartProducts)
            {
                sb.AppendLine($"{product.ProductName}");
            }
            return sb.ToString();
        }
    }
}
