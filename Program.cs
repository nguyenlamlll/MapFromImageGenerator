using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Newtonsoft.Json;
using System.Runtime.InteropServices;

namespace MapFromImage
{
    class Program
    {
        static readonly int mapCell = 16;

        static void Main(string[] args)
        {
            // Algorithm:
            // 1. Cut a big image into smaller ones.
            // 2. Create a tileset with unique tiles only and assign unique Ids to them as well
            // 3. Compare every tile cut from the orginial image with the tileset, replace them with IDs,
            //    and put the numbers to a data model.
            // =========================================================================================================
            // =========================================================================================================

            Bitmap originalImage = (Bitmap)Image.FromFile(args[0]);
            int originalImageWidth = originalImage.Width;
            int originalImageHeight = originalImage.Height;

            var totalColumns = originalImageWidth / mapCell;
            var totalRows = originalImageHeight / mapCell;
            Console.WriteLine("Total columns: " + totalColumns);
            Console.WriteLine("Total rows: " + totalRows);


            var allOriginalTiles = new Dictionary<int, Bitmap>();
            int id = 0;
            for (int row = 0; row < totalRows; row++)
            {
                for (int column = 0; column < totalColumns; column++)
                {
                    var tileToClone = new Rectangle(column * mapCell, row * mapCell, mapCell, mapCell);
                    allOriginalTiles.Add(id, originalImage.Clone(tileToClone, originalImage.PixelFormat));

                    Console.WriteLine("Added tile[" + column + "][" + row + "]");

                    id++;
                }
            }
            var emptyCell = (Bitmap)allOriginalTiles[0].Clone();

            MapData mapData = new MapData()
            {
                Tileset = new TilesetInformation()
                {
                    Column = totalColumns,
                    Row = totalRows
                },
                Height = originalImage.Height / mapCell,
                Width = originalImage.Width / mapCell,
                TileHeight = mapCell,
                TileWidth = mapCell
            };
            originalImage.Dispose();
            foreach (var item in allOriginalTiles)
            {
                if (AreTwoBitMapsTheSame(item.Value, emptyCell))
                {
                    mapData.Data.Add(0);
                }
                else
                {
                    mapData.Data.Add(item.Key);
                }
            }

            // Draw unique tiles into a tileset
            Directory.CreateDirectory("results");
            Console.WriteLine("\n---------------");
            Console.WriteLine("Merge separated tiles into one single tileset image...");

            //var tilesetImage = CombineBitmap(uniqueTiles.Values.ToList());
            //tilesetImage.Save("results\\tileset.png", ImageFormat.Png);
            //tilesetImage.Dispose();
            //foreach (var image in uniqueTiles.Values)
            //{
            //    image.Dispose();
            //}

            var mapDataJSON = JsonConvert.SerializeObject(mapData).ToLower();
            File.WriteAllText("results\\map.json", mapDataJSON);
        }

        [DllImport("msvcrt.dll")]
        private static extern int memcmp(IntPtr b1, IntPtr b2, long count);

        public static bool CompareMemCmp(Bitmap b1, Bitmap b2)
        {
            if ((b1 == null) != (b2 == null)) return false;
            if (b1.Size != b2.Size) return false;

            var bd1 = b1.LockBits(new Rectangle(new Point(0, 0), b1.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var bd2 = b2.LockBits(new Rectangle(new Point(0, 0), b2.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            try
            {
                IntPtr bd1scan0 = bd1.Scan0;
                IntPtr bd2scan0 = bd2.Scan0;

                int stride = bd1.Stride;
                int len = stride * b1.Height;

                return memcmp(bd1scan0, bd2scan0, len) == 0;
            }
            finally
            {
                b1.UnlockBits(bd1);
                b2.UnlockBits(bd2);
            }
        }

        static bool AreTwoBitMapsTheSame(Bitmap bmp1, Bitmap bmp2)
        {
            bool equals = true;
            Rectangle rect = new Rectangle(0, 0, bmp1.Width, bmp1.Height);
            BitmapData bmpData1 = bmp1.LockBits(rect, ImageLockMode.ReadOnly, bmp1.PixelFormat);
            BitmapData bmpData2 = bmp2.LockBits(rect, ImageLockMode.ReadOnly, bmp2.PixelFormat);
            unsafe
            {
                byte* ptr1 = (byte*)bmpData1.Scan0.ToPointer();
                byte* ptr2 = (byte*)bmpData2.Scan0.ToPointer();
                int width = rect.Width * 3; // for 24bpp pixel data
                for (int y = 0; equals && y < rect.Height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (*ptr1 != *ptr2)
                        {
                            equals = false;
                            break;
                        }
                        ptr1++;
                        ptr2++;
                    }
                    ptr1 += bmpData1.Stride - width;
                    ptr2 += bmpData2.Stride - width;
                }
            }
            bmp1.UnlockBits(bmpData1);
            bmp2.UnlockBits(bmpData2);

            return equals;
        }

        private static Bitmap MergeImages(IEnumerable<Bitmap> images)
        {
            var enumerable = images as IList<Bitmap> ?? images.ToList();

            var width = 0;
            var height = 0;

            foreach (var image in enumerable)
            {
                width += image.Width;
                height = image.Height > height
                    ? image.Height
                    : height;
            }

            var bitmap = new Bitmap(width, height);
            using (var g = Graphics.FromImage(bitmap))
            {
                var localWidth = 0;
                foreach (var image in enumerable)
                {
                    g.DrawImage(image, localWidth, 0);
                    localWidth += image.Width;
                }
            }
            return bitmap;
        }

        public static System.Drawing.Bitmap CombineBitmap(List<Bitmap> images)
        {
            //read all images into memory
            //List<System.Drawing.Bitmap> images = new List<System.Drawing.Bitmap>();
            System.Drawing.Bitmap finalImage = null;

            try
            {
                int width = 0;
                int height = 0;

                foreach (var image in images)
                {

                    //update the size of the final bitmap
                    width += image.Width;
                    height = image.Height > height ? image.Height : height;
                }

                //create a bitmap to hold the combined image
                finalImage = new System.Drawing.Bitmap(width, height);

                //get a graphics object from the image so we can draw on it
                using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(finalImage))
                {
                    //set background color
                    g.Clear(System.Drawing.Color.Transparent);

                    //go through each image and draw it on the final image
                    int offset = 0;
                    foreach (System.Drawing.Bitmap image in images)
                    {
                        g.DrawImage(image,
                          new System.Drawing.Rectangle(offset, 0, image.Width, image.Height));
                        offset += image.Width;
                    }
                }

                return finalImage;
            }
            catch (Exception)
            {
                if (finalImage != null)
                    finalImage.Dispose();
                //throw ex;
                throw;
            }
            finally
            {
                //clean up memory
                foreach (System.Drawing.Bitmap image in images)
                {
                    image.Dispose();
                }
            }
        }
    }
}
