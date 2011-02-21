// sIBL_GUI_XSI_ServerPlugin

using System;
using System.Net;
using XSI.TCP;
using XSI.Helpers;
using Softimage.XSIOM; // XSI object model

// Defining Global Variables
public class sIBL_GUI_XSI_Server_Globals
{
    public static string Address = "127.0.0.1";
    public static int Port = 12288;
    public static int MaxCnx = 10;
}

public class XSIPlugin: Base
{
    public bool Load(PluginRegistrar in_reg)
    {
        in_reg.Author = "Softimage";
        in_reg.Name = "sIBL_GUI_XSI_ServerPlugin";
        in_reg.Major = 1;
        in_reg.Minor = 0;

        in_reg.RegisterCommand("sIBL_GUI_XSI_Server_Start", null);
        in_reg.RegisterCommand("sIBL_GUI_XSI_Server_Stop", null);
        in_reg.RegisterTimerEvent("sIBL_GUI_XSI_Server_ProcessRequestsTimerEvent", 500, 0);
        TimerEvent evTimer = (TimerEvent)GetXSI().EventInfos["sIBL_GUI_XSI_Server_ProcessRequestsTimerEvent"];
        evTimer.Mute = true;

        in_reg.RegisterMenu(siMenuAnchorPoints.siMenuTbGetLightID, "sIBL_GUI_XSI_Server_Menu", false, false);
        in_reg.RegisterProperty("sIBL_GUI_XSI_Server_Property");
        in_reg.RegisterEvent("sIBL_GUI_XSI_Server_StartupEvent", siEventID.siOnStartup);
        //RegistrationInsertionPoint - do not remove this line

        return true;
    }

    public bool Unload(PluginRegistrar in_reg)
    {
        String strPluginName = null;
        strPluginName = in_reg.Name;
        Log(strPluginName + " has been unloaded.");

        // stop server
        sIBL_GUI_XSI_Server.Stop();

        return true;
    }
}

// Timer event calback for executing client requests in XSI.
// Note: This callback is necessary in order to perform XSI operations in the
// main thread. Calling XSI operations from worker threads may put XSI in an
// undefined state.
public class sIBL_GUI_XSI_Server_ProcessRequestsTimerEvent: Base
{
    public bool OnEvent(Context in_ctxt)
    {
        ClientRequests requests = new ClientRequests();
        requests.Process();
        return false; // return false if we don't want to mute the timer
    }
}

// The XSI tcp server implementation
public class sIBL_GUI_XSI_Server
{
    public static RequestsService m_provider = null;
    public static Server m_server = null;
    public static void Start(String in_address, int in_port, int in_max)
    {
        if (m_server == null)
        {
            // Creates a TCP server to execute client requests asynchronously.
            m_provider = new RequestsService();
            m_server = new Server(m_provider, in_port, in_max);
            m_server.Start(in_address);
        }
    }
    public static void Stop()
    {
        if (m_server != null)
        {
            m_server.Stop();
            m_server = null;
        }
    }
}

// sIBL_GUI_XSI_Server_Start command
public class sIBL_GUI_XSI_Server_Start: Base
{
    public bool Execute(Context in_ctxt)
    {
        TimerEvent evTimer = (TimerEvent)GetXSI().EventInfos["sIBL_GUI_XSI_Server_ProcessRequestsTimerEvent"];

        try
        {
            //Starting With Default Values
            sIBL_GUI_XSI_Server.Start(sIBL_GUI_XSI_Server_Globals.Address,
                    sIBL_GUI_XSI_Server_Globals.Port,
                    sIBL_GUI_XSI_Server_Globals.MaxCnx);
            evTimer.Mute = false;
        }
        catch
        {
            sIBL_GUI_XSI_Server.Stop();
            evTimer.Mute = true;
        }
        return true;
    }
}

// sIBL_GUI_XSI_Server_Stop command
public class sIBL_GUI_XSI_Server_Stop: Base
{
    public bool Execute(Context in_ctxt)
    {
        Info("sIBL_GUI_XSI_Server | Server Stopped!");
        sIBL_GUI_XSI_Server.Stop();
        TimerEvent evTimer = (TimerEvent)GetXSI().EventInfos["sIBL_GUI_XSI_Server_ProcessRequestsTimerEvent"];
        evTimer.Mute = true;
        return true;
    }
}

