namespace OmronPLCTemperatureReader.Models
{
    public class SerieUsb : Serie
    {
        public SerieUsb() : base() { }
        public SerieUsb(string name, double multiplier = 1) : base(name, multiplier) { }
    }
}
