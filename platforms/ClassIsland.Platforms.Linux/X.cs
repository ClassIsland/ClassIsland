using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
// ReSharper disable InconsistentNaming

namespace ClassIsland.Platforms.Linux;

public partial class X
{
    private const string X11 = "libX11.so.6";
    
    public const ulong CWOverrideRedirect = (1L<<9);
    const int InputOutput = 1;
    const int InputOnly = 2;

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public static extern IntPtr XOpenDisplay(string displayName);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    public static extern int XCloseDisplay(IntPtr display);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr XDefaultRootWindow(IntPtr display);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr XCreateSimpleWindow(IntPtr display, IntPtr parent, int x, int y, int width, int height, uint borderWidth, uint border, uint background);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    public static extern void XMapWindow(IntPtr display, IntPtr window);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    public static extern void XRaiseWindow(IntPtr display, IntPtr window);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    public static extern void XLowerWindow(IntPtr display, IntPtr window);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    public static extern void XSelectInput(IntPtr display, IntPtr window, XEventMask event_mask);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    public static extern void XChangeWindowAttributes(IntPtr display, IntPtr window, ulong valueMask, ref XSetWindowAttributes attributes);
    
    [DllImport(X11)]
    public static extern int XChangeProperty(
	    IntPtr display, IntPtr w, IntPtr property, IntPtr type,
	    int format, int mode,
	    IntPtr[] data, int nelements);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    public static extern int XGetWindowAttributes(IntPtr display, IntPtr window, ref XWindowAttributes attributes);
    
    [DllImport("libXfixes.so.3")]
    public static extern IntPtr XFixesCreateRegion(IntPtr display, IntPtr rectangles, int nrectangles);

    [DllImport("libXfixes.so.3")]
    public static extern void XFixesSetWindowShapeRegion(IntPtr display, IntPtr window, int shape_type, int x_offset,
        int y_offset, IntPtr region);
    
    [DllImport(X11)]
    public static extern int XSetErrorHandler(nint handler);

