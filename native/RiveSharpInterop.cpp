#include "rive/animation/state_machine_input_instance.hpp"
#include "rive/factory.hpp"
#include "rive/file.hpp"
#include "rive/animation/animation.hpp"
#include "rive/animation/linear_animation_instance.hpp"
#include "rive/animation/linear_animation.hpp"
#include "rive/animation/state_machine_instance.hpp"
#include "rive/artboard.hpp"
#include "rive/renderer.hpp"

using namespace rive;

#if 0
// Message box utility since we don't have a console in UWP for printfs.
#include <windows.h>
#include <stdarg.h>
static void msgbox(const char* format, ...) {
    std::vector<char> msg;
    va_list args, args_copy;

    va_start(args, format);
    va_copy(args_copy, args);

    int len = vsnprintf(nullptr, 0, format, args);
    if (len > 0) {
        msg.resize(len + 1);
        vsnprintf(msg.data(), msg.size(), format, args_copy);
        MessageBoxA(NULL, msg.data(), "This is a cool message from Rive!", 0);
    }

    va_end(args_copy);
    va_end(args);
}
#endif

#define RIVE_DLL(RET) extern "C" __declspec(dllexport) RET __cdecl

////////////////////////////////////////////////////////////////////////////////////////////////////

RIVE_DLL(void) Mat2D_Multiply(const Mat2D& a, const Mat2D& b, Mat2D& out) { out = a * b; }
RIVE_DLL(void) Mat2D_MultiplyVec2D(const Mat2D& a, const Vec2D& b, Vec2D& out) { out = a * b; }
RIVE_DLL(bool) Mat2D_Invert(const Mat2D& a, Mat2D& out) { return a.invert(&out); }

////////////////////////////////////////////////////////////////////////////////////////////////////

class RenderPathSharp : public RenderPath {
public:
    using FreeDelegate = void (*)(intptr_t ptr);
    using ResetDelegate = void (*)(intptr_t ptr);
    using AddRenderPathDelegate = void (*)(intptr_t ptr, intptr_t path, const Mat2D&);
    using FillRuleDelegate = void (*)(intptr_t ptr, int rule);
    using MoveToDelegate = void (*)(intptr_t ptr, float x, float y);
    using LineToDelegate = void (*)(intptr_t ptr, float x, float y);
    using CubicToDelegate =
        void (*)(intptr_t ptr, float ox, float oy, float ix, float iy, float x, float y);
    using CloseDelegate = void (*)(intptr_t ptr);

    static FreeDelegate ManagedFree;
    static ResetDelegate ManagedReset;
    static AddRenderPathDelegate ManagedAddRenderPath;
    static FillRuleDelegate ManagedFillRule;
    static MoveToDelegate ManagedMoveTo;
    static LineToDelegate ManagedLineTo;
    static CubicToDelegate ManagedCubicTo;
    static CloseDelegate ManagedClose;

    RenderPathSharp(intptr_t managedPtr) : m_ManagedPtr(managedPtr) {}
    RenderPathSharp(const RenderPathSharp&) = delete;
    RenderPathSharp& operator=(const RenderPathSharp&) = delete;
    ~RenderPathSharp() { ManagedFree(m_ManagedPtr); };

    void reset() override { ManagedReset(m_ManagedPtr); }
    void fillRule(FillRule value) override { ManagedFillRule(m_ManagedPtr, (int)value); }
    void addRenderPath(RenderPath* path, const Mat2D& m) override {
        ManagedAddRenderPath(m_ManagedPtr, static_cast<RenderPathSharp*>(path)->m_ManagedPtr, m);
    }
    void moveTo(float x, float y) override { ManagedMoveTo(m_ManagedPtr, x, y); }
    void lineTo(float x, float y) override { ManagedLineTo(m_ManagedPtr, x, y); }
    void cubicTo(float ox, float oy, float ix, float iy, float x, float y) override {
        ManagedCubicTo(m_ManagedPtr, ox, oy, ix, iy, x, y);
    }
    void close() override { ManagedClose(m_ManagedPtr); }

