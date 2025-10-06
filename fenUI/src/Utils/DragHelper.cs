using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using FenUISharp.Logging;
using FenUISharp.Native;

namespace FenUISharp
{
    // Data object implementation for drag operations
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    public class DataObject : IDataObject
    {
        private readonly Dictionary<int, object> _data = new Dictionary<int, object>();

        public void SetData(int format, object data)
        {
            _data[format] = data;
        }

        public int GetData(ref FORMATETC format, out STGMEDIUM medium)
        {
            medium = new STGMEDIUM();

            if (!_data.ContainsKey(format.cfFormat))
                return DV_E_FORMATETC;

            try
            {
                if (format.cfFormat == CF_HDROP && _data[format.cfFormat] is string[] files)
                {
                    // Check if the requested medium type is compatible
                    if ((format.tymed & TYMED.TYMED_HGLOBAL) == 0)
                        return DV_E_TYMED;

                    // Create HDROP structure for file list
                    IntPtr hDrop = CreateHDrop(files);
                    medium.tymed = TYMED.TYMED_HGLOBAL;
                    medium.unionmember = hDrop;
                    medium.pUnkForRelease = IntPtr.Zero;
                    return S_OK;
                }
                else if ((format.cfFormat == CF_UNICODETEXT || format.cfFormat == CF_TEXT) && _data[format.cfFormat] is string text)
                {
                    // Check if the requested medium type is compatible
                    if ((format.tymed & TYMED.TYMED_HGLOBAL) == 0)
                        return DV_E_TYMED;

                    IntPtr hGlobal = CreateTextData(text, format.cfFormat == CF_UNICODETEXT);
                    medium.tymed = TYMED.TYMED_HGLOBAL;
                    medium.unionmember = hGlobal;
                    medium.pUnkForRelease = IntPtr.Zero;
                    return S_OK;
                }
            }
            catch (Exception ex)
            {
                FLogger.Error($"GetData failed: {ex.Message}");
                return E_UNEXPECTED;
            }

            return DV_E_FORMATETC;
        }

        public int GetDataHere(ref FORMATETC format, ref STGMEDIUM medium)
        {
            if (!_data.ContainsKey(format.cfFormat))
                return DV_E_FORMATETC;

            try
            {
                if (format.cfFormat == CF_HDROP && _data[format.cfFormat] is string[] files)
                {
                    // Check if the requested medium type is compatible
                    if ((format.tymed & TYMED.TYMED_HGLOBAL) == 0)
                        return DV_E_TYMED;

                    // Create HDROP structure for file list
                    IntPtr hDrop = CreateHDrop(files);
                    medium.tymed = TYMED.TYMED_HGLOBAL;
                    medium.unionmember = hDrop;
                    medium.pUnkForRelease = IntPtr.Zero;
                    return S_OK;
                }
                else if ((format.cfFormat == CF_UNICODETEXT || format.cfFormat == CF_TEXT) && _data[format.cfFormat] is string text)
                {
                    // Check if the requested medium type is compatible
                    if ((format.tymed & TYMED.TYMED_HGLOBAL) == 0)
                        return DV_E_TYMED;

                    IntPtr hGlobal = CreateTextData(text, format.cfFormat == CF_UNICODETEXT);
                    medium.tymed = TYMED.TYMED_HGLOBAL;
                    medium.unionmember = hGlobal;
                    medium.pUnkForRelease = IntPtr.Zero;
                    return S_OK;
                }
            }
            catch (Exception ex)
            {
                FLogger.Error($"GetData failed: {ex.Message}");
                return E_UNEXPECTED;
            }

            return DV_E_FORMATETC;
        }

        public int QueryGetData(ref FORMATETC format)
        {
            if (_data.ContainsKey(format.cfFormat))
            {
                if (format.cfFormat == CF_HDROP && (format.tymed & TYMED.TYMED_HGLOBAL) != 0)
                    return S_OK;
                if ((format.cfFormat == CF_UNICODETEXT || format.cfFormat == CF_TEXT) && (format.tymed & TYMED.TYMED_HGLOBAL) != 0)
                    return S_OK;
            }
            return DV_E_FORMATETC;
        }

        public int GetCanonicalFormatEtc(ref FORMATETC formatIn, out FORMATETC formatOut)
        {
            formatOut = formatIn;
            return DATA_S_SAMEFORMATETC; 
        }

        public int SetData(ref FORMATETC formatIn, ref STGMEDIUM medium, bool release)
        {
            return E_NOTIMPL;
        }

        public int DAdvise(ref FORMATETC pFormatetc, ADVF advf, IAdviseSink adviseSink, out int connection)
        {
            connection = 0;
            return E_NOTIMPL;
        }

        public int DUnadvise(int connection)
        {
            return E_NOTIMPL;
        }

        public int EnumDAdvise(out IEnumSTATDATA enumAdvise)
        {
            enumAdvise = null;
            return E_NOTIMPL;
        }

        private IntPtr CreateHDrop(string[] files)
        {
            // Calculate total size needed for DROPFILES structure + file paths
            int dropFilesSize = Marshal.SizeOf<DROPFILES>();
            int totalPathSize = 0;

            foreach (string file in files)
            {
                // Each path needs to be null-terminated, and we're using Unicode (2 bytes per char)
                totalPathSize += (file.Length + 1) * 2;
            }

            // Add extra null terminator for the end of the list
            totalPathSize += 2;

            int totalSize = dropFilesSize + totalPathSize;

            // Allocate global memory
            IntPtr hGlobal = GlobalAlloc(GMEM_MOVEABLE | GMEM_ZEROINIT, (UIntPtr)totalSize);
            if (hGlobal == IntPtr.Zero)
                throw new OutOfMemoryException("Failed to allocate global memory for HDROP");

            IntPtr ptr = GlobalLock(hGlobal);
            if (ptr == IntPtr.Zero)
            {
                GlobalFree(hGlobal);
                throw new InvalidOperationException("Failed to lock global memory");
            }

            try
            {
                // Create DROPFILES structure
                DROPFILES dropFiles = new DROPFILES
                {
                    pFiles = (uint)dropFilesSize,
                    pt = new POINT { x = 0, y = 0 },
                    fNC = false,
                    fWide = true
                };

                Marshal.StructureToPtr(dropFiles, ptr, false);

                // Write file paths starting after the DROPFILES structure
                IntPtr filePtr = IntPtr.Add(ptr, dropFilesSize);

                foreach (string file in files)
                {
                    // Convert to absolute path if it isn't already
                    string absolutePath = System.IO.Path.GetFullPath(file);

                    // Write Unicode string with null terminator
                    byte[] fileBytes = Encoding.Unicode.GetBytes(absolutePath + '\0');
                    Marshal.Copy(fileBytes, 0, filePtr, fileBytes.Length);
                    filePtr = IntPtr.Add(filePtr, fileBytes.Length);
                }

                // Add final double null terminator (already zeroed by GMEM_ZEROINIT, but be explicit)
                Marshal.WriteInt16(filePtr, 0);
            }
            finally
            {
                GlobalUnlock(hGlobal);
            }

            return hGlobal;
        }

        private IntPtr CreateTextData(string text, bool unicode)
        {
            byte[] data;
            if (unicode)
            {
                data = Encoding.Unicode.GetBytes(text + '\0');
            }
            else
            {
                data = Encoding.Default.GetBytes(text + '\0');
            }

            IntPtr hGlobal = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)data.Length);
            if (hGlobal == IntPtr.Zero)
                throw new OutOfMemoryException();

