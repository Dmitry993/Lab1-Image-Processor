using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;

namespace ImageProcessor
{
    class ImageProcessing
    {
        public static void CommandList()
        {
            Console.WriteLine("Select action:");
            Console.WriteLine("1 - Renaming image according to the date of shooting;");
            Console.WriteLine("2 - Add to image mark when photo was made;");
            Console.WriteLine("3 - Sort images by year;");
            Console.WriteLine("4 - Sort images by location;");

            string type = Console.ReadLine();

            switch (type)
            {
                case "1":
                    RenameImage();
                    break;
                case "2":
                    AddDateToImage();
                    break;
                case "3":
                    SortByYear();
                    break;
                case "4":
                    SortByLocation();
                    break;
                default:
                    break;
            }
        }

        private static void RenameImage()
        {
            Console.WriteLine("Write path to the image folder:");

            var path = Console.ReadLine();
            var dirInfo = new DirectoryInfo(path);
            var folderName = dirInfo.Name;
            var newFolder = Directory.CreateDirectory(path + "..\\" + folderName + "_RenameImage\\");

            var files = dirInfo.GetFiles("*.jpg");

            foreach (var file in files)
            {
                Image image = new Bitmap(file.FullName);

                PropertyItem[] propItems = image.PropertyItems;

                System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
                string fileName = encoding.GetString(propItems[5].Value).Replace(':', '.').Substring(0, 16) + ".jpg";
                file.CopyTo(newFolder.FullName + fileName);
            }
        }

        private static void AddDateToImage()
        {
            Console.WriteLine("Write path to the image folder:");

            var path = Console.ReadLine();
            var dirInfo = new DirectoryInfo(path);
            var folderName = dirInfo.Name;
            var newDirectory = Directory.CreateDirectory(path + "..\\" + folderName + "_AddDate\\");

            var files = dirInfo.GetFiles("*.jpg");

            foreach (var file in files)
            {
                Image image = new Bitmap(file.FullName);

                var propItems = image.PropertyItems;

                var encoding = new ASCIIEncoding();
                var date = encoding.GetString(propItems[5].Value).Replace(':', '.').Substring(0, 16);

                var drawFont = new Font("Arial", 12);
                var drawBrush = new SolidBrush(Color.Yellow);

                var x = image.Width - 20.0F;
                var y = 20.0F;

                var drawFormat = new StringFormat();
                drawFormat.FormatFlags = StringFormatFlags.DirectionRightToLeft;

                var graphics = Graphics.FromImage(image);
                graphics.DrawString(date, drawFont, drawBrush, x, y, drawFormat);

                image.Save(newDirectory.FullName + file.Name);

            }
        }

        private static void SortByYear()
        {
            Console.WriteLine("Write path to the image folder:");

            var path = Console.ReadLine();
            var dirInfo = new DirectoryInfo(path);
            var folderName = dirInfo.Name;
            var newDirectory = Directory.CreateDirectory(path + "..\\" + folderName + "_SortByYear\\");
            var files = dirInfo.GetFiles("*.jpg");

            foreach (var file in files)
            {
                Image image = new Bitmap(file.FullName);

                var propItems = image.PropertyItems;

                var encoding = new ASCIIEncoding();
                var year = encoding.GetString(propItems[5].Value).Substring(0, 4);
                var folderByYear = Directory.CreateDirectory(newDirectory.FullName + year + "\\");
                
                file.CopyTo(folderByYear.FullName + file.Name);
            }
        }

        private static void SortByLocation()
        {
            Console.WriteLine("Write path to the image folder:");

            var path = Console.ReadLine();
            var dirInfo = new DirectoryInfo(path);
            var folderName = dirInfo.Name;
            var newDirectory = Directory.CreateDirectory(path + "..\\" + folderName + "_SortByLocation\\");
            var files = dirInfo.GetFiles("*.jpg");
            var url = "";

            foreach (var file in files)
            {
                Image image = new Bitmap(file.FullName);

                try
                {
                    url = CreateRequest(image);
                }
                catch (ArgumentException)
                {
                    continue;
                }

                WebRequest request = WebRequest.Create(url);
                WebResponse response = request.GetResponse();
                using (Stream stream = response.GetResponseStream())
                {
                    using (XmlReader reader = XmlReader.Create(stream))
                    {
                        while (reader.Read())
                        {
                            if (reader.Name == "description")
                            {
                                reader.Read();
                                var folderLocation =
                                    Directory.CreateDirectory(newDirectory.FullName + reader.Value + "\\");
                                file.CopyTo(folderLocation.FullName + file.Name);
                                break;
                            }
                        }
                    }
                }

                response.Close();
                Console.Read();

            }
        }

        private static string CreateRequest(Image image)
        {
            PropertyItem propItems1 = image.GetPropertyItem(2);
            PropertyItem propItems2 = image.GetPropertyItem(4);

            var lat = DecodeRational64U(propItems1);
            var deg = DecodeRational64U(propItems2);

            var basicUrl = "https://geocode-maps.yandex.ru/1.x/?geocode={0}";
            string url = string.Format(basicUrl,
                ToDecimleCoordDouble(deg.Degree, deg.Minute, deg.Second)
                    .ToString(CultureInfo.InvariantCulture) +
                "," + ToDecimleCoordDouble(lat.Degree, lat.Minute, lat.Second)
                    .ToString(CultureInfo.InvariantCulture));
            return url;
        }

        private static double ToDecimleCoordDouble(double degrees, double minutes, double seconds)
        {
            return degrees + (minutes / 60) + (seconds / 3600);
        }

        private static GeoCoords DecodeRational64U(System.Drawing.Imaging.PropertyItem propertyItem)
        {
            var dN = BitConverter.ToUInt32(propertyItem.Value, 0);
            var dD = BitConverter.ToUInt32(propertyItem.Value, 4);
            var mN = BitConverter.ToUInt32(propertyItem.Value, 8);
            var mD = BitConverter.ToUInt32(propertyItem.Value, 12);
            var sN = BitConverter.ToUInt32(propertyItem.Value, 16);
            var sD = BitConverter.ToUInt32(propertyItem.Value, 20);

            double deg;
            double min;
            double sec;

            if (dD > 0) { deg = (double)dN / dD; } else { deg = dN; }
            if (mD > 0) { min = (double)mN / mD; } else { min = mN; }
            if (sD > 0) { sec = (double)sN / sD; } else { sec = sN; }

            return new GeoCoords
            {
                Degree = deg,
                Minute = min,
                Second = sec
            };
        }
    }

    public class GeoCoords
    {
        public double Degree { get; set; }

        public double Minute { get; set; }

        public double Second { get; set; }
    }
}