    const intptr_t m_ManagedPtr;
};

RenderPathSharp::FreeDelegate RenderPathSharp::ManagedFree;
RenderPathSharp::ResetDelegate RenderPathSharp::ManagedReset;
RenderPathSharp::AddRenderPathDelegate RenderPathSharp::ManagedAddRenderPath;
RenderPathSharp::FillRuleDelegate RenderPathSharp::ManagedFillRule;
RenderPathSharp::MoveToDelegate RenderPathSharp::ManagedMoveTo;
RenderPathSharp::LineToDelegate RenderPathSharp::ManagedLineTo;
RenderPathSharp::CubicToDelegate RenderPathSharp::ManagedCubicTo;
RenderPathSharp::CloseDelegate RenderPathSharp::ManagedClose;

RIVE_DLL(void)
RenderPath_NativeInitDelegates(RenderPathSharp::FreeDelegate managedFree,
                               RenderPathSharp::ResetDelegate managedReset,
                               RenderPathSharp::AddRenderPathDelegate managedAddRenderPath,
                               RenderPathSharp::FillRuleDelegate managedFillRule,
                               RenderPathSharp::MoveToDelegate managedMoveTo,
                               RenderPathSharp::LineToDelegate managedLineTo,
                               RenderPathSharp::CubicToDelegate managedCubicTo,
                               RenderPathSharp::CloseDelegate managedClose) {
    RenderPathSharp::ManagedFree = managedFree;
    RenderPathSharp::ManagedReset = managedReset;
    RenderPathSharp::ManagedAddRenderPath = managedAddRenderPath;
    RenderPathSharp::ManagedFillRule = managedFillRule;
    RenderPathSharp::ManagedMoveTo = managedMoveTo;
    RenderPathSharp::ManagedLineTo = managedLineTo;
    RenderPathSharp::ManagedCubicTo = managedCubicTo;
    RenderPathSharp::ManagedClose = managedClose;
}

////////////////////////////////////////////////////////////////////////////////////////////////////

class RenderImageSharp : public RenderImage {
public:
    using FreeDelegate = void (*)(intptr_t);
    using WidthHeightDelegate = int (*)(intptr_t);

    static FreeDelegate ManagedFree;
    static WidthHeightDelegate ManagedWidth;
    static WidthHeightDelegate ManagedHeight;

    RenderImageSharp(intptr_t managedPtr) : m_ManagedPtr(managedPtr) {
        m_Width = ManagedWidth(m_ManagedPtr);
        m_Height = ManagedHeight(m_ManagedPtr);
    }
    RenderImageSharp(const RenderImageSharp&) = delete;
    RenderImageSharp& operator=(const RenderImageSharp&) = delete;
    ~RenderImageSharp() { ManagedFree(m_ManagedPtr); };

    rcp<RenderShader> makeShader(RenderTileMode tx,
                                 RenderTileMode ty,
                                 const Mat2D* localMatrix = nullptr) const override {
        assert(!"Not yet implemented. Is this method used?");
        return nullptr;
    }

    const intptr_t m_ManagedPtr;
};

RenderImageSharp::FreeDelegate RenderImageSharp::ManagedFree;
RenderImageSharp::WidthHeightDelegate RenderImageSharp::ManagedWidth;
RenderImageSharp::WidthHeightDelegate RenderImageSharp::ManagedHeight;

RIVE_DLL(void)
RenderImage_NativeInitDelegates(RenderImageSharp::FreeDelegate managedFree,
                                RenderImageSharp::WidthHeightDelegate managedWidth,
                                RenderImageSharp::WidthHeightDelegate managedHeight) {
    RenderImageSharp::ManagedFree = managedFree;
    RenderImageSharp::ManagedWidth = managedWidth;
    RenderImageSharp::ManagedHeight = managedHeight;
}

////////////////////////////////////////////////////////////////////////////////////////////////////

