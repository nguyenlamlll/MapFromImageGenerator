using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapFromImage
{
    public class MapData
    {
        public MapData()
        {
            Data = new List<int>();
            Tileset = new TilesetInformation();
        }

        public int Width { get; set; }
        public int Height { get; set; }
        public int TileHeight { get; set; }
        public int TileWidth { get; set; }

        /// <summary>
        /// Data of the map. Each number in the list correspond to a tile in the tileset respectively.
        /// </summary>
        public List<int> Data { get; set; }

        public TilesetInformation Tileset { get; set; }
    }
}
