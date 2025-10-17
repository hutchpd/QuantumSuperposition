using PositronicVariables.Runtime;
using System;
using System.IO;

namespace PositronicVariables.Engine.Transponder
{
    public class SubEthaOutputTransponder(IPositronicRuntime runtime) : ISubEthaTransponder
    {
        private TextWriter _originalOut;

        public void Redirect()
        {
            // Like leaving a towel where you parked the time machine.
            _originalOut = ReferenceEquals(Console.Out, AethericRedirectionGrid.ImprobabilityDrive)
                ? AethericRedirectionGrid.ReferenceUniverse
                : Console.Out;
            // Hijack the console output like a space-time parasite.
            Console.SetOut(AethericRedirectionGrid.ImprobabilityDrive);
            runtime.Babelfish = AethericRedirectionGrid.ImprobabilityDrive;
        }
        public void Restore()
        {

            string bufferText = AethericRedirectionGrid.ImprobabilityDrive.ToString();
            if (!AethericRedirectionGrid.AtTheRestaurant)
            {
                _originalOut.Write(bufferText);
                _originalOut.Flush();

                // Prevent temporal echoes when we exit the time stream
                AethericRedirectionGrid.SuppressEndOfUniverseReading = true;
            }
            else
            {
                // Our breadrumb trail to the reference universe, else we'll end up like Quinn from Sliders.
                AethericRedirectionGrid.SuppressEndOfUniverseReading = false;
            }

            Console.SetOut(_originalOut);

        }
    }
}
