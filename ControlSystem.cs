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

        ThreeSeriesTcpIpEthernetIntersystemCommunications [] links = new ThreeSeriesTcpIpEthernetIntersystemCommunications [4];
        ThreeSeriesTcpIpEthernetIntersystemCommunications[] scenes= new ThreeSeriesTcpIpEthernetIntersystemCommunications[8];
        uint numberOfZones = 250;
        bool[] activeZones = new bool[250];

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

        // Store and Ret in D1-8 change from E6-E8 change below
        private void SceneRet(uint num)
        {
            
            try
                {

                uint i,j;
                ushort k;
                string line;
                j = num;
                string sceneFilePath = $"/nvram/Scenes/Scene{j}.txt";
                    


                    using (var sr = new StreamReader(sceneFilePath))
                        {
                        string scenename = sr.ReadLine();

                        #region 
                        for (i = 0; i < 250; i++)
                         {
                            if ((line = sr.ReadLine()) != null)
                                {
                                k = (ushort)(Convert.ToUInt16(line)*655); // 65535 to % level
                                scenes[j-1].UShortInput[i + 1].UShortValue = k;
                                }
                         }
                        for (i = 0; i < 250; i++)
                        {
                            if ((line = sr.ReadLine()) != null)
                            {
                            k = (ushort)(Convert.ToUInt16(line) * 655); // 65535 to % level
                            scenes[j - 1].UShortInput[i + 251].UShortValue = k;
                            }
                        }
                        for (i = 0; i < 250; i++)
                        {
                            if ((line = sr.ReadLine()) != null)
                            {
                            k = Convert.ToUInt16(line);
                            scenes[j - 1].UShortInput[i + 501].UShortValue = k;
                            }
                        }
                        for (i = 0; i < 250; i++)
                        {
                            if ((line = sr.ReadLine()) != null)
                            {
                            k = Convert.ToUInt16(line);
                            scenes[j - 1].UShortInput[i + 751].UShortValue = k;
                            }
                        }
                        for (i = 0; i < 250; i++)
                        {
                            if ((line = sr.ReadLine()) != null)
                            {
                            k = Convert.ToUInt16(line);
                            scenes[j - 1].UShortInput[i + 1001].UShortValue = k;
                            }
                        }
                        #endregion
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


                        if (links[k].BooleanOutput[args.Sig.Number].BoolValue == true)
                        {


                            #region Save
                            if (args.Sig.Number % (numberOfZones + 2) == 1)
                            {
                                for (uint i = 0; i < numberOfZones; i++)
                                {

                                    activeZones[i] = links[k].BooleanOutput[areaIndex * (numberOfZones + 2) + 3 + i].BoolValue;
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

                                    links[k].BooleanInput[areaIndex * (numberOfZones + 2) + 3 + i].BoolValue = activeZones[i];
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

                        if (links[3].BooleanOutput[2000].BoolValue == true)
                        {
                           numberOfAreas=  Convert.ToUInt16(links[3].StringOutput[1].StringValue);
                            links[3].UShortInput[1].UShortValue = numberOfAreas;

                            for (uint i = 0; i < 50; i++)
                            {
                                if (i < numberOfAreas)
                                {
                                    links[3].BooleanInput[i + 2000].BoolValue = true;
                                    message = message + "1\n";
                                }
                                else
                                {
                                    links[3].BooleanInput[i + 2000].BoolValue = false;
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
                        ushort numberOfArea =0;
                        if (links[3].BooleanOutput[2001].BoolValue == true)
                        {
                            try
                            {


                                using (var sr = new StreamReader(filePath))
                                {

                                    line = sr.ReadLine(); // to skip line 1
                                    
                                        uint i =0;
                                        while ((line = sr.ReadLine()) != null)
                                        {
                                            
                                            if (line == "1")

                                            {
                                                links[3].BooleanInput[i + 2000].BoolValue = true;
                                                numberOfArea ++;
                                            }
                                            else
                                                links[3].BooleanInput[i + 2000].BoolValue = false;
                                        i++;
                                        }
                                    

                                }
                                links[3].UShortInput[1].UShortValue = numberOfArea;
                            }
                            catch
                            {
                            }
                        }

                        #endregion


                        #endregion

                        
                    }
                    #endregion



                    // Store and Ret in D1-8 change from E6-E8 change below
                    #region Scene Store D1 to D8
                    try
                    {

                        for (uint j = 1; j < 9; j++)
                        {

                            string values = $"Scene {j} \n";
                            int trueValue = 1;
                            string sceneFilePath = $"/nvram/Scenes/Scene{j}.txt";
                            uint i;

                            #region D1
                            if (scenes[j-1].BooleanOutput[1].BoolValue == true)
                            {
                                for (i = 0; i < 250 ; i++)
                                {
                                    trueValue = scenes[j - 1].UShortOutput[i + 1].UShortValue;
                                    values = values + (trueValue/655).ToString() + "\n";
                                }
                                for (i = 0; i < 250; i++)
                                {
                                    trueValue = scenes[j - 1].UShortOutput[i + 251].UShortValue;
                                    values = values + (trueValue / 655).ToString() + "\n";
                                }
                                for (i = 0; i < 250; i++)
                                {
                                    trueValue = scenes[j - 1].UShortOutput[i + 501].UShortValue;
                                    values = values + (trueValue).ToString() + "\n";
                                }
                                for (i = 0; i < 250; i++)
                                {
                                    trueValue = scenes[j - 1].UShortOutput[i + 751].UShortValue;
                                    values = values + (trueValue).ToString() + "\n";
                                }
                                for (i = 0; i < 250; i++)
                                {
                                    trueValue = scenes[j - 1].UShortOutput[i + 1001].UShortValue;
                                    values = values + (trueValue).ToString() + "\n";
                                    }
                                values = values + "ENDOFFILE";
                                using (FileStream fs = File.Create(sceneFilePath))
                                {
                                    fs.Write(values + Environment.NewLine, Encoding.Default);
                                }

                            }
                            #endregion
                            
                            if (scenes[j-1].BooleanOutput[2].BoolValue == true)
                            {
                                SceneRet(j);
                            }

                        }


                    }
                    catch { }


                    #endregion

                    break;
            }


        }

        public override void InitializeSystem()
        {
            try
            {


                #region EISC

                for (uint i = 0; i <4; i++)
                {
                    links[i] = new ThreeSeriesTcpIpEthernetIntersystemCommunications (225 + i, "127.0.0.2", this); // 225 is IPID E1
                    if (links[i].Register() == eDeviceRegistrationUnRegistrationResponse.Success)
                        links[i].SigChange += Eisc_SigChange;
                    else
                        CrestronConsole.PrintLine("EISC not registered");
                }

                for (uint i = 0; i < 8; i++)
                {

                    scenes[i] = new ThreeSeriesTcpIpEthernetIntersystemCommunications(209 + i, "127.0.0.2", this); // 225 is IPID D1
                    if (scenes[i].Register() == eDeviceRegistrationUnRegistrationResponse.Success)
                        scenes[i].SigChange += Eisc_SigChange;
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