using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using FenUISharp.Native;

// TODO: FIX NOT ALL DRAG DESTINATIONS NOT WORKING

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

    // COM interface definition for drag source
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("00000121-0000-0000-C000-000000000046")]
    public interface IDropSource
    {
        [PreserveSig]
        int QueryContinueDrag(
            [MarshalAs(UnmanagedType.Bool)] bool fEscapePressed,
            uint grfKeyState);

        [PreserveSig]
        int GiveFeedback(uint dwEffect);
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
        UnicodeText = 13,
        AnsiText = 1,
        Bitmap = 2,
        DeviceIndependentBitmap = 8,
        FileDrop = 15,

        AnyText = UnicodeText | AnsiText
    }

    public class FDropData
    {
        public DropType dropType;

        private string? textData;
        private string[]? fileData;
        private IntPtr? bitmapPointer;

        public FDropData(DropType dropType, string[] stringData)
        {
            this.dropType = dropType;
            this.fileData = stringData;
        }

        public FDropData(DropType dropType, string textData)
        {
            this.dropType = dropType;
            this.textData = textData;
        }

        public FDropData(DropType dropType, IntPtr bitmapPointer)
        {
            this.dropType = dropType;
            this.bitmapPointer = bitmapPointer;
        }

        /// <summary>
        /// This method returns different types of objects depending on the dropType.
        /// FileDrop: string[]
        /// UnicodeText / Text: string
        /// Bitmap / DIB: IntPtr
        /// </summary>
        /// <returns></returns>
        public object? GetData()
        {
            switch (dropType)
            {
                case DropType.FileDrop:
                    return fileData;
                case DropType.UnicodeText:
                case DropType.AnsiText:
                    return textData;
                case DropType.DeviceIndependentBitmap:
                case DropType.Bitmap:
                    return bitmapPointer;
            }

            return null;
        }
    }

    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    public class DragDropHandler : IDropTarget
    {
        public DragDropHandler(Window root) => WindowRoot = root;
        private Window WindowRoot { get; set; }

        public Action<FDropData?>? dragEnter { get; set; }
        public Action<FDropData?>? dragOver { get; set; }
        public Action? dragLeave { get; set; }
        public Action<FDropData?>? dragDrop { get; set; }

        public FDropData? lastDropData;

        public MultiAccess<DROPEFFECT> dropEffect { get; set; } = new MultiAccess<DROPEFFECT>(DROPEFFECT.None);

        public bool IsDragDropActionInProgress { get; set; } = false;

        public void DragEnter([In] IntPtr pDataObj, [In] uint grfKeyState, [In] POINT pt, [In, Out] ref uint pdwEffect)
        {
            pdwEffect = (uint)dropEffect.Value; // Temporarily disallow drag drop operations.

            lastDropData = HandleDropAction(pDataObj);
            IsDragDropActionInProgress = true;
            WindowRoot.Dispatcher.Invoke(() => dragEnter?.Invoke(lastDropData));
        }

        void IDropTarget.DragOver(uint grfKeyState, POINT pt, ref uint pdwEffect)
        {
            pdwEffect = (uint)dropEffect.Value;

            IsDragDropActionInProgress = true;
            WindowRoot.Dispatcher.Invoke(() => dragOver?.Invoke(lastDropData));
        }

        void IDropTarget.DragLeave()
        {
            IsDragDropActionInProgress = false;

            WindowRoot.Dispatcher.Invoke(() => dragLeave?.Invoke());
        }

        public void Drop([In] IntPtr pDataObj, [In] uint grfKeyState, [In] POINT pt, [In, Out] ref uint pdwEffect)
        {
            lastDropData = HandleDropAction(pDataObj);
            WindowRoot.Dispatcher.Invoke(() => dragDrop?.Invoke(lastDropData));
        }

        const int DV_E_FORMATETC = unchecked((int)0x80040064);

        FDropData? HandleDropAction([In] IntPtr pDataObj)
        {
            // Convert the IntPtr to IDataObject using the built-in COM interface
            IDataObject dataObject = (IDataObject)Marshal.GetObjectForIUnknown(pDataObj);

            (DropType type, int format, TYMED tymed)[] supportedFormats = new[]
            {
                (DropType.FileDrop, (int)DropType.FileDrop, TYMED.TYMED_HGLOBAL),
                (DropType.UnicodeText, (int)DropType.UnicodeText, TYMED.TYMED_HGLOBAL),
                (DropType.AnsiText, (int)DropType.AnsiText, TYMED.TYMED_HGLOBAL),
                (DropType.Bitmap, (int)DropType.Bitmap, TYMED.TYMED_GDI),
                (DropType.DeviceIndependentBitmap, (int)DropType.DeviceIndependentBitmap, TYMED.TYMED_HGLOBAL)
            };

            foreach (var (dropType, cfFormat, tymed) in supportedFormats)
            {
                FORMATETC formatEtc = new FORMATETC
                {
                    cfFormat = (short)cfFormat,
                    ptd = IntPtr.Zero,
                    dwAspect = DVASPECT.DVASPECT_CONTENT,
                    lindex = -1,
                    tymed = tymed
                };

                // Check if the format is available.
                try
                {
                    // QueryGetData returns S_OK (0) if the format is available.
                    int queryResult = dataObject.QueryGetData(ref formatEtc);
                    if (queryResult != 0)
                        continue; // Format not available, try next

                    // If available, retrieve the data.
                    STGMEDIUM stgMedium;
                    dataObject.GetData(ref formatEtc, out stgMedium);

                    try
                    {
                        switch (dropType)
                        {
                            case DropType.FileDrop:
                                {
                                    // Handle file drop (CF_HDROP)
                                    uint fileCount = DragDropRegistration.DragQueryFile(stgMedium.unionmember, 0xFFFFFFFF, null, 0);
                                    string[] filePathList = new string[fileCount];

                                    for (uint i = 0; i < fileCount; i++)
                                    {
                                        StringBuilder fileName = new StringBuilder(260);
                                        if (DragDropRegistration.DragQueryFile(stgMedium.unionmember, i, fileName, (uint)fileName.Capacity) > 0)
                                            filePathList[i] = fileName.ToString();
                                    }
                                    return new FDropData(dropType, filePathList);
                                }
                            case DropType.UnicodeText:
                                {
                                    // Handle Unicode text drop (CF_UNICODETEXT)
                                    string text = GetDroppedText(stgMedium.unionmember, true);
                                    return new FDropData(dropType, text == null ? "" : text);
                                }
                            case DropType.AnsiText:
                                {
                                    // Handle ANSI text drop (CF_TEXT)
                                    string text = GetDroppedText(stgMedium.unionmember, false);
                                    return new FDropData(dropType, text == null ? "" : text);
                                }
                            case DropType.Bitmap:
                                {
                                    // Convert HBITMAP to usable format later
                                    IntPtr hBitmap = stgMedium.unionmember;
                                    return new FDropData(dropType, hBitmap);
                                }
                            case DropType.DeviceIndependentBitmap:
                                {
                                    // Convert the global memory block to a bitmap later
                                    return new FDropData(dropType, stgMedium.unionmember);
                                }
                        }
                    }
                    finally
                    {
                        DragDropRegistration.ReleaseStgMedium(ref stgMedium);
                    }
                }
                catch (COMException comEx) when (comEx.ErrorCode == DV_E_FORMATETC)
                {
                    // Format not supported; move on to the next one.
                    continue;
                }
            }

            // If none of the supported formats were available, return null or handle appropriately.
            return null;
        }

        string GetDroppedText(IntPtr hGlobal, bool isUnicode)
        {
            // Lock the HGLOBAL to get a pointer to the data
            IntPtr ptr = GlobalLock(hGlobal);
            if (ptr == IntPtr.Zero)
                return "";

            try
            {
                // Get the size of the global memory block
                int size = GlobalSize(hGlobal).ToInt32();
                if (size <= 0)
                    return "";

                // Copy data into a byte array
                byte[] buffer = new byte[size];
                Marshal.Copy(ptr, buffer, 0, size);

                // Depending on whether it's Unicode or ANSI, decode appropriately.
                if (isUnicode)
                {
                    // Trim potential null terminator bytes if necessary.
                    return Encoding.Unicode.GetString(buffer).TrimEnd('\0');
                }
                else
                {
                    return Encoding.Default.GetString(buffer).TrimEnd('\0');
                }
            }
            finally
            {
                GlobalUnlock(hGlobal);
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalUnlock(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalSize(IntPtr hMem);
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


        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern uint DragQueryFile(IntPtr hDrop, uint iFile, StringBuilder? lpszFile, uint cch);

        [DllImport("ole32.dll")]
        public static extern void ReleaseStgMedium(ref STGMEDIUM pmedium);
    }
}