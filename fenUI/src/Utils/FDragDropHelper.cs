using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace FenUISharp
{

    [ComImport, Guid("00000122-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IDropTarget
    {
        void DragEnter([In] IntPtr pDataObj, [In] uint grfKeyState, [In] Win32Helper.POINT pt, [In, Out] ref uint pdwEffect);
        void DragOver([In] uint grfKeyState, [In] Win32Helper.POINT pt, [In, Out] ref uint pdwEffect);
        void DragLeave();
        void Drop([In] IntPtr pDataObj, [In] uint grfKeyState, [In] Win32Helper.POINT pt, [In, Out] ref uint pdwEffect);
    }

    // Define drop effects (for simplicity, we're just using Copy here)
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
    public class FDropTarget : IDropTarget
    {
        public void DragEnter([In] nint pDataObj, [In] uint grfKeyState, [In] Win32Helper.POINT pt, [In, Out] ref uint pdwEffect)
        {
            Console.WriteLine("File Drop Enter");
            pdwEffect = (uint)DROPEFFECT.Copy;

            FWindow.instance.onFileWantDrop?.Invoke();

            throw new Exception(); // For some reason it only works when it throws this
        }

        void IDropTarget.DragOver(uint grfKeyState, Win32Helper.POINT pt, ref uint pdwEffect)
        {
            Console.WriteLine("File Drop Over");
            pdwEffect = (uint)DROPEFFECT.Copy;

            throw new NotImplementedException();
        }

        void IDropTarget.DragLeave()
        {
            Console.WriteLine("File Drop Leave");

            throw new NotImplementedException();
        }

        // Doesn't work
        public void Drop([In] nint pDataObj, [In] uint grfKeyState, [In] Win32Helper.POINT pt, [In, Out] ref uint pdwEffect)
        {
            throw new NotImplementedException();
        }
    }

    public static class DragDropRegistration
    {
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
}