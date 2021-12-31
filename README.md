
# tilegenerator
Convert an image to [Leaflet](https://leafletjs.com/) compatible map tiles

## Usage:
  tilegenerator [options]

## Options:

      --i, --input <input> (REQUIRED)  Input file
      --o, --output <output>           Output folder [default: tiles\yyyy_MM_dd_HHmmss]
      --f, --format <format>           Output file format (jpg, png) [default: jpg]
      --q, --quality <quality>         JPEG quality (1, 100) [default: 90]
      --z, --zoomlevels <zoomlevels>   Number of zoom levels [default: 5]
      --s, --tilesize <tilesize>       Tile size (min: 16, max: 2048) [default: 256]
      --leaflet                        Copy example leaflet file [default: False]
      --version                        Show version information
      -?, -h, --help                   Show help and usage information
      
## Known issues

- Windows only for now (uses very specific system.drawing calls)
- Maximum input size is limited to roughly 32,767 pixels
- Need to adjust the zoomlevel by hand for larger images
