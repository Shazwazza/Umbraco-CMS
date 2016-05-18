using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Umbraco.Core.Logging;

namespace Umbraco.Core
{
	/// <summary>
	/// Starts the timer and invokes a  callback upon disposal. Provides a simple way of timing an operation by wrapping it in a <code>using</code> (C#) statement.
	/// </summary>
	public class DisposableTimer : DisposableObject
	{
	    private readonly ILogger _logger;
	    private readonly LogType? _logType;
	    private readonly IProfiler _profiler;
	    private readonly Type _loggerType;
	    private readonly string _endMessage;
	    private readonly IDisposable _profilerStep;
	    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
		private readonly Action<long> _callback;

	    internal enum LogType
	    {
	        Debug, Info
	    }

        internal DisposableTimer(ILogger logger, LogType logType, IProfiler profiler, Type loggerType, string startMessage, string endMessage)
        {
            if (logger == null) throw new ArgumentNullException("logger");
            if (loggerType == null) throw new ArgumentNullException("loggerType");

            _logger = logger;
            _logType = logType;
            _profiler = profiler;
            _loggerType = loggerType;
            _endMessage = endMessage;
            
            switch (logType)
            {
                case LogType.Debug:
                    logger.Debug(loggerType, startMessage);
                    break;
                case LogType.Info:
                    logger.Info(loggerType, startMessage);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("logType");
            }
            
            if (profiler != null)
            {
                _profilerStep = profiler.Step(startMessage);
            }
        }

	    protected internal DisposableTimer(Action<long> callback)
	    {
	        if (callback == null) throw new ArgumentNullException("callback");
	        _callback = callback;
	    }

	    public Stopwatch Stopwatch
		{
			get { return _stopwatch; }
		}
        
		/// <summary>
		/// Handles the disposal of resources. Derived from abstract class <see cref="DisposableObject"/> which handles common required locking logic.
		/// </summary>
		protected override void DisposeResources()
		{
            if (_profiler != null)
            {
                _profiler.DisposeIfDisposable();
            }

		    if (_profilerStep != null)
		    {
                _profilerStep.Dispose();
            }

		    if (_logType.HasValue && _endMessage.IsNullOrWhiteSpace() == false && _loggerType != null && _logger != null)
		    {
                switch (_logType)
                {
                    case LogType.Debug:
                        _logger.Debug(_loggerType, () => _endMessage + " (took " + Stopwatch.ElapsedMilliseconds + "ms)");
                        break;
                    case LogType.Info:
                        _logger.Info(_loggerType, () => _endMessage + " (took " + Stopwatch.ElapsedMilliseconds + "ms)");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("logType");
                }
            }

		    if (_callback != null)
		    {
                _callback.Invoke(Stopwatch.ElapsedMilliseconds);
            }
            
		}

	}
}