class RenderPaintSharp : public RenderPaint {
public:
    using FreeDelegate = void (*)(intptr_t);
    using StyleDelegate = void (*)(intptr_t, int style);
    using ColorDelegate = void (*)(intptr_t, uint32_t color);
    using LinearGradientDelegate = void (*)(intptr_t,
                                            float sx,
                                            float sy,
                                            float ex,
                                            float ey,
                                            const uint32_t colors[],
                                            const float stops[],
                                            int count);
    using RadialGradientDelegate = void (*)(intptr_t,
                                            float cx,
                                            float cy,
                                            float radius,
                                            const uint32_t colors[],
                                            const float stops[],
                                            int count);
    using ThicknessDelegate = void (*)(intptr_t, float thickness);
    using JoinDelegate = void (*)(intptr_t, int join);
    using CapDelegate = void (*)(intptr_t, int cap);
    using BlendModeDelegate = void (*)(intptr_t, int blendMode);

    static FreeDelegate ManagedFree;
    static StyleDelegate ManagedStyle;
    static ColorDelegate ManagedColor;
    static LinearGradientDelegate ManagedLinearGradient;
    static RadialGradientDelegate ManagedRadialGradient;
    static ThicknessDelegate ManagedThickness;
    static JoinDelegate ManagedJoin;
    static CapDelegate ManagedCap;
    static BlendModeDelegate ManagedBlendMode;

    RenderPaintSharp(intptr_t managedPtr) : m_ManagedPtr(managedPtr) {}
    RenderPaintSharp(const RenderPaintSharp&) = delete;
    RenderPaintSharp& operator=(const RenderPaintSharp&) = delete;
    ~RenderPaintSharp() { ManagedFree(m_ManagedPtr); };

    struct Shader : public RenderShader {
        virtual void apply(intptr_t) const = 0;
    };

    struct LinearGradientShader : public Shader {
        LinearGradientShader(float sx,
                             float sy,
                             float ex,
                             float ey,
                             const uint32_t colors[],
                             const float stops[],
                             int n) :
            sx(sx), sy(sy), ex(ex), ey(ey), colors(colors, colors + n), stops(stops, stops + n) {}
        void apply(intptr_t ptr) const override {
            ManagedLinearGradient(
                ptr, sx, sy, ex, ey, colors.data(), stops.data(), (int)colors.size());
        }
        const float sx, sy;
        const float ex, ey;
        const std::vector<uint32_t> colors;
        const std::vector<float> stops;
    };

    struct RadialGradientShader : public Shader {
        RadialGradientShader(
            float cx, float cy, float radius, const uint32_t colors[], const float stops[], int n) :
            cx(cx), cy(cy), radius(radius), colors(colors, colors + n), stops(stops, stops + n) {}
        void apply(intptr_t ptr) const override {
            ManagedRadialGradient(
                ptr, cx, cy, radius, colors.data(), stops.data(), (int)colors.size());
        }
        const float cx, cy;
        const float radius;
        const std::vector<uint32_t> colors;
        const std::vector<float> stops;
    };

    void style(RenderPaintStyle style) override { ManagedStyle(m_ManagedPtr, (int)style); }
    void color(uint32_t value) override { ManagedColor(m_ManagedPtr, value); }
    void thickness(float value) override { ManagedThickness(m_ManagedPtr, value); }
    void join(StrokeJoin value) override { ManagedJoin(m_ManagedPtr, (int)value); }
    void cap(StrokeCap value) override { ManagedCap(m_ManagedPtr, (int)value); }
    void blendMode(BlendMode value) override { ManagedBlendMode(m_ManagedPtr, (int)value); }
    void shader(rcp<RenderShader> shader) override { ((Shader*)shader.get())->apply(m_ManagedPtr); }

    const intptr_t m_ManagedPtr;
};

