using System;

namespace Umbraco.Core.Logging
{
    /// <summary>
    /// TODO: This is totally temporary for aspnetcore until we get a real profiler in there
    /// </summary>
    internal class NoopProfiler : IProfiler
    {
        public NoopProfiler()
        {
        }

        public string Render()
        {
            return string.Empty;
        }

        public IDisposable Step(string name)
        {
            return new Nothing();
        }
        
        public void Start()
        {
        }

        public void Stop(bool discardResults = false)
        {            
        }

        private class Nothing : DisposableObject
        {
            protected override void DisposeResources()
            {                
            }
        }
    }
}