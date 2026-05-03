/**
 * MbglFrontend.cs — Typed wrapper around mbgl_frontend_t.
 */
using System.Runtime.InteropServices;

namespace Maui.MapLibre.Native;

/// <summary>Wraps <c>mbgl_frontend_t*</c>. Dispose after the <see cref="MbglMap"/>.</summary>
public sealed class MbglFrontend : IDisposable
{
    internal IntPtr Handle { get; private set; }

    // Prevent the delegate from being collected
    private readonly NativeMethods.RenderFn _renderDelegate;
    private readonly Action _renderCallback;

    /// <param name="surfaceHandle">Platform-specific surface: HDC (Windows), ANativeWindow* (Android), CAMetalLayer* (Apple)</param>
    /// <param name="glContext">WGL context (Windows) or null (Android/Apple)</param>
    /// <param name="widthPx">Initial width in physical pixels</param>
    /// <param name="heightPx">Initial height in physical pixels</param>
    /// <param name="pixelRatio">Device pixel ratio</param>
    /// <param name="onRender">Called by the native layer when a new frame is ready; call <see cref="Render"/> inside it.</param>
    public MbglFrontend(
        IntPtr surfaceHandle,
        IntPtr glContext,
        int    widthPx,
        int    heightPx,
        float  pixelRatio,
        Action onRender)
    {
        _renderCallback = onRender;
        _renderDelegate = _ => _renderCallback();

        Handle = NativeMethods.FrontendCreateGl(
            surfaceHandle, glContext,
            widthPx, heightPx, pixelRatio,
            _renderDelegate, IntPtr.Zero);

        if (Handle == IntPtr.Zero)
            throw new InvalidOperationException("mbgl_frontend_create_gl returned null.");
    }

    /// <summary>
    /// Execute the pending render pass. Call from the render thread when
    /// <see cref="onRender"/> fires.
    /// </summary>
    public void Render() => NativeMethods.FrontendRender(Handle);

    public void SetSize(int widthPx, int heightPx)
        => NativeMethods.FrontendSetSize(Handle, widthPx, heightPx);

    /// <summary>
    /// Returns the platform-native view created by the frontend, or <see cref="IntPtr.Zero"/>.
    /// On Apple platforms this is the <c>MTKView*</c>; cast to <c>UIView</c> and add as subview.
    /// </summary>
    public IntPtr GetNativeView() => NativeMethods.FrontendGetNativeView(Handle);

    public void Dispose()
    {
        if (Handle != IntPtr.Zero)
        {
            NativeMethods.FrontendDestroy(Handle);
            Handle = IntPtr.Zero;
        }
    }
}
