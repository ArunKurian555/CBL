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

        ThreeSeriesTcpIpEthernetIntersystemCommunications [] areas = new ThreeSeriesTcpIpEthernetIntersystemCommunications [5];
        uint numberOfZones = 250;
        bool[] activeZones = new bool[250];


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

                CrestronConsole.AddNewConsoleCommand(ZoneAreaRead, "Read", "Output the file content.", ConsoleAccessLevelEnum.AccessAdministrator);


            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in the constructor: {0}", e.Message);
            }
        }

        private void ZoneAreaWrite(string args,uint areanumber)
        {
            string filePath = "/nvram/Areas";
            string fullpath = Path.Combine(filePath, args);
            string message = $"Area{areanumber}\n";

            try
            {

                for (uint i = 0; i < numberOfZones; i++)
                {
                        if (activeZones[i] == true)
                            message = message + "1\n";
                        else
                            message = message + "0\n";
                }
                message = message + "ENDOFFILE";
                using (FileStream fs= File.Create(fullpath))
                {
                    fs.Write(message + Environment.NewLine, Encoding.Default);
                }


            }
            catch
            {

            }



        }

        private void ZoneAreaRead(string args)
        {
            string filePath = "/nvram/Areas";
            string fullpath = Path.Combine(filePath, args);
            string line;

            try
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
            catch 
            {
            }


        }

        private void Eisc_SigChange(BasicTriList currentDevice, SigEventArgs args)
        {


            switch (args.Sig.Type)
            {
                case eSigType.Bool:


                    #region IPID E1 to E4
                    for(uint k=0; k < 4; k++)
                    {

                        #region Zone Area
                        uint areaIndex = args.Sig.Number / (numberOfZones + 2);
                        uint areaNumber = areaIndex + 1;
                        uint trueAreaNumber = areaNumber+ k * 15 ;


                        if (areas[k].BooleanOutput[args.Sig.Number].BoolValue == true)
                        {


                            #region Save
                            if (args.Sig.Number % (numberOfZones + 2) == 1)
                            {
                                for (uint i = 0; i < numberOfZones; i++)
                                {

                                    activeZones[i] = areas[k].BooleanOutput[areaIndex * (numberOfZones + 2) + 3 + i].BoolValue;
                                }
                                this.ZoneAreaWrite($"A{trueAreaNumber}.txt", trueAreaNumber);

                            }
                            #endregion

                            #region Retrieve
                            if (args.Sig.Number % (numberOfZones + 2) == 2)
                            {
                                this.ZoneAreaRead($"A{trueAreaNumber}.txt");
                                for (uint i = 0; i < numberOfZones; i++)
                                {

                                    areas[k].BooleanInput[areaIndex * (numberOfZones + 2) + 3 + i].BoolValue = activeZones[i];
                                }
                            }
                            #endregion

                        }

                        #endregion


                        #region Number of Areas

                        #region Set and Save
                        ushort numberOfAreas;
                        string message = $"Number of Area\n";
                        string filePath = "/nvram/Areas/NosArea.txt";

                        if (areas[3].BooleanOutput[2000].BoolValue == true)
                        {
                           numberOfAreas=  Convert.ToUInt16(areas[3].StringOutput[1].StringValue);
                            areas[3].UShortInput[1].UShortValue = numberOfAreas;

                            for (uint i = 0; i < 50; i++)
                            {
                                if (i < numberOfAreas)
                                {
                                    areas[3].BooleanInput[i + 2000].BoolValue = true;
                                    message = message + "1\n";
                                }
                                else
                                {
                                    areas[3].BooleanInput[i + 2000].BoolValue = false;
                                    message = message + "0\n";
                                }
                            }
                            message = message + "ENDOFFILE";
                            using (FileStream fs = File.Create(filePath))
                            {
                                fs.Write(message + Environment.NewLine, Encoding.Default);
                            }
                        }
                        #endregion

                        #region Retrieve
                       
                        string line;
                        if (areas[3].BooleanOutput[2001].BoolValue == true)
                        {
                            try
                            {


                                using (var sr = new StreamReader(filePath))
                                {

                                    line = sr.ReadLine(); // to skip line 1
                                    for (uint i = 0; i < numberOfZones; i++)
                                    {
                                        if ((line = sr.ReadLine()) != null)
                                        {
                                            if (line == "1")
                                                areas[3].BooleanInput[i + 2000].BoolValue = true;
                                            else
                                                areas[3].BooleanInput[i + 2000].BoolValue = false;
                                        }
                                    }

                                }

                            }
                            catch
                            {
                            }
                        }

                        #endregion


                        #endregion

                        #endregion









                    }






                    break;
            }


        }

        public override void InitializeSystem()
        {
            try
            {


                #region EISC

                for (uint i = 0; i < 4; i++)
                {
                    areas[i] = new ThreeSeriesTcpIpEthernetIntersystemCommunications (225 + i, "127.0.0.2", this); // 225 is IPID E1
                    if (areas[i].Register() == eDeviceRegistrationUnRegistrationResponse.Success)
                        areas[i].SigChange += Eisc_SigChange;
                    else
                        CrestronConsole.PrintLine("EISC not registered");
                }

                #endregion



            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in InitializeSystem: {0}", e.Message);
            }
        }




    }
}