// sIBL_GUI_XSI_Server menu
public class sIBL_GUI_XSI_Server_Menu: Base
{
    public bool Init(Context in_ctxt)
    {
        Menu oMenu = null;
        oMenu = (Menu)in_ctxt.Source;
        oMenu.AddCallbackItem("sIBL_GUI_XSI_Server Properties", "OnOpensIBL_GUI_XSI_ServerPPG");
        return true;
    }

    public bool OnOpensIBL_GUI_XSI_ServerPPG(Context in_ctxt)
    {
        try
        {
            Array inspectobjArgs = Array.CreateInstance(typeof(Object), 5);
            inspectobjArgs.SetValue(sIBL_GUI_XSI_Server_Property.PSet(GetXSI()), 0);
            GetXSI().ExecuteCommand("InspectObj", inspectobjArgs);
        }
        catch (Exception e)
        {
            Error(e.ToString());
        }
        return true;
    }
}

//
// sIBL_GUI_XSI_Server_Property: Provides the UI for managing the sIBL_GUI_XSI_Server
//

public class sIBL_GUI_XSI_Server_Property: Base
{
    public bool Define(Context in_ctxt)
    {
        try
        {
            CustomProperty oCustomProperty = (CustomProperty)in_ctxt.Source;
            oCustomProperty.AddParameter2("Address", siVariantType.siString, sIBL_GUI_XSI_Server_Globals.Address, null, null, null, null, siParamClassification.siClassifUnknown, (int)(siCapabilities.siPersistable | siCapabilities.siReadOnly), null, null, null);
            oCustomProperty.AddParameter2("Port", siVariantType.siInt4, sIBL_GUI_XSI_Server_Globals.Port, 10000, 65536, 10000, 65536, siParamClassification.siClassifUnknown, (int)siCapabilities.siPersistable, null, null, null);
            oCustomProperty.AddParameter2("MaxCnx", siVariantType.siInt4, sIBL_GUI_XSI_Server_Globals.MaxCnx, 1, 20, 1, 20, siParamClassification.siClassifUnknown, (int)siCapabilities.siPersistable, null, null, null);
            oCustomProperty.AddParameter2("ProcessReqTimer", siVariantType.siInt4, 500, 100, 10000, 100, 10000, siParamClassification.siClassifUnknown, (int)siCapabilities.siPersistable, null, null, null);
            oCustomProperty.AddParameter2("EnableRequests", siVariantType.siBool, true, null, null, null, null, siParamClassification.siClassifUnknown, (int)siCapabilities.siPersistable, null, null, null);
        }
        catch (Exception e)
        {
            Error(e.ToString());
        }

        return true;
    }

    public bool DefineLayout(Context in_ctxt)
    {
        try
        {
            PPGLayout oLayout = (PPGLayout)in_ctxt.Source;
            oLayout.Clear();

            oLayout.AddGroup("Server", true, 0);

            oLayout.AddGroup(null, true, 0);
            oLayout.AddRow();
            oLayout.AddButton("StartServer", "Start");
            oLayout.AddButton("StopServer", "Stop");
            oLayout.EndRow();
            oLayout.EndGroup();

            oLayout.AddGroup(null, true, 0);
            oLayout.AddItem("Address", "Address", null);
            oLayout.AddRow();
            oLayout.AddButton("GetHostAddress", "Use Host Address");
            oLayout.AddButton("GetLocalHostAddress", "Use Local Host Address");
            oLayout.EndRow();
            oLayout.EndGroup();

            oLayout.AddItem("Port", "Port", null);
            oLayout.AddItem("MaxCnx", "Maximum Connections", null);
            oLayout.EndGroup();

            oLayout.AddGroup("Requests", true, 0);
            oLayout.AddItem("EnableRequests", "Enable Processing", null);
            oLayout.AddItem("ProcessReqTimer", "Processing Interval (ms)", null);

            oLayout.AddGroup(null, true, 0);
            oLayout.AddRow();
            oLayout.AddButton("LogRequests", "Log");
            oLayout.AddButton("ClearRequests", "Clear");
            oLayout.EndRow();
            oLayout.EndGroup();

            oLayout.EndGroup();
        }
        catch (Exception e)
        {
            Error(e.ToString());
        }

        return true;
    }

