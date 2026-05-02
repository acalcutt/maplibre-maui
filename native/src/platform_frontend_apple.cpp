/**
 * platform_frontend_apple.cpp — Metal frontend for iOS / macOS (Catalyst).
 *
 * surface_handle: CAMetalLayer* (bridged as void*)
 * gl_context:     unused (pass nullptr)
 *
 * Metal rendering is managed by mbgl's built-in Metal backend.
 * We simply create the Metal renderer backend with the provided layer.
 */
#include "platform_frontend.hpp"
#include "null_map_observer.hpp"
#include <mbgl/mtl/renderer_backend.hpp>
#include <mbgl/mtl/renderable_resource.hpp>
#include <mbgl/renderer/renderer.hpp>
#include <mbgl/renderer/update_parameters.hpp>
#include <mbgl/gfx/backend_scope.hpp>
#import  <QuartzCore/CAMetalLayer.h>
#import  <Metal/Metal.h>
#include <memory>
#include <mutex>

/* ── Metal renderable resource ──────────────────────────────────────── */
class MetalRenderableResource : public mbgl::mtl::RenderableResource {
public:
    MetalRenderableResource(class MetalBackend& b) : _backend(b) {}
    void bind() override;
private:
    class MetalBackend& _backend;
};

/* ── Metal backend ───────────────────────────────────────────────────── */
class MetalBackend : public mbgl::mtl::RendererBackend,
                     public mbgl::gfx::Renderable {
public:
    MetalBackend(CAMetalLayer* layer, mbgl::Size sz)
        : mbgl::gfx::Renderable(sz, std::make_unique<MetalRenderableResource>(*this))
        , mbgl::mtl::RendererBackend(mbgl::gfx::ContextMode::Unique)
        , _layer(layer)
    {}

    void setSize(mbgl::Size sz) { this->size = sz; }
    mbgl::gfx::Renderable& getDefaultRenderable() override { return *this; }
    CAMetalLayer* getLayer() const { return _layer; }

protected:
    std::vector<const char*> getInstanceExtensions() const override { return {}; }
    std::vector<const char*> getDeviceExtensions()   const override { return {}; }

private:
    CAMetalLayer* _layer;
};

void MetalRenderableResource::bind() {}

/* ── Metal frontend ──────────────────────────────────────────────────── */
class MetalFrontend : public PlatformFrontend {
public:
    MetalFrontend(CAMetalLayer* layer, mbgl::Size sz, float pixelRatio,
                  mbgl_render_fn renderCb, void* renderUd)
        : _backend(layer, sz)
        , _renderer(std::make_unique<mbgl::Renderer>(_backend, pixelRatio))
        , _renderCb(renderCb), _renderUd(renderUd)
    {}

    ~MetalFrontend() override {
        mbgl::BackendScope guard(_backend, mbgl::BackendScope::ScopeType::Implicit);
        _renderer.reset();
    }

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

    void setSize(mbgl::Size sz) override { _backend.setSize(sz); }
    mbgl::Size getSize() const override { return _backend.getSize(); }
    mbgl::MapObserver& getObserver() override { return _nullObserver; }

private:
    MetalBackend                            _backend;
    std::unique_ptr<mbgl::Renderer>         _renderer;
    mbgl_render_fn                          _renderCb;
    void*                                   _renderUd;
    std::shared_ptr<mbgl::UpdateParameters> _updateParams;
    std::mutex                              _mutex;
    NullMapObserver                         _nullObserver;
};

PlatformFrontend* createPlatformFrontend(
    void* surface_handle, void* /*gl_context*/,
    mbgl::Size sz, float pixelRatio,
    mbgl_render_fn renderCb, void* renderUd)
{
    CAMetalLayer* layer = (__bridge CAMetalLayer*)surface_handle;
    return new MetalFrontend(layer, sz, pixelRatio, renderCb, renderUd);
}
