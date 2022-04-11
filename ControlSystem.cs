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
        Xlsheet xlsheet = new Xlsheet();
        string FilePath = "/nvram/CB/Settings.xlsx";
        ThreeSeriesTcpIpEthernetIntersystemCommunications[] links = new ThreeSeriesTcpIpEthernetIntersystemCommunications [5];
        ThreeSeriesTcpIpEthernetIntersystemCommunications[] scenes= new ThreeSeriesTcpIpEthernetIntersystemCommunications[8];
        uint numberOfZones = 250;
        bool[] activeZones = new bool[250];
        string[,] sceneData,zoneAreaData, tempdata;

        public ControlSystem()
            : base()
        {
            try
            {
                Thread.MaxNumberOfUserThreads = 20;

                CrestronConsole.AddNewConsoleCommand(RET, "Read", "Output the file content.", ConsoleAccessLevelEnum.AccessAdministrator);


            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in the constructor: {0}", e.Message);
            }
        }
        private void InfoRet()
        {

            try  // Scene Retrieve
            {

                // RET Excel with 1st Row and Column as index 0,0
                sceneData = xlsheet.ReadExcel(FilePath, 0);
                zoneAreaData = xlsheet.ReadExcel(FilePath, 1);
            }
            catch (Exception ex)
            {
                CrestronConsole.PrintLine(ex.ToString());
            }
        }
        private void RET(string args)
        {

            tempdata = xlsheet.ReadExcel(FilePath, 1);


            for (uint i = 0; i < 51; i++)
            {
                CrestronConsole.PrintLine(tempdata[1, i].ToString());
            }

        }



        private void ZoneAreaWrite(uint areanumber, uint k, uint areaIndex)
        {

            try
            {

              
                for (uint i = 0; i < numberOfZones; i++)
                {
                    links[k].BooleanInput[areaIndex * (numberOfZones + 2) + 3 + i].BoolValue = links[k].BooleanOutput[areaIndex * (numberOfZones + 2) + 3 + i].BoolValue;
                    activeZones[i] = links[k].BooleanOutput[areaIndex * (numberOfZones + 2) + 3 + i].BoolValue;

                    if (activeZones[i] == true)
                        zoneAreaData[i + 2, areanumber] = "1";
                    else
                        zoneAreaData[i + 2, areanumber] = "0";
                }

               


            }
            catch
            {

            }



        }

        private void ZoneAreaRead(uint args,uint k, uint areaIndex)
        {

            try
            {

 
                    for (uint i = 0; i < numberOfZones; i++)
                    {
                        if (zoneAreaData[i + 2, args].ToString() == "1")
                            activeZones[i] = true;
                        else
                            activeZones[i] = false;

                        links[k].BooleanInput[areaIndex * (numberOfZones + 2) + 3 + i].BoolValue = activeZones[i];
                    }

                
            }
            catch 
            {
            }


        }


        private void SceneRet(uint num)
        {

            try
            {

              
                int k;
                uint j = num;
                for (uint i = 1; i < 251; i++)
                {

                    k = (ushort)(Convert.ToUInt16(sceneData[i, j]) * 655); // 65535 to % level
                    scenes[j - 1].UShortInput[i].UShortValue = (ushort)k;

                }
                for (uint i = 1; i < 251; i++)
                {

                    k = (ushort)(Convert.ToUInt16(sceneData[i + 251, j]) * 655); // 65535 to % level
                    scenes[j - 1].UShortInput[i + 250].UShortValue = (ushort)k;

                }
                for (uint i = 1; i < 251; i++)
                {

                    k = (ushort)(Convert.ToUInt16(sceneData[i + 502, j])); // Red
                    scenes[j - 1].UShortInput[i + 500].UShortValue = (ushort)k;

                }
                for (uint i = 1; i < 251; i++)
                {

                    k = (ushort)(Convert.ToUInt16(sceneData[i + 753, j])); // Green
                    scenes[j - 1].UShortInput[i + 750].UShortValue = (ushort)k;

                }
                for (uint i = 1; i < 251; i++)
                {

                    k = (ushort)(Convert.ToUInt16(sceneData[i + 1004, j])); // Blue
                    scenes[j - 1].UShortInput[i + 1000].UShortValue = (ushort)k;

                }

            }
            catch
            {
             
            }

        }

        private void SceneSave(uint num)

        {
            try
            {

                
                uint j = num;
                uint i,trueValue;
                

                for (i = 0; i < 250; i++)
                {
                    trueValue = scenes[j - 1].UShortOutput[i + 1].UShortValue;
                    sceneData[i+1,j] =  (trueValue / 655).ToString();
                }
                for (i = 0; i < 250; i++)
                {
                    trueValue = scenes[j - 1].UShortOutput[i + 251].UShortValue;
                    sceneData[i + 252, j] = (trueValue / 655).ToString();
                }
                for (i = 0; i < 250; i++)
                {
                    trueValue = scenes[j - 1].UShortOutput[i + 501].UShortValue;
                    sceneData[i + 503, j] =  (trueValue).ToString();
                }
                for (i = 0; i < 250; i++)
                {
                    trueValue = scenes[j - 1].UShortOutput[i + 751].UShortValue;
                    sceneData[i + 754, j] = (trueValue).ToString();
                }
                for (i = 0; i < 250; i++)
                {
                    trueValue = scenes[j - 1].UShortOutput[i + 1001].UShortValue;
                    sceneData[i + 1005, j] = (trueValue).ToString();
                }





                xlsheet.WriteExcel(sceneData, FilePath, 0);
            }
            catch
            {
            }


        }

        
        private void Eisc_SigChange(BasicTriList currentDevice, SigEventArgs args)
        {
            
            ushort numberOfAreas;
            try
            {

                switch (args.Sig.Type)
                {
                    case eSigType.Bool:

                        #region Scene D1 to D8
                        try
                        {
                            for (uint j = 1; j < 9; j++)
                            {
                                if (scenes[j - 1].BooleanOutput[1].BoolValue == true)
                                {

                                    SceneSave(j);
                                }
                                if (scenes[j - 1].BooleanOutput[2].BoolValue == true)
                                {
                                    SceneRet(j);
                                }

                            }


                        }
                        catch { }


                        #endregion




                        #region Zone area E1 to E4
                        
                        
                        #region Number of Areas

                        #region Set and Save

                        if (links[3].BooleanOutput[2000].BoolValue == true)
                        {
                            numberOfAreas = Convert.ToUInt16(links[3].StringOutput[1].StringValue);
                            links[3].UShortInput[1].UShortValue = numberOfAreas;

                            for (uint i = 0; i < 50; i++)
                            {
                                if (i < numberOfAreas)
                                {
                                    links[3].BooleanInput[i + 2000].BoolValue = true;
                                    zoneAreaData[1, i + 1] = "1";
                                }
                                else
                                {
                                    links[3].BooleanInput[i + 2000].BoolValue = false;
                                    zoneAreaData[1, i + 1] = "0";
                                }
                            }
                            xlsheet.WriteExcel(zoneAreaData, FilePath, 1);
                        }
                        #endregion

                        #region Retrieve

                        ushort numberOfArea = 0;
                        if (links[3].BooleanOutput[2001].BoolValue == true)
                        {
                            try
                            {


                                for (uint i = 0; i < 50; i++)
                                {
                                    if (zoneAreaData[1, i + 1].ToString() == "1")

                                    {
                                        links[3].BooleanInput[i + 2000].BoolValue = true;
                                        numberOfArea++;
                                    }
                                    else
                                        links[3].BooleanInput[i + 2000].BoolValue = false;
                                }

                                links[3].UShortInput[1].UShortValue = numberOfArea;
                            }
                            catch
                            {
                            }
                        }

                        #endregion


                        #endregion

                        #region Zone Area
                        for (uint k = 0; k < 4; k++)
                        {
                            uint areaIndex = args.Sig.Number / (numberOfZones + 2);
                            uint areaNumber = areaIndex + 1;
                            uint trueAreaNumber = areaNumber + k * 15;


                            if (links[k].BooleanOutput[args.Sig.Number].BoolValue == true)
                            {


                                #region Save
                                if (args.Sig.Number % (numberOfZones + 2) == 1)
                                {
                                    ZoneAreaWrite(trueAreaNumber, k, areaIndex);
                                    xlsheet.WriteExcel(zoneAreaData, FilePath, 1);

                                }
                                #endregion

                                #region Retrieve
                                if (args.Sig.Number % (numberOfZones + 2) == 2)
                                {
                                    ZoneAreaRead(trueAreaNumber, k, areaIndex);

                                }
                                #endregion

                            }
                        }
                            #endregion

                            #region Name
                            // Save
                            if (links[4].BooleanOutput[args.Sig.Number].BoolValue == true)
                            {
                                for (uint i = 1; i < 301; i++)
                                {
                                    if (links[4].BooleanOutput[i].BoolValue == true)
                                    {

                                        if (i < 251)
                                            zoneAreaData[i + 1, 0] = links[4].StringOutput[1].StringValue;

                                        if (i > 250)
                                            zoneAreaData[0, i - 250] = links[4].StringOutput[1].StringValue;

                                    }
                                }
                                xlsheet.WriteExcel(zoneAreaData, FilePath, 1);
                            }

                            if (links[4].BooleanOutput[301].BoolValue == true)
                                for (uint i = 1; i < 301; i++)
                            {

                                if (i < 251)
                                    links[4].StringInput[i].StringValue = zoneAreaData[i + 1, 0];

                                if (i > 250)
                                    links[4].StringInput[i].StringValue = zoneAreaData[0, i - 250];


                            }


                            #endregion
                        #endregion



                        break;
                }
            }
            catch (Exception ex)
            {
                CrestronConsole.PrintLine(ex.ToString());
            }

        }

        public override void InitializeSystem()
        {
            try
            {


                #region EISC Reg

                for (uint i = 0; i <5; i++)
                {
                    links[i] = new ThreeSeriesTcpIpEthernetIntersystemCommunications (225 + i, "127.0.0.2", this); // 225 is IPID E1
                    if (links[i].Register() == eDeviceRegistrationUnRegistrationResponse.Success)
                        links[i].SigChange += Eisc_SigChange;
                    else
                        CrestronConsole.PrintLine("EISC not registered");
                }

                for (uint i = 0; i < 8; i++)
                {

                    scenes[i] = new ThreeSeriesTcpIpEthernetIntersystemCommunications(209 + i, "127.0.0.2", this); // 209 is IPID D1
                    if (scenes[i].Register() == eDeviceRegistrationUnRegistrationResponse.Success)
                        scenes[i].SigChange += Eisc_SigChange;
                    else
                        CrestronConsole.PrintLine("EISC not registered");
                }
                #endregion

                #region Info Ret
                InfoRet();
                #endregion

            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine("Error in InitializeSystem: {0}", e.Message);
            }
        }




    }
}