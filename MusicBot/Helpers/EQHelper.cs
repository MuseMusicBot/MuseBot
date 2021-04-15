using Victoria.Payloads;

namespace MusicBot.Helpers
{
    class EQHelper
    {
        public static object NoEQ()
        {
            var band1 = new EqualizerBand(band: 0, gain: 1);
            var band2 = new EqualizerBand(band: 1, gain: 1);
            var band3 = new EqualizerBand(band: 2, gain: 1);
            var band4 = new EqualizerBand(band: 3, gain: 1);
            var band5 = new EqualizerBand(band: 4, gain: -0.25);
            var band6 = new EqualizerBand(band: 5, gain: -0.25);
            var band7 = new EqualizerBand(band: 6, gain: -0.25);
            var band8 = new EqualizerBand(band: 7, gain: -0.25);
            var band9 = new EqualizerBand(band: 8, gain: -0.25);
            var band10 = new EqualizerBand(band: 9, gain: -0.25);
            var band11 = new EqualizerBand(band: 10, gain: -0.25);
            var band12 = new EqualizerBand(band: 11, gain: -0.25);
            var band13 = new EqualizerBand(band: 12, gain: -0.25);
            var band14 = new EqualizerBand(band: 13, gain: -0.25);
            var band15 = new EqualizerBand(band: 14, gain: -0.25);
            return (band1, band2, band3, band4, band5, band6, band7, band8, band9, band10, band11, band12, band13, band14, band15);
        }
    }
}
