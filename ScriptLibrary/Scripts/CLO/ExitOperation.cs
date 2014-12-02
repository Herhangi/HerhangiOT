using System;
using System.Runtime.InteropServices;
using System.Timers;
using HerhangiOT.ServerLibrary;

namespace HerhangiOT.ScriptLibrary.Scripts.CLO
{
    public class ExitOperation : CommandLineOperation
    {
        private bool _isInCountdown;
        private int _timeLeft;
        private Timer _timer;

        public override void Setup()
        {
            Command = "exit";
            _timer = new Timer();
            _timer.Interval = 1000;
            _timer.Elapsed += new ElapsedEventHandler(TimerCallback);
        }

        public override void Operation(string[] args)
        {
            if (_isInCountdown)
            {
                if (args.Length < 2)
                {
                    Logger.Log(LogLevels.Warning, "There is an exit countdown going on you must cancel it before new 'exit' operation!");
                    return; 
                }

                if (args[1].Equals("--cancel"))
                {
                    Logger.Log(LogLevels.Operation, "Exit countdown has been canceled!");

                    _timer.Enabled = false;
                    _isInCountdown = false;
                    return;
                }
                else
                {
                    Logger.Log(LogLevels.Warning, "There is an exit countdown going on you must cancel it before new 'exit' operation!");
                    return;
                }
            }
            _timeLeft = 0;

            if (args.Length > 1)
            {
                if (!int.TryParse(args[1], out _timeLeft))
                {
                    Logger.Log(LogLevels.Warning, "Operation 'exit' is called with invalid parameters!");
                    return;
                }
            }

            string response;
            do
            {
                Console.Write("Are you sure to exit " + ((_timeLeft == 0) ? "IMMEDIATELY" : ("in " + _timeLeft + " seconds")) + "(yes/no): ");
                response = (Console.ReadLine() ?? string.Empty).ToLowerInvariant();
            } while (!response.Equals("yes") && !response.Equals("no"));

            if (response.Equals("yes"))
            {
                if (_timeLeft == 0)
                    Environment.Exit(0);
                else
                {
                    Logger.Log(LogLevels.Operation, "Server will be closed in " + _timeLeft + " seconds! You can cancel countdown by typing 'exit --cancel'!");
                    
                    _isInCountdown = true;
                    _timer.Enabled = true;
                }
            }
        }

        void TimerCallback(object source, ElapsedEventArgs e)
        {
			_timeLeft--;
			
			if(_timeLeft <= 0)
			{
			    Logger.Log(LogLevels.Operation, "Exit timer is up, closing down server!");
			    _timer.Enabled = false;
                Environment.Exit(0);
			}
            else if (_timeLeft % 60 == 0)
            {
                // BROADCAST
                Logger.Log(LogLevels.Operation, (_timeLeft / 60) + " minute(s) left for closing server! Broadcasting to users!");
            }
        }
    }
}