RenderPaintSharp::FreeDelegate RenderPaintSharp::ManagedFree;
RenderPaintSharp::StyleDelegate RenderPaintSharp::ManagedStyle;
RenderPaintSharp::ColorDelegate RenderPaintSharp::ManagedColor;
RenderPaintSharp::LinearGradientDelegate RenderPaintSharp::ManagedLinearGradient;
RenderPaintSharp::RadialGradientDelegate RenderPaintSharp::ManagedRadialGradient;
RenderPaintSharp::ThicknessDelegate RenderPaintSharp::ManagedThickness;
RenderPaintSharp::JoinDelegate RenderPaintSharp::ManagedJoin;
RenderPaintSharp::CapDelegate RenderPaintSharp::ManagedCap;
RenderPaintSharp::BlendModeDelegate RenderPaintSharp::ManagedBlendMode;

RIVE_DLL(void)
RenderPaint_NativeInitDelegates(RenderPaintSharp::FreeDelegate managedFree,
                                RenderPaintSharp::StyleDelegate managedStyle,
                                RenderPaintSharp::ColorDelegate managedColor,
                                RenderPaintSharp::LinearGradientDelegate managedLinearGradient,
                                RenderPaintSharp::RadialGradientDelegate managedRadialGradient,
                                RenderPaintSharp::ThicknessDelegate managedThickness,
                                RenderPaintSharp::JoinDelegate managedJoin,
                                RenderPaintSharp::CapDelegate managedCap,
                                RenderPaintSharp::BlendModeDelegate managedBlendMode) {
    RenderPaintSharp::ManagedFree = managedFree;
    RenderPaintSharp::ManagedStyle = managedStyle;
    RenderPaintSharp::ManagedColor = managedColor;
    RenderPaintSharp::ManagedLinearGradient = managedLinearGradient;
    RenderPaintSharp::ManagedRadialGradient = managedRadialGradient;
    RenderPaintSharp::ManagedThickness = managedThickness;
    RenderPaintSharp::ManagedJoin = managedJoin;
    RenderPaintSharp::ManagedCap = managedCap;
    RenderPaintSharp::ManagedBlendMode = managedBlendMode;
}

////////////////////////////////////////////////////////////////////////////////////////////////////

template <typename T> class RenderBufferSharp : public RenderBuffer {
public:
    RenderBufferSharp(Span<const T> span) :
        RenderBuffer(span.count()), m_Data(new T[span.count()]) {
        memcpy(m_Data.get(), span.data(), count() * sizeof(T));
    }
    const T* data() const { return m_Data.get(); }

private:
    std::unique_ptr<T[]> m_Data;
};

////////////////////////////////////////////////////////////////////////////////////////////////////

class RendererSharp : public Renderer {
public:
    using SaveDelegate = void (*)(intptr_t);
    using RestoreDelegate = void (*)(intptr_t);
    using TransformDelegate = void (*)(intptr_t, const Mat2D&);
    using DrawPathDelegate = void (*)(intptr_t, intptr_t path, intptr_t paint);
    using ClipPathDelegate = void (*)(intptr_t, intptr_t path);
    using DrawImageDelegate = void (*)(intptr_t, intptr_t image, int blendMode, float opacity);
    using DrawImageMeshDelegate = void (*)(intptr_t,
                                           intptr_t image,
                                           const float* vertices,
                                           const float* texcoords,
                                           int vertexCount,
                                           const uint16_t* indices,
                                           int indexCount,
                                           int blendMode,
                                           float opacity);

    static SaveDelegate ManagedSave;
    static RestoreDelegate ManagedRestore;
    static TransformDelegate ManagedTransform;
    static DrawPathDelegate ManagedDrawPath;
    static ClipPathDelegate ManagedClipPath;
    static DrawImageDelegate ManagedDrawImage;
    static DrawImageMeshDelegate ManagedDrawImageMesh;

    RendererSharp(intptr_t managedPtr) : m_ManagedPtr(managedPtr) {}
    RendererSharp(const RendererSharp&) = delete;
    RendererSharp& operator=(const RendererSharp&) = delete;

