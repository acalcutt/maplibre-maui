/**
 * platform_frontend_windows.cpp — WGL OpenGL frontend for Windows.
 *
 * Expects the caller (C# MaplibreMapHost) to:
 *   1. Create a Win32 child HWND with CS_OWNDC | CS_DBLCLKS
 *   2. Create a WGL context on that DC
 *   3. Pass the HDC and HGLRC as void* to mbgl_frontend_create_gl()
 *
 * The render_callback is invoked after each frame so the caller can
 * call SwapBuffers on its own DC.
 */
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include "platform_frontend.hpp"
#include <mbgl/gl/renderable_resource.hpp>
#include <mbgl/gl/renderer_backend.hpp>
#include <mbgl/renderer/renderer.hpp>
#include <mbgl/renderer/update_parameters.hpp>
#include <mbgl/gfx/backend_scope.hpp>
#include <memory>
#include <mutex>

#include "null_map_observer.hpp"

/* ── Renderable resource ────────────────────────────────────────────── */
class WGLRenderableResource : public mbgl::gl::RenderableResource {
public:
    WGLRenderableResource(class WGLBackend& backend) : _backend(backend) {}
    void bind() override;
private:
    class WGLBackend& _backend;
};

/* ── WGL backend ────────────────────────────────────────────────────── */
class WGLBackend : public mbgl::gl::RendererBackend,
                   public mbgl::gfx::Renderable {
public:
    WGLBackend(HDC hDC, HGLRC hGLRC, mbgl::Size sz)
        : mbgl::gfx::Renderable(sz, std::make_unique<WGLRenderableResource>(*this))
        , mbgl::gl::RendererBackend(mbgl::gfx::ContextMode::Unique)
        , _hDC(hDC), _hGLRC(hGLRC)
    {}

    mbgl::gfx::Renderable& getDefaultRenderable() override { return *this; }
    void setSize(mbgl::Size sz) { this->size = sz; }

protected:
    void activate()   override { wglMakeCurrent(_hDC, _hGLRC); }
    void deactivate() override { wglMakeCurrent(nullptr, nullptr); }
    mbgl::gl::ProcAddress getExtensionFunctionPointer(const char* name) override {
        return reinterpret_cast<mbgl::gl::ProcAddress>(wglGetProcAddress(name));
    }
    std::vector<const char*> getInstanceExtensions() const override { return {}; }
    std::vector<const char*> getDeviceExtensions()   const override { return {}; }

private:
    HDC   _hDC;
    HGLRC _hGLRC;
};

void WGLRenderableResource::bind() {
    _backend.setFramebufferBinding(0);
    _backend.setViewport(0, 0, _backend.getSize());
}

/* ── WGL frontend ───────────────────────────────────────────────────── */
class WGLFrontend : public PlatformFrontend {
public:
    WGLFrontend(HDC hDC, HGLRC hGLRC, mbgl::Size sz, float pixelRatio,
                mbgl_render_fn renderCb, void* renderUd)
        : _backend(hDC, hGLRC, sz)
        , _renderer(std::make_unique<mbgl::Renderer>(_backend, pixelRatio))
        , _renderCb(renderCb), _renderUd(renderUd)
    {}

    ~WGLFrontend() override {
        mbgl::BackendScope guard(_backend, mbgl::BackendScope::ScopeType::Implicit);
        _renderer.reset();
    }

    /* RendererFrontend */
    void reset() override { _renderer.reset(); }

    void setObserver(mbgl::RendererObserver& obs) override {
        _renderer->setObserver(&obs);
    }

    void update(std::shared_ptr<mbgl::UpdateParameters> params) override {
        {
            std::unique_lock<std::mutex> lock(_mutex);
            _updateParams = std::move(params);
        }
        if (_renderCb) _renderCb(_renderUd);
    }

    /* PlatformFrontend */
    void render() override {
        std::shared_ptr<mbgl::UpdateParameters> params;
        {
            std::unique_lock<std::mutex> lock(_mutex);
            params = std::move(_updateParams);
        }
        if (!params) return;
        mbgl::BackendScope guard(_backend, mbgl::BackendScope::ScopeType::Implicit);
        _renderer->render(*params);
    }

    void setSize(mbgl::Size sz) override {
        _backend.setSize(sz);
    }

    mbgl::Size getSize() const override { return _backend.getSize(); }

    mbgl::MapObserver& getObserver() override { return _nullObserver; }
    mbgl::Renderer* getRenderer() override { return _renderer.get(); }

private:
    WGLBackend                                _backend;
    std::unique_ptr<mbgl::Renderer>           _renderer;
    mbgl_render_fn                            _renderCb;
    void*                                     _renderUd;
    std::shared_ptr<mbgl::UpdateParameters>   _updateParams;
    std::mutex                                _mutex;
    NullMapObserver                           _nullObserver;
};

/* ── Factory (called by mbgl_cabi.cpp) ─────────────────────────────── */
PlatformFrontend* createPlatformFrontend(
    void* surface_handle, void* gl_context,
    mbgl::Size sz, float pixelRatio,
    mbgl_render_fn renderCb, void* renderUd)
{
    return new WGLFrontend(
        reinterpret_cast<HDC>(surface_handle),
        reinterpret_cast<HGLRC>(gl_context),
        sz, pixelRatio, renderCb, renderUd
    );
}
