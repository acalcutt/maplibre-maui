cmake_minimum_required(VERSION 3.15 FATAL_ERROR)

project(mbgl-cabi LANGUAGES CXX C)
set(CMAKE_CXX_STANDARD 20)

# ── maplibre-native dependency ────────────────────────────────────────────────
# maplibre-native is expected as a submodule at ../dependencies/maplibre-native
# or the path can be overridden via -DMAPLIBRE_NATIVE_DIR=...
if(NOT DEFINED MAPLIBRE_NATIVE_DIR)
    set(MAPLIBRE_NATIVE_DIR "${CMAKE_CURRENT_LIST_DIR}/../dependencies/maplibre-native"
        CACHE PATH "Path to maplibre-native source tree")
endif()

# Use the platform-specific toolchain if available (Windows vcpkg)
if(WIN32 AND EXISTS "${MAPLIBRE_NATIVE_DIR}/platform/windows/custom-toolchain.cmake")
    set(CMAKE_TOOLCHAIN_FILE "${MAPLIBRE_NATIVE_DIR}/platform/windows/custom-toolchain.cmake"
        CACHE STRING "CMake toolchain file")
endif()

# ── Shared (Windows/Android/Linux/macOS) or Static (iOS/tvOS/macCatalyst) ────
# Pass -DMBGL_STATIC=ON explicitly to force a static build (required for
# macCatalyst where the lib is statically linked via __Internal P/Invoke).
option(MBGL_STATIC "Build mbgl-cabi as a static library" OFF)
if(MBGL_STATIC OR CMAKE_SYSTEM_NAME STREQUAL "iOS" OR CMAKE_SYSTEM_NAME STREQUAL "tvOS")
    add_library(mbgl-cabi STATIC)
else()
    add_library(mbgl-cabi SHARED)
endif()

target_compile_definitions(mbgl-cabi PRIVATE
    MBGL_CABI_EXPORT
    _USE_MATH_DEFINES
    NOMINMAX
)

target_include_directories(mbgl-cabi PRIVATE
    "${CMAKE_CURRENT_LIST_DIR}/include"
    "${CMAKE_CURRENT_LIST_DIR}/src"
)

target_sources(mbgl-cabi PRIVATE
    src/mbgl_cabi.cpp
)

# ── Platform-specific frontend ────────────────────────────────────────────────
if(WIN32)
    target_sources(mbgl-cabi PRIVATE src/platform_frontend_windows.cpp)
    target_compile_options(mbgl-cabi PRIVATE /wd4267)
elseif(ANDROID)
    target_sources(mbgl-cabi PRIVATE src/platform_frontend_android.cpp)
    target_link_libraries(mbgl-cabi PRIVATE EGL android log)
elseif(APPLE)
    target_sources(mbgl-cabi PRIVATE src/platform_frontend_apple.cpp)
    find_library(METAL_LIB Metal REQUIRED)
    find_library(QUARTZCORE_LIB QuartzCore REQUIRED)
    target_link_libraries(mbgl-cabi PRIVATE ${METAL_LIB} ${QUARTZCORE_LIB})
else()
    message(FATAL_ERROR "Unsupported platform")
endif()

# ── Link maplibre-native ──────────────────────────────────────────────────────
# maplibre-native must be configured separately; we consume it via its cmake targets.
# Preferred: add_subdirectory(maplibre-native) before this or use find_package.
if(TARGET mbgl-core)
    target_link_libraries(mbgl-cabi PRIVATE mbgl-core)
else()
    message(WARNING "mbgl-core target not found — link maplibre-native before this project")
endif()

# ── Install / output ──────────────────────────────────────────────────────────
set_target_properties(mbgl-cabi PROPERTIES
    OUTPUT_NAME "mbgl-cabi"
    POSITION_INDEPENDENT_CODE ON
)

install(TARGETS mbgl-cabi DESTINATION lib)
install(FILES include/mbgl_cabi.h DESTINATION include)