    void save() override { ManagedSave(m_ManagedPtr); }
    void restore() override { ManagedRestore(m_ManagedPtr); }
    void transform(const Mat2D& m) override { ManagedTransform(m_ManagedPtr, m); }
    void drawPath(RenderPath* path, RenderPaint* paint) override {
        ManagedDrawPath(m_ManagedPtr,
                        static_cast<RenderPathSharp*>(path)->m_ManagedPtr,
                        static_cast<RenderPaintSharp*>(paint)->m_ManagedPtr);
    }
    void clipPath(RenderPath* path) override {
        ManagedClipPath(m_ManagedPtr, static_cast<RenderPathSharp*>(path)->m_ManagedPtr);
    }
    void drawImage(const RenderImage* image, BlendMode blendMode, float opacity) override {
        ManagedDrawImage(m_ManagedPtr,
                         static_cast<const RenderImageSharp*>(image)->m_ManagedPtr,
                         (int)blendMode,
                         opacity);
    }
    void drawImageMesh(const RenderImage* image,
                       rcp<RenderBuffer> vertices_f32,
                       rcp<RenderBuffer> uvCoords_f32,
                       rcp<RenderBuffer> indices_u16,
                       BlendMode blendMode,
                       float opacity) override {
        assert(vertices_f32->count() == uvCoords_f32->count());
        assert(vertices_f32->count() % 2 == 0);

        // The local matrix is ignored for SkCanvas::drawVertices, so we have to manually scale the
        // UVs to match Skia's convention.
        float w = (float)image->width();
        float h = (float)image->height();
        int n = (int)uvCoords_f32->count();
        const float* uvs = static_cast<const RenderBufferSharp<float>*>(uvCoords_f32.get())->data();
        std::vector<float> denormUVs(n);
        for (int i = 0; i < n; i += 2) {
            denormUVs[i] = uvs[i] * w;
            denormUVs[i + 1] = uvs[i + 1] * h;
        }

        ManagedDrawImageMesh(
            m_ManagedPtr,
            static_cast<const RenderImageSharp*>(image)->m_ManagedPtr,
            static_cast<const RenderBufferSharp<float>*>(vertices_f32.get())->data(),
            denormUVs.data(),
            (int)vertices_f32->count() / 2,
            static_cast<const RenderBufferSharp<uint16_t>*>(indices_u16.get())->data(),
            (int)indices_u16->count(),
            (int)blendMode,
            opacity);
    }

private:
    intptr_t m_ManagedPtr;
};

RendererSharp::SaveDelegate RendererSharp::ManagedSave;
RendererSharp::RestoreDelegate RendererSharp::ManagedRestore;
RendererSharp::TransformDelegate RendererSharp::ManagedTransform;
RendererSharp::DrawPathDelegate RendererSharp::ManagedDrawPath;
RendererSharp::ClipPathDelegate RendererSharp::ManagedClipPath;
RendererSharp::DrawImageDelegate RendererSharp::ManagedDrawImage;
RendererSharp::DrawImageMeshDelegate RendererSharp::ManagedDrawImageMesh;

RIVE_DLL(void)
Renderer_NativeInitDelegates(RendererSharp::SaveDelegate managedSave,
                             RendererSharp::RestoreDelegate managedRestore,
                             RendererSharp::TransformDelegate managedTransform,
                             RendererSharp::DrawPathDelegate managedDrawPath,
                             RendererSharp::ClipPathDelegate managedClipPath,
                             RendererSharp::DrawImageDelegate managedDrawImage,
                             RendererSharp::DrawImageMeshDelegate managedDrawImageMesh) {
    RendererSharp::ManagedSave = managedSave;
    RendererSharp::ManagedRestore = managedRestore;
    RendererSharp::ManagedTransform = managedTransform;
    RendererSharp::ManagedDrawPath = managedDrawPath;
    RendererSharp::ManagedClipPath = managedClipPath;
    RendererSharp::ManagedDrawImage = managedDrawImage;
    RendererSharp::ManagedDrawImageMesh = managedDrawImageMesh;
}