    public bool PPGEvent(Context in_ctxt)
    {
        try
        {
            PPGEventContext ppgctxt = (PPGEventContext)in_ctxt;
            siPPGEventID eventID = ppgctxt.EventID;

            if (eventID == siPPGEventID.siParameterChange)
            {
                // The Source of the event is the parameter itself
                Parameter changed = (Parameter)ppgctxt.Source;
                if ("ProcessReqTimer" == changed.ScriptName)
                {
                    int interval = (int)changed.GetValue2(null);
                    TimerEvent evTimer = (TimerEvent)GetXSI().EventInfos["sIBL_GUI_XSI_Server_ProcessRequestsTimerEvent"];
                    evTimer.Reset(interval, 0);
                }
                else if ("EnableRequests" == changed.ScriptName)
                {
                    bool bValue = (bool)changed.GetValue2(null);
                    EventInfo evTimer = GetXSI().EventInfos["sIBL_GUI_XSI_Server_ProcessRequestsTimerEvent"];
                    evTimer.Mute = bValue == false;
                }

            }
            else if (eventID == siPPGEventID.siButtonClicked)
            {
                String buttonPressed = (String)ppgctxt.GetAttribute("Button");

                if (buttonPressed == "GetHostAddress")
                {
                    // Set host param with this host address
                    IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());
                    IPAddress[] ipAddresses = hostEntry.AddressList;

                    Property prop = (Property)ppgctxt.Source;
                    Parameter adr = prop.Parameters["Address"];
                    adr.PutValue2(null, ipAddresses[0].ToString());
                }
                else if (buttonPressed == "GetLocalHostAddress")
                {
                    // Set host param with this host address
                    Property prop = (Property)ppgctxt.Source;
                    Parameter adr = prop.Parameters["Address"];
                    adr.PutValue2(null, "127.0.0.1");
                }
                else if (buttonPressed == "LogRequests")
                {
                    Info("Logging all requests ...");
                    ClientRequests requests = new ClientRequests();
                    requests.Log();
                }
                else if (buttonPressed == "ClearRequests")
                {
                    Info("Removing all requests ...");
                    ClientRequests requests = new ClientRequests();
                    requests.Clear();
                }
                else if (buttonPressed == "StartServer" || buttonPressed == "StopServer")
                {
                    TimerEvent evTimer = (TimerEvent)GetXSI().EventInfos["sIBL_GUI_XSI_Server_ProcessRequestsTimerEvent"];
                    if (buttonPressed == "StartServer")
                    {
                        // Start server
                        Info("sIBL_GUI_XSI_Server | Server Started!");
                        Property prop = (Property)ppgctxt.Source;
                        sIBL_GUI_XSI_Server.Start((String)prop.Parameters["Address"].GetValue2(null),
                                    (int)prop.Parameters["Port"].GetValue2(null),
                                    (int)prop.Parameters["MaxCnx"].GetValue2(null));
                        evTimer.Mute = false;
                    }
                    else if (buttonPressed == "StopServer")
                    {
                        Info("sIBL_GUI_XSI_Server | Server Stopped!");
                        sIBL_GUI_XSI_Server.Stop();
                        evTimer.Mute = true;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Error(e.ToString());
        }

        return true;
    }

    static public Property PSet(XSIApplication in_xsi)
    {
        Property prop = null;
        try
        {
            Model root = in_xsi.ActiveSceneRoot;
            prop = root.Properties["sIBL_GUI_XSI_Server_Property"];

            if (prop == null)
            {
                prop = root.AddProperty("sIBL_GUI_XSI_Server_Property", false, "sIBL_GUI_XSI_Server_Property");
            }
        }
        catch (Exception e)
        {
            in_xsi.LogMessage(e.ToString(), siSeverity.siError);
        }
        return prop;
    }

}

// sIBL_GUI_XSI_Server_StartupEvent
public class sIBL_GUI_XSI_Server_StartupEvent: Base
{
    public bool OnEvent(Context in_ctxt)
    {
        GetXSI().ExecuteCommand("sIBL_GUI_XSI_Server_Start", null);

        return true;
    }
}