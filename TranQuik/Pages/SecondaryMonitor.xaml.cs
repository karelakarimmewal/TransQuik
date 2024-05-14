using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using TranQuik.Model;
using WpfScreenHelper;

namespace TranQuik.Pages
{
    public partial class SecondaryMonitor : Window
    {
        public decimal Discount { get; set; }
        public decimal TaxPercentage { get; set; }
        private ModelProcessing modelProcessing;
        public SecondaryMonitor(ModelProcessing modelProcessing)
        {
            InitializeComponent();
            Screen[] screens = Screen.AllScreens.ToArray();
            this.modelProcessing = modelProcessing;
            // Check if there are at least two screens available
            if (screens.Length > 1)
            {
                // Get the second screen (index 1)
                Screen secondScreen = screens[1]; // Index 1 for the second monitor

                // Get the resolution (width and height) of the second screen
                int secondScreenWidth = (int)secondScreen.Bounds.Width;
                int secondScreenHeight = (int)secondScreen.Bounds.Height;


                // Calculate the center position of the second screen
                int secondScreenCenterX = (int)(secondScreen.Bounds.Left + (secondScreenWidth / 2));
                int secondScreenCenterY = (int)(secondScreen.Bounds.Top + (secondScreenHeight / 2));

                this.Left = secondScreenCenterX - (this.Width / 2);
                this.Top = secondScreenCenterY - (this.Height / 2);


                string videoFileName = "AwAds.mp4";
                string videoDirectory = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Video");
                string videoUrl = System.IO.Path.Combine(videoDirectory, videoFileName);

                Uri videoUri = new Uri(videoUrl);

                // Set the source of the MediaElement
                MediaPlayer.Source = videoUri;

                // Set the loaded behavior to play the video automatically when loaded
                MediaPlayer.LoadedBehavior = MediaState.Play;

                // Handle the MediaEnded event to restart the video when it ends
                MediaPlayer.MediaEnded += (sender, e) =>
                {
                    // Rewind the video to the beginning
                    MediaPlayer.Position = TimeSpan.Zero;
                };

            }
            else
            {
                // If there are not enough screens, fallback to default behavior (e.g., center on primary screen)
                this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
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

            imageLoader.Width = 150;
            imageLoader.Height = 150;
            imageLoader.Stretch = Stretch.Fill;
            imageLoader.Source = bitmapImage;
        }
        public void UpdateCartUI()
        {
            // Clear existing items in the ListView
            bool hasItemsInCart = modelProcessing.cartProducts.Any();

            // Set the visibility of PaymentDetail based on the presence of items in CartProducts
            PaymentDetail.Visibility = hasItemsInCart ? Visibility.Visible : Visibility.Collapsed;

            cartPanelSecondary.Items.Clear();
            decimal totalPrice = 0;

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

            gridView.Columns.Add(new GridViewColumn { Header = "#", DisplayMemberBinding = new Binding("Index"), Width = 20, HeaderContainerStyle = headerColumnStyle, CellTemplate = indexColumnTemplate });
            gridView.Columns.Add(new GridViewColumn { Header = "Product", DisplayMemberBinding = new Binding("ProductName"), Width = 70, HeaderContainerStyle = headerColumnStyle });
            gridView.Columns.Add(new GridViewColumn { Header = "Price", DisplayMemberBinding = new Binding("ProductPrice"), Width = 80, HeaderContainerStyle = headerColumnStyle });
            gridView.Columns.Add(new GridViewColumn { Header = "Qty", DisplayMemberBinding = new Binding("Quantity"), Width = 25, HeaderContainerStyle = headerColumnStyle, CellTemplate = quantityColumnTemplate });
            gridView.Columns.Add(new GridViewColumn { Header = "Total", DisplayMemberBinding = new Binding("TotalPrice"), Width = 80, HeaderContainerStyle = headerColumnStyle });

            // Define the height of each row
            double rowHeight = 40; // Set the desired height for each row (in pixels)

            // Create a Style for ListViewItem
            var listViewItemStyle = new Style(typeof(ListViewItem));

            // Setters for ListViewItem properties
            listViewItemStyle.Setters.Add(new Setter(ListViewItem.HeightProperty, rowHeight)); // Set the height of each ListViewItem
            listViewItemStyle.Setters.Add(new Setter(ListViewItem.BorderBrushProperty, Brushes.LightGray)); // Set border brush
            listViewItemStyle.Setters.Add(new Setter(ListViewItem.BorderThicknessProperty, new Thickness(0, 0, 0, 1))); // Set border thickness (bottom only)
            listViewItemStyle.Setters.Add(new Setter(ListViewItem.HorizontalContentAlignmentProperty, HorizontalAlignment.Center)); // Center content horizontally

            // Apply the style to the ItemContainerStyle of ListView
            cartPanelSecondary.ItemContainerStyle = listViewItemStyle;

            // Create a DataTemplate for the Action column
            DataTemplate actionCellTemplate = new DataTemplate();
            FrameworkElementFactory stackPanelFactory = new FrameworkElementFactory(typeof(StackPanel));
            stackPanelFactory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

            // Set the stack panel as the visual tree of the DataTemplate
            actionCellTemplate.VisualTree = stackPanelFactory;

            // Set the ListView's View to the created GridView
            cartPanelSecondary.View = gridView;

            int index = 1;

            // Loop through each product in the cart and add corresponding UI elements
            foreach (Product product in modelProcessing.cartProducts)
            {
                decimal totalProductPrice = product.ProductPrice * product.Quantity; // Total price for the product
                TextDecorationCollection textDecorations = new TextDecorationCollection();
                // Determine background and foreground colors based on product status
                Brush rowBackground = product.Status ? Brushes.Transparent : Brushes.Red;
                Brush rowForeground = product.Status ? Brushes.Black : Brushes.White; // Foreground color for canceled (false) products
                if (product.Status)
                {
                    totalPrice += totalProductPrice; // Only add to totalPrice if status is true
                }
                else
                {
                    continue;
                }
                // Add item to ListView
                cartPanelSecondary.Items.Add(new
                {
                    Index = index,
                    ProductName = product.ProductName,
                    ProductPrice = product.ProductPrice.ToString("#,0"), // Format ProductPrice without currency symbol
                    Quantity = product.Quantity,
                    TotalPrice = totalProductPrice.ToString("#,0"),
                    ProductId = product.ProductId,
                });
                index++;
            }

            // Update total price text blocks
            priceTextBlock.Text = $"{totalPrice:C0}";
            taxTextBlock.Text = $"{totalPrice * TaxPercentage / 100:C0}";
            finalPriceTextBlock.Text = $"{totalPrice + totalPrice * TaxPercentage / 100:C0}";
        }

        public void ResetUI()
        {
            imageLoader.Source = null;
            Payment.Text = "Payment Type : None";
            UpdateCartUI();
        }
    }
}