            IntPtr ptr = GlobalLock(hGlobal);
            if (ptr == IntPtr.Zero)
            {
                GlobalFree(hGlobal);
                throw new InvalidOperationException("Failed to lock global memory");
            }

            try
            {
                Marshal.Copy(data, 0, ptr, data.Length);
            }
            finally
            {
                GlobalUnlock(hGlobal);
            }

            return hGlobal;
        }

        // Constants
        private const int S_OK = 0;
        private const int DATA_S_SAMEFORMATETC = unchecked((int)0x00040130);
        private const int E_NOTIMPL = unchecked((int)0x80004001);
        private const int E_UNEXPECTED = unchecked((int)0x8000FFFF);
        private const int DV_E_FORMATETC = unchecked((int)0x80040064);
        private const int DV_E_TYMED = unchecked((int)0x80040069);
        private const int CF_HDROP = 15;
        private const int CF_UNICODETEXT = 13;
        private const int CF_TEXT = 1;
        private const uint GMEM_MOVEABLE = 0x0002;
        private const uint GMEM_ZEROINIT = 0x0040;

        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll")]
        private static extern bool GlobalUnlock(IntPtr hMem);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalFree(IntPtr hMem);

        void IDataObject.DUnadvise(int connection)
        {
            throw new NotImplementedException();
        }

        public int EnumFormatEtc(DATADIR direction, out IEnumFORMATETC ppenumFormatEtc)
        {
            if (direction == DATADIR.DATADIR_GET)
            {
                // Only advertise formats we actually support
                var formatList = new List<FORMATETC>();
                
                if (_data.ContainsKey(CF_HDROP))
                {
                    formatList.Add(new FORMATETC 
                    { 
                        cfFormat = (short)CF_HDROP, 
                        dwAspect = DVASPECT.DVASPECT_CONTENT, 
                        lindex = -1, 
                        tymed = TYMED.TYMED_HGLOBAL,
                        ptd = IntPtr.Zero
                    });
                }

                if (_data.ContainsKey(CF_UNICODETEXT))
                {
                    formatList.Add(new FORMATETC 
                    { 
                        cfFormat = (short)CF_UNICODETEXT, 
                        dwAspect = DVASPECT.DVASPECT_CONTENT, 
                        lindex = -1, 
                        tymed = TYMED.TYMED_HGLOBAL,
                        ptd = IntPtr.Zero
                    });
                }

                if (_data.ContainsKey(CF_TEXT))
                {
                    formatList.Add(new FORMATETC 
                    { 
                        cfFormat = (short)CF_TEXT, 
                        dwAspect = DVASPECT.DVASPECT_CONTENT, 
                        lindex = -1, 
                        tymed = TYMED.TYMED_HGLOBAL,
                        ptd = IntPtr.Zero
                    });
                }

                ppenumFormatEtc = new FormatEtcEnumerator(formatList.ToArray());
                return S_OK;
            }

            ppenumFormatEtc = null;
            return E_NOTIMPL;
        }

        void IDataObject.GetData(ref FORMATETC format, out STGMEDIUM medium)
        {
            int hr = GetData(ref format, out medium);
            if (hr != 0)
                Marshal.ThrowExceptionForHR(hr);
        }

        void IDataObject.GetDataHere(ref FORMATETC format, ref STGMEDIUM medium)
        {
            int hr = GetDataHere(ref format, ref medium);
            if (hr != 0)
                Marshal.ThrowExceptionForHR(hr);
        }

        void IDataObject.SetData(ref FORMATETC formatIn, ref STGMEDIUM medium, bool release)
        {
            throw new NotImplementedException();
        }

        public IEnumFORMATETC EnumFormatEtc(DATADIR direction)
        {
            if (direction == DATADIR.DATADIR_GET)
            {
                // Only advertise formats we actually support
                var formatList = new List<FORMATETC>();
                
                if (_data.ContainsKey(CF_HDROP))
                {
                    formatList.Add(new FORMATETC 
                    { 
                        cfFormat = (short)CF_HDROP, 
                        dwAspect = DVASPECT.DVASPECT_CONTENT, 
                        lindex = -1, 
                        tymed = TYMED.TYMED_HGLOBAL,
                        ptd = IntPtr.Zero
                    });
                }

                if (_data.ContainsKey(CF_UNICODETEXT))
                {
                    formatList.Add(new FORMATETC 
                    { 
                        cfFormat = (short)CF_UNICODETEXT, 
                        dwAspect = DVASPECT.DVASPECT_CONTENT, 
                        lindex = -1, 
                        tymed = TYMED.TYMED_HGLOBAL,
                        ptd = IntPtr.Zero
                    });
                }

                if (_data.ContainsKey(CF_TEXT))
                {
                    formatList.Add(new FORMATETC 
                    { 
                        cfFormat = (short)CF_TEXT, 
                        dwAspect = DVASPECT.DVASPECT_CONTENT, 
                        lindex = -1, 
                        tymed = TYMED.TYMED_HGLOBAL,
                        ptd = IntPtr.Zero
                    });
                }

                return new FormatEtcEnumerator(formatList.ToArray());
            }

            return null!;
        }
    }

    // Drop source implementation
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    public class DropSource : IDropSource
    {
        public int QueryContinueDrag(bool fEscapePressed, uint grfKeyState)
        {
            // Check if escape was pressed
            if (fEscapePressed)
                return DRAGDROP_S_CANCEL;

            // Check if mouse button is still pressed
            bool leftButtonDown = (grfKeyState & MK_LBUTTON) != 0;
            bool rightButtonDown = (grfKeyState & MK_RBUTTON) != 0;

            if (!leftButtonDown && !rightButtonDown)
                return DRAGDROP_S_DROP;

            return S_OK;
        }

        public int GiveFeedback(uint dwEffect)
        {
            // Let Windows handle the cursor feedback
            return DRAGDROP_S_USEDEFAULTCURSORS;
        }

        // Constants
        private const int S_OK = 0;
        private const int DRAGDROP_S_DROP = 0x00040100;
        private const int DRAGDROP_S_CANCEL = 0x00040101;
        private const int DRAGDROP_S_USEDEFAULTCURSORS = 0x00040102;
        private const uint MK_LBUTTON = 0x0001;
        private const uint MK_RBUTTON = 0x0002;
    }

    public static class DragSourceHelper
    {
        /// <summary>
        /// Initiates a drag operation with the specified file paths
        /// This should be called in response to a mouse drag gesture (typically on MouseMove after MouseDown)
        /// </summary>
        /// <param name="filePaths">Array of file paths to drag</param>
        /// <param name="allowedEffects">Allowed drop effects (Copy, Move, Link)</param>
        /// <param name="onMoveComplete">Callback for when move operation should be performed (only called for MOVE effect)</param>
        /// <returns>The actual drop effect that occurred</returns>
        public static DROPEFFECT DragFiles(string[] filePaths, DROPEFFECT allowedEffects = DROPEFFECT.Copy | DROPEFFECT.Move | DROPEFFECT.Link, Action<string[]> onMoveComplete = null)
        {
            if (filePaths == null || filePaths.Length == 0)
                throw new ArgumentException("File paths cannot be null or empty");

            // Check if we're on an STA thread
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {
                throw new InvalidOperationException("Drag operations must be performed on an STA thread");
            }

            // Convert to absolute paths and validate
            string[] absolutePaths = new string[filePaths.Length];
            for (int i = 0; i < filePaths.Length; i++)
            {
                absolutePaths[i] = System.IO.Path.GetFullPath(filePaths[i]);
                if (!System.IO.File.Exists(absolutePaths[i]) && !System.IO.Directory.Exists(absolutePaths[i]))
                {
                    FLogger.Log($"Warning: Path does not exist: {absolutePaths[i]}");
                }
            }

            try
            {
                // Create data object and set file data
                var dataObject = new DataObject();
                dataObject.SetData(CF_HDROP, absolutePaths);

                // Create drop source
                var dropSource = new DropSource();

                FLogger.Log($"Starting drag operation with {absolutePaths.Length} files");
                FLogger.Log($"Allowed effects: {allowedEffects}");
                foreach (string path in absolutePaths)
                {
                    FLogger.Log($"  - {path}");
                }

                // Perform drag operation - this will block until drag is complete
                uint effect = 0;
                int result = DoDragDrop(dataObject, dropSource, (uint)allowedEffects, ref effect);

                FLogger.Log($"DoDragDrop returned: 0x{result:X8}, effect: {(DROPEFFECT)effect}");

                // Check result and handle move operation if needed
                if (result == DRAGDROP_S_DROP)
                {
                    DROPEFFECT actualEffect = (DROPEFFECT)effect;

                    // If it was a move operation, the source needs to delete the original files
                    if (actualEffect == DROPEFFECT.Move && onMoveComplete != null)
                    {
                        try
                        {
                            onMoveComplete(absolutePaths);
                        }
                        catch (Exception ex)
                        {
                            FLogger.Log($"Move completion failed: {ex.Message}");
                        }
                    }

                    return actualEffect;
                }
                else if (result == DRAGDROP_S_CANCEL)
                {
                    return DROPEFFECT.None;
                }
                else
                {
                    // Some error occurred
                    FLogger.Log($"DoDragDrop failed with HRESULT: 0x{result:X8}");
                    return DROPEFFECT.None;
                }
            }
            catch (Exception ex)
            {
                FLogger.Log($"Drag operation failed: {ex.Message}");
                return DROPEFFECT.None;
            }
        }

        /// <summary>
        /// Checks if a drag operation should be started based on mouse movement
        /// Call this in MouseMove events after MouseDown
        /// </summary>
        /// <param name="startPoint">Initial mouse down point</param>
        /// <param name="currentPoint">Current mouse position</param>
        /// <returns>True if drag should be initiated</returns>
        public static bool ShouldStartDrag(POINT startPoint, POINT currentPoint)
        {
            int dragWidth = GetSystemMetrics(SM_CXDRAG);
            int dragHeight = GetSystemMetrics(SM_CYDRAG);

            return Math.Abs(currentPoint.x - startPoint.x) >= dragWidth ||
                   Math.Abs(currentPoint.y - startPoint.y) >= dragHeight;
        }

        /// <summary>
        /// Initiates a drag operation with text data
        /// </summary>
        /// <param name="text">Text to drag</param>
        /// <param name="allowedEffects">Allowed drop effects</param>
        /// <returns>The actual drop effect that occurred</returns>
        public static DROPEFFECT DragText(string text, DROPEFFECT allowedEffects = DROPEFFECT.Copy)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentException("Text cannot be null or empty");

            // Check if we're on an STA thread
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {
                throw new InvalidOperationException("Drag operations must be performed on an STA thread");
            }

            // Create data object and set text data
            var dataObject = new DataObject();
            dataObject.SetData(CF_UNICODETEXT, text);
            dataObject.SetData(CF_TEXT, text);

            // Create drop source
            var dropSource = new DropSource();

            // Perform drag operation
            uint effect = 0;
            int result = DoDragDrop(dataObject, dropSource, (uint)allowedEffects, ref effect);

            return (DROPEFFECT)effect;
        }

        // Constants
        private const int CF_HDROP = 15;
        private const int CF_UNICODETEXT = 13;
        private const int CF_TEXT = 1;
        private const int DRAGDROP_S_DROP = 0x00040100;
        private const int DRAGDROP_S_CANCEL = 0x00040101;
        private const int SM_CXDRAG = 68;
        private const int SM_CYDRAG = 69;

        [DllImport("ole32.dll")]
        private static extern int DoDragDrop(
            [MarshalAs(UnmanagedType.Interface)] IDataObject pDataObj,
            [MarshalAs(UnmanagedType.Interface)] IDropSource pDropSource,
            uint dwOKEffects,
            ref uint pdwEffect);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);
    }

    // Supporting structures
    [StructLayout(LayoutKind.Sequential)]
    public struct DROPFILES
    {
        public uint pFiles;
        public POINT pt;
        [MarshalAs(UnmanagedType.Bool)]
        public bool fNC;
        [MarshalAs(UnmanagedType.Bool)]
        public bool fWide;
    }

    public class FormatEtcEnumerator : IEnumFORMATETC
    {
        private readonly FORMATETC[] _formats;
        private int _index = 0;

        public FormatEtcEnumerator(FORMATETC[] formats)
        {
            _formats = formats;
        }

        public int Next(int celt, FORMATETC[] rgelt, int[] pceltFetched)
        {
            int fetched = 0;
            while (_index < _formats.Length && fetched < celt)
            {
                rgelt[fetched] = _formats[_index];
                fetched++;
                _index++;
            }

            if (pceltFetched != null && pceltFetched.Length > 0)
                pceltFetched[0] = fetched;

            return (fetched == celt) ? 0 : 1; // S_OK or S_FALSE
        }

        public int Skip(int celt)
        {
            _index += celt;
            return (_index <= _formats.Length) ? 0 : 1;
        }

        public void Reset()
        {
            _index = 0;
        }

        public void Clone(out IEnumFORMATETC newEnum)
        {
            newEnum = new FormatEtcEnumerator(_formats) { _index = _index };
        }

        int IEnumFORMATETC.Reset()
        {
            Reset();
            return 0;
        }
    }
}