RIVE_DLL(void)
Renderer_NativeComputeAlignment(
    int fit, float alignX, float alignY, const AABB& frame, const AABB& content, Mat2D& outMatrix) {
    outMatrix = computeAlignment((Fit)fit, Alignment(alignX, alignY), frame, content);
}

////////////////////////////////////////////////////////////////////////////////////////////////////

class FactorySharp : public Factory {
public:
    using FreeDelegate = void (*)(intptr_t ptr);
    using MakeRenderPathDelegate = intptr_t (*)(intptr_t ptr);
    using MakeRenderPaintDelegate = intptr_t (*)(intptr_t ptr);
    using DecodeImageDelegate = intptr_t (*)(intptr_t ptr, const uint8_t* bytes, int n);

    static FreeDelegate ManagedFree;
    static MakeRenderPathDelegate MangedMakeRenderPath;
    static MakeRenderPaintDelegate MangedMakeRenderPaint;
    static DecodeImageDelegate MangedDecodeImage;

    FactorySharp(intptr_t managedPtr) : m_ManagedPtr(managedPtr) {}
    ~FactorySharp() { ManagedFree(m_ManagedPtr); }

    rcp<RenderBuffer> makeBufferU16(Span<const uint16_t> span) override {
        return rcp<RenderBuffer>(new RenderBufferSharp<uint16_t>(span));
    }
    rcp<RenderBuffer> makeBufferU32(Span<const uint32_t> span) override {
        return rcp<RenderBuffer>(new RenderBufferSharp<uint32_t>(span));
    }
    rcp<RenderBuffer> makeBufferF32(Span<const float> span) override {
        return rcp<RenderBuffer>(new RenderBufferSharp<float>(span));
    }

    rcp<RenderShader> makeLinearGradient(float sx,
                                         float sy,
                                         float ex,
                                         float ey,
                                         const ColorInt colors[], // [count]
                                         const float stops[],     // [count]
                                         size_t count,
                                         RenderTileMode tileMode,
                                         const Mat2D* localMatrix) override {
        assert(tileMode == RenderTileMode::clamp); // Not yet implemented.
        assert(!localMatrix);                      // Not yet implemented.
        return rcp<RenderShader>(
            new RenderPaintSharp::LinearGradientShader(sx, sy, ex, ey, colors, stops, (int)count));
    }

    rcp<RenderShader> makeRadialGradient(float cx,
                                         float cy,
                                         float radius,
                                         const ColorInt colors[], // [count]
                                         const float stops[],     // [count]
                                         size_t count,
                                         RenderTileMode tileMode,
                                         const Mat2D* localMatrix) override {
        assert(tileMode == RenderTileMode::clamp); // Not yet implemented.
        assert(!localMatrix);                      // Not yet implemented.
        return rcp<RenderShader>(
            new RenderPaintSharp::RadialGradientShader(cx, cy, radius, colors, stops, (int)count));
    }

    std::unique_ptr<RenderPath>
    makeRenderPath(Span<const Vec2D> points, Span<const uint8_t> verbs, FillRule) override {
        assert(!"Not yet implemented. This method appears unused?");
        return nullptr;
    }

    std::unique_ptr<RenderPath> makeEmptyRenderPath() override {
        return std::make_unique<RenderPathSharp>(MangedMakeRenderPath(m_ManagedPtr));
    }

    std::unique_ptr<RenderPaint> makeRenderPaint() override {
        return std::make_unique<RenderPaintSharp>(MangedMakeRenderPaint(m_ManagedPtr));
    }

    std::unique_ptr<RenderImage> decodeImage(Span<const uint8_t> bytes) override {
        intptr_t managedPtr = MangedDecodeImage(m_ManagedPtr, bytes.data(), bytes.count());
        return managedPtr ? std::make_unique<RenderImageSharp>(managedPtr) : nullptr;
    }

    const intptr_t m_ManagedPtr;
};

