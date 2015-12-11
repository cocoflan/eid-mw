﻿using System;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;



/*
[StructLayout(LayoutKind.Sequential)]
public struct CSCbStruct
{
    public CbNewSrc theCbNewSrc;
    public CbNewStringData theCbNewStringData;
    public Cbnewbindata theCbnewbindata;
    public Cblog theCbLog;
    public Cblog theCbLogv;
    public Cbnewstate theCbnewstate;
    public Cbpinop_result theCbpinopResult;
}*/

namespace eIDViewer
{

    public static class eIDViewerBackend
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void CbNewSrc(eid_vwr_source eid_vwr_source);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void CbNewStringData([MarshalAs(UnmanagedType.LPStr)] string label, [MarshalAs(UnmanagedType.LPStr)]string data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void Cbnewbindata([MarshalAs(UnmanagedType.LPStr)] string label, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] data, int datalen);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void Cblog(eid_vwr_loglevel logLevel, [MarshalAs(UnmanagedType.LPStr)] string str);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void Cbnewstate(eid_vwr_states state);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void Cbpinop_result(eid_vwr_pinops pinop, eid_vwr_result result);

        public static eIDViewerBackendData theData {get;set;}

        //list all functions of the C backend we need to call
        //[DllImport("eIDViewerBackend.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        //public static extern int eid_vwr_createcallbacks(ref CSCbStruct eid_vwr_ui_callbacks);       
        private static Cblog mylog = eIDViewerBackend.CSCblog;
        private static CbNewSrc mynewsrc = eIDViewerBackend.CSCbNewSrc;
        private static CbNewStringData mystringdata = eIDViewerBackend.CSCbNewStringData;
        private static Cbnewbindata mybindata = eIDViewerBackend.CSCbnewbindata;
        private static Cbnewstate mynewstate = eIDViewerBackend.CSCbnewstate;
        private static Cbpinop_result mypinopresult = eIDViewerBackend.CSCbpinopResult;

        [DllImport("eIDViewerBackend.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern int eid_vwr_set_cbfuncs(CbNewSrc theCbNewSrc, CbNewStringData theCbNewStringData,
            Cbnewbindata theCbnewbindata, Cblog theCbLog, Cbnewstate theCbnewstate, Cbpinop_result theCbpinopResult);

        [DllImport("eIDViewerBackend.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern void eid_vwr_be_mainloop();
                                                            
        public static void Init()
        {
            eid_vwr_set_cbfuncs(mynewsrc, mystringdata,
                mybindata, mylog, mynewstate, mypinopresult);

            theData.cardreader_icon = new BitmapImage(new Uri("Resources/state_noreaders.png", UriKind.Relative));
            /*
            //fill in the functions reference struct
            eIDViewerBackend.mCSCbStruct.theCbNewSrc = eIDViewerBackend.CSCbNewSrc;
            eIDViewerBackend.mCSCbStruct.theCbNewStringData = eIDViewerBackend.CSCbNewStringData;
            eIDViewerBackend.mCSCbStruct.theCbnewbindata = eIDViewerBackend.CSCbnewbindata;
            eIDViewerBackend.mCSCbStruct.theCbLog = eIDViewerBackend.CSCblog;
            eIDViewerBackend.mCSCbStruct.theCbLogv = null;
            eIDViewerBackend.mCSCbStruct.theCbnewstate = eIDViewerBackend.CSCbnewstate;
            eIDViewerBackend.mCSCbStruct.theCbpinopResult = eIDViewerBackend.CSCbpinopResult;

            var managedItem = (MMTPConxNack)Marshal.PtrToStructure(pointer,typeof(MMTPConxNack));

            eid_vwr_createcallbacks(ref mCSCbStruct);*/
        }

        private static void AdjustIconImage(string fileName)
        {
            if (App.Current.Dispatcher.CheckAccess())
            {
                theData.cardreader_icon = new BitmapImage(new Uri(fileName, UriKind.Relative));
            }
            else
            {
                App.Current.Dispatcher.BeginInvoke(
                  DispatcherPriority.Background,
                  new Action(() => {
                      theData.cardreader_icon = new BitmapImage(new Uri(fileName, UriKind.Relative));
                  }));
            }
        }

        private static void CSCbNewSrc(eid_vwr_source eid_vwr_source)
        {
            Console.WriteLine("CbNewSrc called ");
            Console.WriteLine(eid_vwr_source.ToString());
            theData.logText += "CSCbNewSrc called " + eid_vwr_source.ToString() + "\n";

            switch (eid_vwr_source)
            {
                case eid_vwr_source.EID_VWR_SRC_UNKNOWN:
                    theData.type_kaart = "onbekende kaart";
                    theData.ResetDataValues();
                    AdjustIconImage("Resources\\state_error.png");
                    break;
                case eid_vwr_source.EID_VWR_SRC_NONE:
                    theData.ResetDataValues();
                    AdjustIconImage("Resources\\state_noeidpresent.png");
                    break;
                case eid_vwr_source.EID_VWR_SRC_CARD:
                    theData.type_kaart = "IDENTITEITSKAART";
                    AdjustIconImage("Resources\\state_eidpresent.png");
                    break;
                case eid_vwr_source.EID_VWR_SRC_FILE:
                    theData.type_kaart = "IDENTITEITSKAART";
                    AdjustIconImage("Resources\\state_fileloaded.png");
                    break;
                default:
                    break;
            }
        }
        private static void CSCbNewStringData([MarshalAs(UnmanagedType.LPStr)] string label, [MarshalAs(UnmanagedType.LPStr)]string data)
        {
            Console.WriteLine("CSCbNewStringData called, label = ");
            Console.WriteLine(label);
            Console.WriteLine("data = " + data);
            theData.StoreStringData(label, data);

            theData.logText += "CSCbNewStringData called, data =  " + data + "\n";
        }

        private static void CSCbnewbindata([MarshalAs(UnmanagedType.LPStr)] string label,  [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] data,  int datalen)
        {
            Console.WriteLine("CSCbnewbindata called, label = ");
            Console.WriteLine(label);
            theData.logText += "CSCbnewbindata called " + label + "\n";

            theData.StoreBinData(label, data, datalen);

        }

        private static void CSCblog(eid_vwr_loglevel logLevel, [MarshalAs(UnmanagedType.LPStr)]string str)
        {
            Console.WriteLine("CSCblog called: ");
            Console.WriteLine(str);
            theData.logText += "CSCblogtest called " + str + "\n";
        }

        private static void CSCbnewstate(eid_vwr_states state)
        {
            Console.WriteLine("CSCbnewstate called ");
            Console.WriteLine(state.ToString());
            theData.logText += "CSCbnewstate called " + state.ToString() + "\n";

            switch(state)
            {
                case eid_vwr_states.STATE_CARD_INVALID:
                case eid_vwr_states.STATE_TOKEN_ERROR:
                    theData.ResetDataValues();
                    break;
                default:
                    break;
            }
 
        }
        private static void CSCbpinopResult(eid_vwr_pinops pinop, eid_vwr_result result)
        {
            Console.WriteLine("CSCbpinopResult called ");
            theData.logText += "CSCbpinopResult called, result = " + result.ToString() + "\n";
        }

        public static void backendMainloop()
        {
            eid_vwr_be_mainloop();
        }

       // public CSCbStruct mCSCbStruct;
    }

}
