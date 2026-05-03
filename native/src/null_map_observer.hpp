/**
 * null_map_observer.hpp — A no-op MapObserver shared by all platform frontends.
 */
#pragma once
#include <mbgl/map/map_observer.hpp>

class NullMapObserver : public mbgl::MapObserver {
public:
    void onCameraWillChange(mbgl::MapObserver::CameraChangeMode) override {}
    void onCameraIsChanging() override {}
    void onCameraDidChange(mbgl::MapObserver::CameraChangeMode) override {}
    void onWillStartLoadingMap() override {}
    void onDidFinishLoadingMap() override {}
    void onDidFailLoadingMap(mbgl::MapLoadError, const std::string&) override {}
    void onWillStartRenderingFrame() override {}
    void onDidFinishRenderingFrame(const mbgl::MapObserver::RenderFrameStatus&) override {}
    void onWillStartRenderingMap() override {}
    void onDidFinishRenderingMap(mbgl::MapObserver::RenderMode) override {}
    void onDidFinishLoadingStyle() override {}
    void onSourceChanged(mbgl::style::Source&) override {}
    void onDidBecomeIdle() override {}
    void onStyleImageMissing(const std::string&) override {}
};
