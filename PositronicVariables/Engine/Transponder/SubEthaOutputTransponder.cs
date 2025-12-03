using PositronicVariables.Runtime;
using System;
using System.IO;

namespace PositronicVariables.Engine.Transponder
{
    public class SubEthaOutputTransponder(IPositronicRuntime runtime) : ISubEthaTransponder
    {
        private TextWriter _originalOut;
        private bool _redirected;
        internal static void FlushNow()
        {
            try
            {
                string bufferText = AethericRedirectionGrid.ImprobabilityDrive.ToString();
                if (string.IsNullOrEmpty(bufferText)) return;
                TextWriter target = AethericRedirectionGrid.ReferenceUniverse;
                target.Write(bufferText);
                target.Flush();
                Console.SetOut(target);
                AethericRedirectionGrid.SuppressEndOfUniverseReading = true;
            }
            catch { }
        }
        public void Redirect()
        {
            // If already redirected to the buffer, treat original as the real console to avoid self-append
            _originalOut = ReferenceEquals(Console.Out, AethericRedirectionGrid.ImprobabilityDrive)
                ? AethericRedirectionGrid.ReferenceUniverse
                : Console.Out;

            Console.SetOut(AethericRedirectionGrid.ImprobabilityDrive);
            runtime.Babelfish = AethericRedirectionGrid.ImprobabilityDrive;
            _redirected = true;
        }
        public void Restore()
        {
            if (!_redirected)
            {
                return;
            }
            string bufferText = AethericRedirectionGrid.ImprobabilityDrive.ToString();
            try
            {
                // Avoid self-append in all cases
                bool originalIsBuffer = ReferenceEquals(_originalOut, AethericRedirectionGrid.ImprobabilityDrive);

                if (!originalIsBuffer)
                {
                    if (!AethericRedirectionGrid.AtTheRestaurant)
                    {
                        _originalOut.Write(bufferText);
                        _originalOut.Flush();
                        AethericRedirectionGrid.SuppressEndOfUniverseReading = true;
                    }
                    else
                    {
                        AethericRedirectionGrid.SuppressEndOfUniverseReading = false;
                    }
                }
                else
                {
                    // If original target is buffer, do not write; let outer flush handle emission
                    AethericRedirectionGrid.SuppressEndOfUniverseReading = !AethericRedirectionGrid.AtTheRestaurant ? true : false;
                }

                Console.SetOut(_originalOut);
            }
            finally
            {
                _redirected = false;
            }
        }
    }
}
