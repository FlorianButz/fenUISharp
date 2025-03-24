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
        void DragEnter([In] IntPtr pDataObj, [In] uint grfKeyState, [In] POINT pt, [In, Out] ref uint pdwEffect);
        void DragOver([In] uint grfKeyState, [In] POINT pt, [In, Out] ref uint pdwEffect);
        void DragLeave();
        void Drop([In] IntPtr pDataObj, [In] uint grfKeyState, [In] POINT pt, [In, Out] ref uint pdwEffect);
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


    public enum DropType : int
    {
        FileDrop = 15,
        UnicodeText = 13,
        Text = 1,
        Bitmap = 2,
        DeviceIndependentBitmap = 8
    }
    public class FDropData
    {
        public DropType dropType;
        private string[] stringData;

        public FDropData(DropType dropType, string[] stringData)
        {
            this.dropType = dropType;
            this.stringData = stringData;
        }

        /// <summary>
        /// This method returns different types of objects depending on the dropType.
        /// FileDrop: string[]
        /// UnicodeText / Text: string
        /// </summary>
        /// <returns></returns>
        public object? GetData()
        {
            switch (dropType)
            {
                case DropType.FileDrop:
                    return stringData;
                case DropType.UnicodeText:
                case DropType.Text:
                    return stringData[0];
                case DropType.Bitmap:
                    throw new NotImplementedException("Bitmap drop data has not been implemented yet.");
            }

            return null;
        }
    }

    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    public class DragDropHandler : IDropTarget
    {
        public Action? dragEnter { get; set; }
        public Action? dragOver { get; set; }
        public Action? dragLeave { get; set; }
        public Action<FDropData>? dragDrop { get; set; }

        public DROPEFFECT dropeffect { get; set; } = DROPEFFECT.None;

        public void DragEnter([In] IntPtr pDataObj, [In] uint grfKeyState, [In] POINT pt, [In, Out] ref uint pdwEffect)
        {
            pdwEffect = (uint)dropeffect; // Temporarily disallow drag drop operations.
            dragEnter?.Invoke();
        }

        void IDropTarget.DragOver(uint grfKeyState, POINT pt, ref uint pdwEffect)
        {
            pdwEffect = (uint)dropeffect;
            dragOver?.Invoke();
        }

        void IDropTarget.DragLeave()
        {
            dragLeave?.Invoke();
        }

        public void Drop([In] IntPtr pDataObj, [In] uint grfKeyState, [In] POINT pt, [In, Out] ref uint pdwEffect)
        {
            // Add other file drop formats

            // Convert the IntPtr to IDataObject using the built-in COM interface
            IDataObject dataObject = (IDataObject)Marshal.GetObjectForIUnknown(pDataObj);

            // Set up FORMATETC for CF_HDROP using System.Runtime.InteropServices.ComTypes.FORMATETC
            FORMATETC formatEtc = new FORMATETC
            {
                cfFormat = (int)DropType.FileDrop,
                ptd = IntPtr.Zero,
                dwAspect = DVASPECT.DVASPECT_CONTENT,
                lindex = -1,
                tymed = TYMED.TYMED_HGLOBAL
            };

            // Retrieve the data in a STGMEDIUM structure
            STGMEDIUM stgMedium;
            dataObject.GetData(ref formatEtc, out stgMedium);

            try
            {
                // Get the number of files dropped
                uint fileCount = DragDropRegistration.DragQueryFile(stgMedium.unionmember, 0xFFFFFFFF, null, 0);
                string[] filePathList = new string[fileCount];

                for (uint i = 0; i < fileCount; i++)
                {
                    StringBuilder fileName = new StringBuilder(260);
                    if (DragDropRegistration.DragQueryFile(stgMedium.unionmember, i, fileName, (uint)fileName.Capacity) > 0)
                    {
                        string filePath = fileName.ToString();
                        filePathList[i] = filePath;
                    }
                }

                dragDrop?.Invoke(new FDropData(DropType.FileDrop, filePathList));
            }
            finally
            {
                // Always release the STGMEDIUM to avoid memory leaks
                DragDropRegistration.ReleaseStgMedium(ref stgMedium);
            }
        }
    }

    public static class DragDropRegistration
    {
        private static bool _hasBeenInitialized = false;

        public static void Initialize()
        {
            if (_hasBeenInitialized) return;
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

        // Retrieves the data from an IDataObject.
        [DllImport("ole32.dll", CallingConvention = CallingConvention.StdCall)]
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
