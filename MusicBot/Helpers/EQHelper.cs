using System.Collections.Generic;
using Victoria.Payloads;

namespace MusicBot.Helpers
{
    class EQHelper
    {
        /// <summary>
        /// Builds EQ Bands for LavaPlayer.EqualizerAsync
        /// </summary>
        /// <param name="bands">gain for bands 0-14. If null, all bands will have 0.0 gain.</param>
        /// <returns>Array of EqualizerBands with specified gain.</returns>
        public static EqualizerBand[] BuildEQ(double[] bands = null)
        {
            List<EqualizerBand> eqBands = new List<EqualizerBand>();

            for (int i = 0; i < 15; i++)
            {
                double gain = (i < bands?.Length) ? bands[i] : 0.0;
                eqBands.Add(new EqualizerBand(i, gain));
            }

            return eqBands.ToArray();
        }
    }
}