    [DllImport(X11)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool XQueryPointer(
        IntPtr display,
        IntPtr window,
        out IntPtr rootReturn,
        out IntPtr childReturn,
        out int rootXReturn,
        out int rootYReturn,
        out int winXReturn,
        out int winYReturn,
        out uint maskReturn);
    
    [DllImport(X11)]
    public static extern int XGetClassHint(IntPtr display, IntPtr window, out XClassHint classHint);

    [DllImport(X11)]
    public static extern int XFree(IntPtr data);

    [DllImport(X11)]
    public static extern int XGetWMName(IntPtr display, IntPtr window, out XTextProperty textProp);

    [DllImport(X11)]
    public static extern int XGetTextProperty(IntPtr display, IntPtr window, out XTextProperty textProp, IntPtr property);

    [DllImport(X11)]
    public static extern IntPtr XInternAtom(IntPtr display, string atomName, bool onlyIfExists);
    
    [DllImport(X11)]
    public static extern int XGetWindowProperty(
        IntPtr display,
        IntPtr window,
        IntPtr property,        // atom
        long long_offset,
        long long_length,
        [MarshalAs(UnmanagedType.Bool)] bool delete,
        IntPtr req_type,        // atom
        out IntPtr actual_type, // returned type
        out int actual_format,  // 8/16/32
        out ulong nitems,
        out ulong bytes_after,
        out nint prop
    );
    
    [LibraryImport(X11)]
    public static unsafe partial int XNextEvent(IntPtr display, out XEvent e);
    
    [DllImport(X11)]
    public static extern int XInitThreads();
    
    [DllImport(X11)]
    public static extern int XClearWindow(nint display, nint window);

    [DllImport(X11)]
    public static extern void XFlush(nint display);

    [StructLayout(LayoutKind.Sequential)]
    public struct XClassHint
    {
        public IntPtr res_name;   // 程序名称（malloc 出来的字符串）
        public IntPtr res_class;  // 资源类名（malloc 出来的字符串）
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct XTextProperty
    {
        public IntPtr value;      // 指向字符串数据
        public IntPtr encoding;   // 原子(atom)类型
        public int format;        // 8/16/32 位
        public ulong nitems;      // 字符数量
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct XSetWindowAttributes
    {
        public nint background_pixmap;
        public ulong background_pixel;
        public nint border_pixmap;
        public ulong border_pixel;
        public int bit_gravity;
        public int win_gravity;
        public int backing_store;
        public ulong backing_planes;
        public ulong backing_pixel;
        public int save_under;
        public long event_mask;
        public long do_not_propagate_mask;
        public int override_redirect;
        public nint colormap;
        public nint cursor;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct XWindowAttributes
    {
        public int x;
        public int y;
        public int width;
        public int height;
        public int border_width;
        public int depth;
        public int visual;
        public IntPtr colormap;
        public int root;
        public int class_window;
        public int colormap_win;
        public int map_state;
        public int all_event_mask;
        public int your_event_mask;
        public int do_not_use;
        public int resize_inc_x;
        public int resize_inc_y;
        public int border_pixel;
        public int background_pixel;
        public IntPtr background_pixmap;
    }
    
    [Flags]
    internal enum XModifierMask
    {
	    ShiftMask = (1 << 0),
	    LockMask = (1 << 1),
	    ControlMask = (1 << 2),
	    Mod1Mask = (1 << 3),
	    Mod2Mask = (1 << 4),
	    Mod3Mask = (1 << 5),
	    Mod4Mask = (1 << 6),
	    Mod5Mask = (1 << 7),
	    Button1Mask = (1 << 8),
	    Button2Mask = (1 << 9),
	    Button3Mask = (1 << 10),
	    Button4Mask = (1 << 11),
	    Button5Mask = (1 << 12),
	    AnyModifier = (1 << 15)

    }

    #region XEvents
    
    internal enum XEventName {
	    KeyPress                = 2,
	    KeyRelease              = 3,
	    ButtonPress             = 4,
	    ButtonRelease           = 5,
	    MotionNotify            = 6,
	    EnterNotify             = 7,
	    LeaveNotify             = 8,
	    FocusIn                 = 9,
	    FocusOut                = 10,
	    KeymapNotify            = 11,
	    Expose                  = 12,
	    GraphicsExpose          = 13,
	    NoExpose                = 14,
	    VisibilityNotify        = 15,
	    CreateNotify            = 16,
	    DestroyNotify           = 17,
	    UnmapNotify             = 18,
	    MapNotify               = 19,
	    MapRequest              = 20,
	    ReparentNotify          = 21,
	    ConfigureNotify         = 22,
	    ConfigureRequest        = 23,
	    GravityNotify           = 24,
	    ResizeRequest           = 25,
	    CirculateNotify         = 26,
	    CirculateRequest        = 27,
	    PropertyNotify          = 28,
	    SelectionClear          = 29,
	    SelectionRequest        = 30,
	    SelectionNotify         = 31,
	    ColormapNotify          = 32,
	    ClientMessage		= 33,
	    MappingNotify		= 34,
	    GenericEvent = 35,
	    LASTEvent
    }

    [StructLayout(LayoutKind.Sequential)]
	internal struct XAnyEvent {
		internal XEventName	type;
		internal IntPtr		serial;
		internal int		send_event;
		internal IntPtr		display;
		internal IntPtr		window;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct XKeyEvent {
		internal XEventName	type;
		internal IntPtr		serial;
		internal int		send_event;
		internal IntPtr		display;
		internal IntPtr		window;
		internal IntPtr		root;
		internal IntPtr		subwindow;
		internal IntPtr		time;
		internal int		x;
		internal int		y;
		internal int		x_root;
		internal int		y_root;
	    internal XModifierMask state;
		internal int		keycode;
		internal int		same_screen;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct XButtonEvent {
		internal XEventName	type;
		internal IntPtr		serial;
		internal int		send_event;
		internal IntPtr		display;
		internal IntPtr		window;
		internal IntPtr		root;
		internal IntPtr		subwindow;
		internal IntPtr		time;
		internal int		x;
		internal int		y;
		internal int		x_root;
		internal int		y_root;
		internal XModifierMask		state;
		internal int		button;
		internal int		same_screen;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct XMotionEvent {
		internal XEventName	type;
		internal IntPtr		serial;
		internal int		send_event;
		internal IntPtr		display;
		internal IntPtr		window;
		internal IntPtr		root;
		internal IntPtr		subwindow;
		internal IntPtr		time;
		internal int		x;
		internal int		y;
		internal int		x_root;
		internal int		y_root;
		internal XModifierMask		state;
		internal byte		is_hint;
		internal int		same_screen;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct XCrossingEvent {
		internal XEventName	type;
		internal IntPtr		serial;
		internal int		send_event;
		internal IntPtr		display;
		internal IntPtr		window;
		internal IntPtr		root;
		internal IntPtr		subwindow;
		internal IntPtr		time;
		internal int		x;
		internal int		y;
		internal int		x_root;
		internal int		y_root;
		internal int	mode;
		internal int	detail;
		internal int		same_screen;
		internal int		focus;
		internal XModifierMask		state;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct XFocusChangeEvent {
		internal XEventName	type;
		internal IntPtr		serial;
		internal int		send_event;
		internal IntPtr		display;
		internal IntPtr		window;
		internal int		mode;
		internal int	detail;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct XKeymapEvent {
		internal XEventName	type;
		internal IntPtr		serial;
		internal int		send_event;
		internal IntPtr		display;
		internal IntPtr		window;
		internal byte		key_vector0;
		internal byte		key_vector1;
		internal byte		key_vector2;
		internal byte		key_vector3;
		internal byte		key_vector4;
		internal byte		key_vector5;
		internal byte		key_vector6;
		internal byte		key_vector7;
		internal byte		key_vector8;
		internal byte		key_vector9;
		internal byte		key_vector10;
		internal byte		key_vector11;
		internal byte		key_vector12;
		internal byte		key_vector13;
		internal byte		key_vector14;
		internal byte		key_vector15;
		internal byte		key_vector16;
		internal byte		key_vector17;
		internal byte		key_vector18;
		internal byte		key_vector19;
		internal byte		key_vector20;
		internal byte		key_vector21;
		internal byte		key_vector22;
		internal byte		key_vector23;
		internal byte		key_vector24;
		internal byte		key_vector25;
		internal byte		key_vector26;
		internal byte		key_vector27;
		internal byte		key_vector28;
		internal byte		key_vector29;
		internal byte		key_vector30;
		internal byte		key_vector31;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct XExposeEvent {
		internal XEventName	type;
		internal IntPtr		serial;
		internal int		send_event;
		internal IntPtr		display;
		internal IntPtr		window;
		internal int		x;
		internal int		y;
		internal int		width;
		internal int		height;
		internal int		count;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct XGraphicsExposeEvent {
		internal XEventName	type;
		internal IntPtr		serial;
		internal int		send_event;
		internal IntPtr		display;
		internal IntPtr		drawable;
		internal int		x;
		internal int		y;
		internal int		width;
		internal int		height;
		internal int		count;
		internal int		major_code;
		internal int		minor_code;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct XNoExposeEvent {
		internal XEventName	type;
		internal IntPtr		serial;
		internal int		send_event;
		internal IntPtr		display;
		internal IntPtr		drawable;
		internal int		major_code;
		internal int		minor_code;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct XVisibilityEvent {
		internal XEventName	type;
		internal IntPtr		serial;
		internal int		send_event;
		internal IntPtr		display;
		internal IntPtr		window;
		internal int		state;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct XCreateWindowEvent {
		internal XEventName	type;
		internal IntPtr		serial;
		internal int		send_event;
		internal IntPtr		display;
		internal IntPtr		parent;
		internal IntPtr		window;
		internal int		x;
		internal int		y;
		internal int		width;
		internal int		height;
		internal int		border_width;
		internal int		override_redirect;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct XDestroyWindowEvent {
		internal XEventName	type;
		internal IntPtr		serial;
		internal int		send_event;
		internal IntPtr		display;
		internal IntPtr		xevent;
		internal IntPtr		window;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct XUnmapEvent {
		internal XEventName	type;
		internal IntPtr		serial;
		internal int		send_event;
		internal IntPtr		display;
		internal IntPtr		xevent;
		internal IntPtr		window;
		internal int		from_configure;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct XMapEvent {
		internal XEventName	type;
		internal IntPtr		serial;
		internal int		send_event;
		internal IntPtr		display;
		internal IntPtr		xevent;
		internal IntPtr		window;
		internal int		override_redirect;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct XMapRequestEvent {
		internal XEventName	type;
		internal IntPtr		serial;
		internal int		send_event;
		internal IntPtr		display;
		internal IntPtr		parent;
		internal IntPtr		window;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct XReparentEvent {
		internal XEventName	type;
		internal IntPtr		serial;
		internal int		send_event;
		internal IntPtr		display;
		internal IntPtr		xevent;
		internal IntPtr		window;
		internal IntPtr		parent;
		internal int		x;
		internal int		y;
		internal int		override_redirect;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct XConfigureEvent {
		internal XEventName	type;
		internal IntPtr		serial;
		internal int		send_event;
		internal IntPtr		display;
		internal IntPtr		xevent;
		internal IntPtr		window;
		internal int		x;
		internal int		y;
		internal int		width;
		internal int		height;
		internal int		border_width;
		internal IntPtr		above;
		internal int		override_redirect;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct XGravityEvent {
		internal XEventName	type;
		internal IntPtr		serial;
		internal int		send_event;
		internal IntPtr		display;
		internal IntPtr		xevent;
		internal IntPtr		window;
		internal int		x;
		internal int		y;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct XResizeRequestEvent {
		internal XEventName	type;
		internal IntPtr		serial;
		internal int		send_event;
		internal IntPtr		display;
		internal IntPtr		window;
		internal int		width;
		internal int		height;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct XConfigureRequestEvent {
		internal XEventName	type;
		internal IntPtr		serial;
		internal int		send_event;
		internal IntPtr		display;
		internal IntPtr		parent;
		internal IntPtr		window;
		internal int		x;
		internal int		y;
		internal int		width;
		internal int		height;
		internal int		border_width;
		internal IntPtr		above;
		internal int		detail;
		internal IntPtr		value_mask;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct XCirculateEvent {
		internal XEventName	type;
		internal IntPtr		serial;
		internal int		send_event;
		internal IntPtr		display;
		internal IntPtr		xevent;
		internal IntPtr		window;
		internal int		place;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct XCirculateRequestEvent {
		internal XEventName	type;
		internal IntPtr		serial;
		internal int		send_event;
		internal IntPtr		display;
		internal IntPtr		parent;
		internal IntPtr		window;
		internal int		place;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct XPropertyEvent {
		internal XEventName	type;
		internal IntPtr		serial;
		internal int		send_event;
		internal IntPtr		display;
		internal IntPtr		window;
		internal IntPtr		atom;
		internal IntPtr		time;
		internal int		state;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct XSelectionClearEvent {
		internal XEventName	type;
		internal IntPtr		serial;
		internal int		send_event;
		internal IntPtr		display;
		internal IntPtr		window;
		internal IntPtr		selection;
		internal IntPtr		time;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct XSelectionRequestEvent {
		internal XEventName	type;
		internal IntPtr		serial;
		internal int		send_event;
		internal IntPtr		display;
		internal IntPtr		owner;
		internal IntPtr		requestor;
		internal IntPtr		selection;
		internal IntPtr		target;
		internal IntPtr		property;
		internal IntPtr		time;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct XSelectionEvent {
		internal XEventName	type;
		internal IntPtr		serial;
		internal int		send_event;
		internal IntPtr		display;
		internal IntPtr		requestor;
		internal IntPtr		selection;
		internal IntPtr		target;
		internal IntPtr		property;
		internal IntPtr		time;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct XColormapEvent {
		internal XEventName	type;
		internal IntPtr		serial;
		internal int		send_event;
		internal IntPtr		display;
		internal IntPtr		window;
		internal IntPtr		colormap;
		internal int		c_new;
		internal int		state;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct XClientMessageEvent {
		internal XEventName	type;
		internal IntPtr		serial;
		internal int		send_event;
		internal IntPtr		display;
		internal IntPtr		window;
		internal IntPtr		message_type;
		internal int		format;
		internal IntPtr		ptr1;
		internal IntPtr		ptr2;
		internal IntPtr		ptr3;
		internal IntPtr		ptr4;
		internal IntPtr		ptr5;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct XMappingEvent {
		internal XEventName	type;
		internal IntPtr		serial;
		internal int		send_event;
		internal IntPtr		display;
		internal IntPtr		window;
		internal int		request;
		internal int		first_keycode;
		internal int		count;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct XErrorEvent {
		internal XEventName	type;
		internal IntPtr		display;
		internal IntPtr		resourceid;
		internal IntPtr		serial;
		internal byte		error_code;
		internal byte	request_code;
		internal byte		minor_code;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct XEventPad {
		internal IntPtr pad0;
		internal IntPtr pad1;
		internal IntPtr pad2;
		internal IntPtr pad3;
		internal IntPtr pad4;
		internal IntPtr pad5;
		internal IntPtr pad6;
		internal IntPtr pad7;
		internal IntPtr pad8;
		internal IntPtr pad9;
		internal IntPtr pad10;
		internal IntPtr pad11;
		internal IntPtr pad12;
		internal IntPtr pad13;
		internal IntPtr pad14;
		internal IntPtr pad15;
		internal IntPtr pad16;
		internal IntPtr pad17;
		internal IntPtr pad18;
		internal IntPtr pad19;
		internal IntPtr pad20;
		internal IntPtr pad21;
		internal IntPtr pad22;
		internal IntPtr pad23;
		internal IntPtr pad24;
		internal IntPtr pad25;
		internal IntPtr pad26;
		internal IntPtr pad27;
		internal IntPtr pad28;
		internal IntPtr pad29;
		internal IntPtr pad30;
		internal IntPtr pad31;
		internal IntPtr pad32;
	}

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct XGenericEventCookie
    {
        internal int type; /* of event. Always GenericEvent */
        internal IntPtr serial; /* # of last request processed */
        internal int send_event; /* true if from SendEvent request */
        internal IntPtr display; /* Display the event was read from */
        internal int extension; /* major opcode of extension that caused the event */
        internal int evtype; /* actual event type. */
        internal uint cookie;
        internal void* data;

        public T GetEvent<T>() where T : unmanaged
        {
            if (data == null)
                throw new InvalidOperationException();
            return Unsafe.ReadUnaligned<T>(data);
        }
    }

    [StructLayout(LayoutKind.Explicit)]
	public struct XEvent {
		[ FieldOffset(0) ] internal XEventName type;
		[ FieldOffset(0) ] internal XAnyEvent AnyEvent;
		[ FieldOffset(0) ] internal XKeyEvent KeyEvent;
		[ FieldOffset(0) ] internal XButtonEvent ButtonEvent;
		[ FieldOffset(0) ] internal XMotionEvent MotionEvent;
		[ FieldOffset(0) ] internal XCrossingEvent CrossingEvent;
		[ FieldOffset(0) ] internal XFocusChangeEvent FocusChangeEvent;
		[ FieldOffset(0) ] internal XExposeEvent ExposeEvent;
		[ FieldOffset(0) ] internal XGraphicsExposeEvent GraphicsExposeEvent;
		[ FieldOffset(0) ] internal XNoExposeEvent NoExposeEvent;
		[ FieldOffset(0) ] internal XVisibilityEvent VisibilityEvent;
		[ FieldOffset(0) ] internal XCreateWindowEvent CreateWindowEvent;
		[ FieldOffset(0) ] internal XDestroyWindowEvent DestroyWindowEvent;
		[ FieldOffset(0) ] internal XUnmapEvent UnmapEvent;
		[ FieldOffset(0) ] internal XMapEvent MapEvent;
		[ FieldOffset(0) ] internal XMapRequestEvent MapRequestEvent;
		[ FieldOffset(0) ] internal XReparentEvent ReparentEvent;
		[ FieldOffset(0) ] internal XConfigureEvent ConfigureEvent;
		[ FieldOffset(0) ] internal XGravityEvent GravityEvent;
		[ FieldOffset(0) ] internal XResizeRequestEvent ResizeRequestEvent;
		[ FieldOffset(0) ] internal XConfigureRequestEvent ConfigureRequestEvent;
		[ FieldOffset(0) ] internal XCirculateEvent CirculateEvent;
		[ FieldOffset(0) ] internal XCirculateRequestEvent CirculateRequestEvent;
		[ FieldOffset(0) ] internal XPropertyEvent PropertyEvent;
		[ FieldOffset(0) ] internal XSelectionClearEvent SelectionClearEvent;
		[ FieldOffset(0) ] internal XSelectionRequestEvent SelectionRequestEvent;
		[ FieldOffset(0) ] internal XSelectionEvent SelectionEvent;
		[ FieldOffset(0) ] internal XColormapEvent ColormapEvent;
		[ FieldOffset(0) ] internal XClientMessageEvent ClientMessageEvent;
		[ FieldOffset(0) ] internal XMappingEvent MappingEvent;
		[ FieldOffset(0) ] internal XErrorEvent ErrorEvent;
		[ FieldOffset(0) ] internal XKeymapEvent KeymapEvent;
		[ FieldOffset(0) ] internal XGenericEventCookie GenericEventCookie;

		//[MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst=24)]
		//[ FieldOffset(0) ] internal int[] pad;
		[ FieldOffset(0) ] internal XEventPad Pad;
	}

    #endregion
    
    [Flags]
    public enum XEventMask : int
    {
	    NoEventMask = 0,
	    KeyPressMask = (1 << 0),
	    KeyReleaseMask = (1 << 1),
	    ButtonPressMask = (1 << 2),
	    ButtonReleaseMask = (1 << 3),
	    EnterWindowMask = (1 << 4),
	    LeaveWindowMask = (1 << 5),
	    PointerMotionMask = (1 << 6),
	    PointerMotionHintMask = (1 << 7),
	    Button1MotionMask = (1 << 8),
	    Button2MotionMask = (1 << 9),
	    Button3MotionMask = (1 << 10),
	    Button4MotionMask = (1 << 11),
	    Button5MotionMask = (1 << 12),
	    ButtonMotionMask = (1 << 13),
	    KeymapStateMask = (1 << 14),
	    ExposureMask = (1 << 15),
	    VisibilityChangeMask = (1 << 16),
	    StructureNotifyMask = (1 << 17),
	    ResizeRedirectMask = (1 << 18),
	    SubstructureNotifyMask = (1 << 19),
	    SubstructureRedirectMask = (1 << 20),
	    FocusChangeMask = (1 << 21),
	    PropertyChangeMask = (1 << 22),
	    ColormapChangeMask = (1 << 23),
	    OwnerGrabButtonMask = (1 << 24)
    }

    public const int ShapeSet = 0;
    public const int ShapeUnion = 1;
    public const int ShapeIntersect = 2;
    public const int ShapeSubtract = 3;
    public const int ShapeInvert = 4;

    public const int ShapeBounding = 0;
    public const int ShapeClip = 1;
    public const int ShapeInput = 2;
}