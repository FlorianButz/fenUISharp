using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace FenUISharp
{
    // COM interface definition for drop target.
    [ComImport, Guid("00000122-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IDropTarget
    {
        void DragEnter([In] IntPtr pDataObj, [In] uint grfKeyState, [In] OLENativeHelper.POINT pt, [In, Out] ref uint pdwEffect);
        void DragOver([In] uint grfKeyState, [In] OLENativeHelper.POINT pt, [In, Out] ref uint pdwEffect);
        void DragLeave();
        void Drop([In] IntPtr pDataObj, [In] uint grfKeyState, [In] OLENativeHelper.POINT pt, [In, Out] ref uint pdwEffect);
    }

    [Flags]
    public enum DROPEFFECT : uint
    {
        None = 0,
        Copy = 1,
        Move = 2,
        Link = 4,
        Scroll = 0x80000000
    }

    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    public class DragDropHandler : IDropTarget
    {
        public Action? dragEnter { get; set; }
        public Action? dragOver { get; set; }
        public Action? dragLeave { get; set; }
        public Action? dragDrop { get; set; }

        public void DragEnter([In] IntPtr pDataObj, [In] uint grfKeyState, [In] OLENativeHelper.POINT pt, [In, Out] ref uint pdwEffect)
        {            
            pdwEffect = (uint)DROPEFFECT.None; // Temporarily disallow drag drop operations.
            // Transfer data to event

            dragEnter?.Invoke();
        }

        void IDropTarget.DragOver(uint grfKeyState, OLENativeHelper.POINT pt, ref uint pdwEffect)
        {
            pdwEffect = (uint)DROPEFFECT.None;
            dragOver?.Invoke();
        }

        void IDropTarget.DragLeave()
        {            
            dragLeave?.Invoke();
        }

        public void Drop([In] IntPtr pDataObj, [In] uint grfKeyState, [In] OLENativeHelper.POINT pt, [In, Out] ref uint pdwEffect)
        {
            // Convert the pointer to an IDataObject.
            IDataObject dataObject = (IDataObject)Marshal.GetObjectForIUnknown(pDataObj);

            // Set up the format.
            OLENativeHelper.FORMATETC format = new OLENativeHelper.FORMATETC
            {
                cfFormat = (short)OLENativeHelper.DataFormats.CF_HDROP,
                ptd = IntPtr.Zero,
                dwAspect = OLENativeHelper.DVASPECT.DVASPECT_CONTENT,
                lindex = -1,
                tymed = OLENativeHelper.TYMED.TYMED_HGLOBAL
            };

            // Get the data using OleGetData.
            OLENativeHelper.STGMEDIUM stgMedium;
            int hr = OLENativeHelper.OleGetData(dataObject, ref format, out stgMedium);
            if (hr == 0) // S_OK
            {
                // Retrieve the number of dropped files.
                uint fileCount = DragDropRegistration.DragQueryFile(stgMedium.hGlobal, 0xFFFFFFFF, null, 0);
                for (uint i = 0; i < fileCount; i++)
                {
                    // Get the required length of the file name.
                    int fileNameLength = (int)DragDropRegistration.DragQueryFile(stgMedium.hGlobal, i, null, 0) + 1;
                    StringBuilder sb = new StringBuilder(fileNameLength);
                    DragDropRegistration.DragQueryFile(stgMedium.hGlobal, i, sb, (uint)sb.Capacity);
                    Console.WriteLine("Dropped File: " + sb.ToString());
                }

                OLENativeHelper.ReleaseStgMedium(ref stgMedium);
            }
            else
            {
                Console.WriteLine("Failed to get drop data. HRESULT: " + hr);
            }

            dragDrop?.Invoke();
        }
    }

    public static class DragDropRegistration
    {
        private static bool _hasBeenInitialized = false;

        public static void Initialize(){
            if(_hasBeenInitialized) return;
            _hasBeenInitialized = true;

            OleInitialize(IntPtr.Zero);
        }

        [DllImport("ole32.dll", ExactSpelling = true)]
        public static extern int OleInitialize(IntPtr pvReserved);

        [DllImport("ole32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern int RegisterDragDrop(IntPtr hwnd, IntPtr pDropTarget);

        [DllImport("ole32.dll")]
        public static extern int RevokeDragDrop(IntPtr hwnd);

        [DllImport("shell32.dll")]
        public static extern uint DragQueryFile(IntPtr hDrop, uint iFile, StringBuilder lpszFile, uint cch);

        [DllImport("shell32.dll")]
        public static extern void DragFinish(IntPtr hDrop);
    }

    public static class OLENativeHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        public static class DataFormats
        {
            public const int CF_HDROP = 15;       // File drop format.
            public const int CF_UNICODETEXT = 13; // Unicode text.
            public const int CF_TEXT = 1;         // ANSI text.
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct FORMATETC
        {
            public short cfFormat;
            public IntPtr ptd;           // Target device (not used, so set to IntPtr.Zero).
            public DVASPECT dwAspect;    // Aspect (content, thumbnail, etc.).
            public int lindex;           // Part of the aspect; -1 for all parts.
            public TYMED tymed;          // Storage medium type.
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct STGMEDIUM
        {
            public TYMED tymed;          // Storage medium type.
            public IntPtr hGlobal;       // Handle to the data (used when tymed is TYMED_HGLOBAL).
            public IntPtr pUnkForRelease; // For custom release (typically IntPtr.Zero).
        }

        public enum DVASPECT : uint
        {
            DVASPECT_CONTENT = 1,
            DVASPECT_THUMBNAIL = 2,
            DVASPECT_ICON = 4,
            DVASPECT_DOCPRINT = 8
        }

        [Flags]
        public enum TYMED : uint
        {
            TYMED_HGLOBAL = 1,
            TYMED_FILE = 2,
            TYMED_ISTREAM = 4,
            TYMED_ISTORAGE = 8,
            TYMED_GDI = 16,
            TYMED_MFPICT = 32,
            TYMED_ENHMF = 64,
            TYMED_NULL = 0
        }

        // Retrieves the data from an IDataObject.
        [DllImport("ole32.dll")]
        public static extern int OleGetData(
            [MarshalAs(UnmanagedType.Interface)] IDataObject pDataObj,
            ref FORMATETC pformatetcIn,
            out STGMEDIUM pmedium);

        // Checks if the requested format is available.
        [DllImport("ole32.dll")]
        public static extern int OleQueryGetData(
            [MarshalAs(UnmanagedType.Interface)] IDataObject pDataObj,
            ref FORMATETC pformatetc);

        // Releases the storage medium.
        [DllImport("ole32.dll")]
        public static extern void ReleaseStgMedium(ref STGMEDIUM pmedium);
    }
}