FactorySharp::FreeDelegate FactorySharp::ManagedFree;
FactorySharp::MakeRenderPathDelegate FactorySharp::MangedMakeRenderPath;
FactorySharp::MakeRenderPaintDelegate FactorySharp::MangedMakeRenderPaint;
FactorySharp::DecodeImageDelegate FactorySharp::MangedDecodeImage;

RIVE_DLL(void)
Factory_NativeInitDelegates(FactorySharp::FreeDelegate managedFree,
                            FactorySharp::MakeRenderPathDelegate managedMakeRenderPath,
                            FactorySharp::MakeRenderPaintDelegate managedMakeRenderPaint,
                            FactorySharp::DecodeImageDelegate managedDecodeImage) {
    FactorySharp::ManagedFree = managedFree;
    FactorySharp::MangedMakeRenderPath = managedMakeRenderPath;
    FactorySharp::MangedMakeRenderPaint = managedMakeRenderPaint;
    FactorySharp::MangedDecodeImage = managedDecodeImage;
}

////////////////////////////////////////////////////////////////////////////////////////////////////

class NativeScene {
public:
    NativeScene(std::unique_ptr<FactorySharp> factory) : m_Factory(std::move(factory)) {}

    bool loadFile(const uint8_t* fileBytes, int length) {
        m_Scene.reset();
        m_Artboard.reset();
        m_File = File::import(Span<const uint8_t>(fileBytes, length), m_Factory.get());
        return m_File != nullptr;
    }

    bool loadStateMachine(const char* name) {
        if (m_Artboard) {
            m_Scene = (name && name[0]) ? m_Artboard->stateMachineNamed(name)
                                        : m_Artboard->stateMachineAt(0);
        }
        return m_Scene != nullptr;
    }

    bool loadArtboard(const char* name) {
        m_Scene.reset();
        if (m_File) {
            m_Artboard =
                (name && name[0]) ? m_File->artboardNamed(name) : m_File->artboardDefault();
        }
        return m_Artboard != nullptr;
    }

    bool loadAnimation(const char* name) {
        if (m_Artboard) {
            m_Scene =
                (name && name[0]) ? m_Artboard->animationNamed(name) : m_Artboard->animationAt(0);
        }
        return m_Scene != nullptr;
    }

    bool setBool(const char* name, bool value) {
        if (SMIBool * input; m_Scene && (input = m_Scene->getBool(name))) {
            input->value(value);
            return true;
        }
        return false;
    }

    bool setNumber(const char* name, float value) {
        if (SMINumber * input; m_Scene && (input = m_Scene->getNumber(name))) {
            input->value(value);
            return true;
        }
        return false;
    }

    bool fireTrigger(const char* name) {
        if (SMITrigger * input; m_Scene && (input = m_Scene->getTrigger(name))) {
            input->fire();
            return true;
        }
        return false;
    }

    Scene* scene() { return m_Scene.get(); }

private:
    std::unique_ptr<FactorySharp> m_Factory;
    std::unique_ptr<File> m_File;
    std::unique_ptr<ArtboardInstance> m_Artboard;
    std::unique_ptr<Scene> m_Scene;
};

RIVE_DLL(intptr_t)
Scene_NativeNew(intptr_t managedFactory) {
    return reinterpret_cast<intptr_t>(
        new NativeScene(std::make_unique<FactorySharp>(managedFactory)));
}

RIVE_DLL(void) Scene_NativeDelete(intptr_t ptr) { delete reinterpret_cast<NativeScene*>(ptr); }

RIVE_DLL(bool) Scene_NativeLoadFile(intptr_t ptr, const uint8_t* fileBytes, int length) {
    return reinterpret_cast<NativeScene*>(ptr)->loadFile(fileBytes, length);
}

RIVE_DLL(bool) Scene_NativeLoadArtboard(intptr_t ptr, const char* name) {
    return reinterpret_cast<NativeScene*>(ptr)->loadArtboard(name);
}

