using System;
using System.Text;
using Crestron.SimplSharp;                          	// For Basic SIMPL# Classes
using Crestron.SimplSharpPro;                       	// For Basic SIMPL#Pro classes
using Crestron.SimplSharpPro.CrestronThread;        	// For Threading
using Crestron.SimplSharpPro.Diagnostics;		    	// For System Monitor Access
using Crestron.SimplSharpPro.DeviceSupport;         	// For Generic Device Support
using Crestron.SimplSharpPro.EthernetCommunication;
using Crestron.SimplSharp.CrestronIO;


namespace CBL
{
    public class ControlSystem : CrestronControlSystem
    {
        EthernetIntersystemCommunications eisc;
        int numberOfZones = 250;
        bool[] activeZones;
        const string file = "Single.txt";

        /// <summary>
        /// ControlSystem Constructor. Starting point for the SIMPL#Pro program.
        /// Use the constructor to:
        /// * Initialize the maximum number of threads (max = 400)
        /// * Register devices
        /// * Register event handlers
        /// * Add Console Commands
        /// 
        /// Please be aware that the constructor needs to exit quickly; if it doesn't
        /// exit in time, the SIMPL#Pro program will exit.
        /// 
        /// You cannot send / receive data in the constructor
        /// </summary>
        public ControlSystem()
            : base()
        {
            try
            {
                Thread.MaxNumberOfUserThreads = 20;

                CrestronConsole.AddNewConsoleCommand(Read, "Read", "Output the file content.", ConsoleAccessLevelEnum.AccessAdministrator);

                CrestronEnvironment.ProgramStatusEventHandler += new ProgramStatusEventHandler(_ControllerProgramEventHandler);
                eisc = new EthernetIntersystemCommunications(0xE1, "127.0.0.2",this);
                activeZones = new bool[numberOfZones];

                #region EISC
                if (eisc.Register() == eDeviceRegistrationUnRegistrationResponse.Success)
                    eisc.SigChange += Eisc_SigChange;

                else
                    CrestronConsole.PrintLine("EISC not registered");

                #endregion


            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in the constructor: {0}", e.Message);
            }
        }


        private void Read(string args)
        {
            string filePath = "/nvram/Areas";
            string fullpath = Path.Combine(filePath, args);
            string line;

            try
            {
                if (File.Exists(fullpath))
                {

                    using (var sr = new StreamReader(fullpath))
                    {
                        string areaname = sr.ReadLine();

                        for (uint i = 0; i < numberOfZones; i++)
                        {
                            if ((line = sr.ReadLine()) != null)
                            {
                                if (line == "1")
                                    activeZones[i] = true;
                                else
                                    activeZones[i] = false;
                            }
                        }
                       
                    }
                }
                else
                    CrestronConsole.PrintLine("file not found ");
            }
            catch 
            {
            }


        }

        private void Eisc_SigChange(BasicTriList currentDevice, SigEventArgs args)
        {


            switch (args.Sig.Type)
            {
                case eSigType.Bool:
                    

                    #region Save todo
                    
                    if (eisc.BooleanOutput[1].BoolValue == true)
                    {
                        for (uint i = 0; i < numberOfZones; i++)
                        {
                            activeZones[i]= eisc.BooleanOutput[3+i].BoolValue;
                        }
                    }
                    #endregion


                    #region Retrieve DONE


                    uint areaIndex = args.Sig.Number / 252;
                    uint areaNumber = areaIndex + 1;

                  
                    if (eisc.BooleanOutput[args.Sig.Number%252 + (areaIndex*252)].BoolValue == true)
                    {
                       
                        this.Read($"A{areaNumber}.txt");
                        for (uint i = 0; i < numberOfZones; i++)
                        {

                            eisc.BooleanInput[areaIndex*252+3 + i].BoolValue = activeZones[i];
                        }
                    }


                    #endregion 



                    break;
                case eSigType.UShort:
                    eisc.UShortInput[args.Sig.Number].UShortValue = args.Sig.UShortValue;
                    break;
                case eSigType.String:
                    break;
            }


        }

        /// <summary>
        /// InitializeSystem - this method gets called after the constructor 
        /// has finished. 
        /// 
        /// Use InitializeSystem to:
        /// * Start threads
        /// * Configure ports, such as serial and verisports
        /// * Start and initialize socket connections
        /// Send initial device configurations
        /// 
        /// Please be aware that InitializeSystem needs to exit quickly also; 
        /// if it doesn't exit in time, the SIMPL#Pro program will exit.
        /// </summary>
        public override void InitializeSystem()
        {
            try
            {

            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in InitializeSystem: {0}", e.Message);
            }
        }

        /// <summary>
        /// Event Handler for Programmatic events: Stop, Pause, Resume.
        /// Use this event to clean up when a program is stopping, pausing, and resuming.
        /// This event only applies to this SIMPL#Pro program, it doesn't receive events
        /// for other programs stopping
        /// </summary>
        /// <param name="programStatusEventType"></param>
        void _ControllerProgramEventHandler(eProgramStatusEventType programStatusEventType)
        {
            switch (programStatusEventType)
            {
                case (eProgramStatusEventType.Paused):
                    //The program has been paused.  Pause all user threads/timers as needed.
                    break;
                case (eProgramStatusEventType.Resumed):
                    //The program has been resumed. Resume all the user threads/timers as needed.
                    break;
                case (eProgramStatusEventType.Stopping):
                    //The program has been stopped.
                    //Close all threads. 
                    //Shutdown all Client/Servers in the system.
                    //General cleanup.
                    //Unsubscribe to all System Monitor events
                    break;
            }

        }


    }
}