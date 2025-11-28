using PositronicVariables.Runtime;
using System;
using System.IO;

namespace PositronicVariables.Engine.Transponder
{
    public class SubEthaOutputTransponder(IPositronicRuntime runtime) : ISubEthaTransponder
    {
        private TextWriter _originalOut;
        private bool _redirected;

        public void Redirect()
        {
            // Capture caller's current writer (tests may set StringWriter). Always do this once per logical run.
            _originalOut = Console.Out;
            Console.SetOut(AethericRedirectionGrid.ImprobabilityDrive);
            runtime.Babelfish = AethericRedirectionGrid.ImprobabilityDrive;
            _redirected = true;
        }
        public void Restore()
        {
            if (!_redirected)
            {
                return; // nothing to restore
            }
            string bufferText = AethericRedirectionGrid.ImprobabilityDrive.ToString();
            try
            {
                // Always write captured buffer to original test writer regardless of Restaurant flag;
                // tests depend on deterministic emission. Restaurant flag only toggles suppress mode.
                _originalOut.Write(bufferText);
                _originalOut.Flush();
                AethericRedirectionGrid.SuppressEndOfUniverseReading = !AethericRedirectionGrid.AtTheRestaurant;
            }
            finally
            {
                Console.SetOut(_originalOut);
                _redirected = false;
            }
        }
    }
}