RIVE_DLL(bool) Scene_NativeLoadStateMachine(intptr_t ptr, const char* name) {
    return reinterpret_cast<NativeScene*>(ptr)->loadStateMachine(name);
}

RIVE_DLL(bool) Scene_NativeLoadAnimation(intptr_t ptr, const char* name) {
    return reinterpret_cast<NativeScene*>(ptr)->loadAnimation(name);
}

RIVE_DLL(bool) Scene_NativeSetBool(intptr_t ptr, const char* name, bool value) {
    return reinterpret_cast<NativeScene*>(ptr)->setBool(name, value);
}

RIVE_DLL(bool) Scene_NativeSetNumber(intptr_t ptr, const char* name, float value) {
    return reinterpret_cast<NativeScene*>(ptr)->setNumber(name, value);
}

RIVE_DLL(bool) Scene_NativeFireTrigger(intptr_t ptr, const char* name) {
    return reinterpret_cast<NativeScene*>(ptr)->fireTrigger(name);
}

RIVE_DLL(float) Scene_NativeWidth(intptr_t ptr) {
    if (Scene* scene = reinterpret_cast<NativeScene*>(ptr)->scene()) {
        return scene->width();
    }
    return 0;
}

RIVE_DLL(float) Scene_NativeHeight(intptr_t ptr) {
    if (Scene* scene = reinterpret_cast<NativeScene*>(ptr)->scene()) {
        return scene->height();
    }
    return 0;
}

RIVE_DLL(const char*) Scene_NativeName(intptr_t ptr) {
    if (Scene* scene = reinterpret_cast<NativeScene*>(ptr)->scene()) {
        static __declspec(thread) std::string name = scene->name();
        return name.c_str();
    }
    return "";
}

RIVE_DLL(int) Scene_NativeLoop(intptr_t ptr) {
    if (Scene* scene = reinterpret_cast<NativeScene*>(ptr)->scene()) {
        return (int)reinterpret_cast<NativeScene*>(ptr)->scene()->loop();
    }
    return 0;
}

RIVE_DLL(bool) Scene_NativeIsTranslucent(intptr_t ptr) {
    if (Scene* scene = reinterpret_cast<NativeScene*>(ptr)->scene()) {
        return reinterpret_cast<NativeScene*>(ptr)->scene()->isTranslucent();
    }
    return 0;
}

RIVE_DLL(float) Scene_NativeDurationSeconds(intptr_t ptr) {
    if (Scene* scene = reinterpret_cast<NativeScene*>(ptr)->scene()) {
        return scene->durationSeconds();
    }
    return 0;
}

RIVE_DLL(bool) Scene_NativeAdvanceAndApply(intptr_t ptr, float elapsedSeconds) {
    if (Scene* scene = reinterpret_cast<NativeScene*>(ptr)->scene()) {
        return scene->advanceAndApply(elapsedSeconds);
    }
    return false;
}

RIVE_DLL(void) Scene_NativeDraw(intptr_t ptr, intptr_t renderer) {
    if (Scene* scene = reinterpret_cast<NativeScene*>(ptr)->scene()) {
        RendererSharp nativeRenderer(renderer);
        return scene->draw(&nativeRenderer);
    }
}

RIVE_DLL(void) Scene_NativePointerDown(intptr_t ptr, const Vec2D& pos) {
    if (Scene* scene = reinterpret_cast<NativeScene*>(ptr)->scene()) {
        return scene->pointerDown(pos);
    }
}

RIVE_DLL(void) Scene_NativePointerMove(intptr_t ptr, const Vec2D& pos) {
    if (Scene* scene = reinterpret_cast<NativeScene*>(ptr)->scene()) {
        return scene->pointerMove(pos);
    }
}

RIVE_DLL(void) Scene_NativePointerUp(intptr_t ptr, const Vec2D& pos) {
    if (Scene* scene = reinterpret_cast<NativeScene*>(ptr)->scene()) {
        return scene->pointerUp(pos);
    }
}
