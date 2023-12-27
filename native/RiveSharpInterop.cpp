#include "rive/animation/state_machine_input_instance.hpp"
#include "rive/factory.hpp"
#include "utils/factory_utils.hpp"
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

#ifndef WASM
#define RIVE_DLL(RET) extern "C" __declspec(dllexport) RET __cdecl
#else
#define RIVE_DLL(RET) extern "C" __attribute__((visibility("default"))) RET __cdecl
#endif

// Native P/Invoke functions may only return "blittable" types. To protect from inadvertently
// returning an invalid type, we explicitly enumerate valid return types here. See:
// https://docs.microsoft.com/en-us/dotnet/framework/interop/blittable-and-non-blittable-types
#define RIVE_DLL_VOID RIVE_DLL(void)
#define RIVE_DLL_INT8_BOOL RIVE_DLL(int8_t)
#define RIVE_DLL_INT32 RIVE_DLL(int32_t)
#define RIVE_DLL_FLOAT RIVE_DLL(float)
#define RIVE_DLL_INTPTR RIVE_DLL(intptr_t)

// Reverse P/Invoke Function pointers back into managed code are also __cdecl and may also only
// return blittable types.
#define RIVE_DELEGATE_VOID(NAME, ...) void(__cdecl * NAME)(__VA_ARGS__)
#define RIVE_DELEGATE_INTPTR(NAME, ...) intptr_t(__cdecl* NAME)(__VA_ARGS__)
#define RIVE_DELEGATE_INT32(NAME, ...) int32_t(__cdecl* NAME)(__VA_ARGS__)

////////////////////////////////////////////////////////////////////////////////////////////////////

RIVE_DLL_VOID CopySKPointArray(intptr_t sourceArray, Vec2D* destination, int32_t count)
{
    const Vec2D* source = reinterpret_cast<const Vec2D*>(sourceArray);
    memcpy(destination, source, count * sizeof(Vec2D));
}

RIVE_DLL_VOID CopyU32Array(intptr_t sourceArray, uint32_t* destination, int32_t count)
{
    const uint32_t* source = reinterpret_cast<const uint32_t*>(sourceArray);
    memcpy(destination, source, count * sizeof(uint32_t));
}

RIVE_DLL_VOID CopyU16Array(intptr_t sourceArray, uint16_t* destination, int32_t count)
{
    const uint16_t* source = reinterpret_cast<const uint16_t*>(sourceArray);
    memcpy(destination, source, count * sizeof(uint16_t));
}

////////////////////////////////////////////////////////////////////////////////////////////////////

RIVE_DLL_VOID Mat2D_Multiply(Mat2D a, Mat2D b, Mat2D* out) { *out = a * b; }
RIVE_DLL_VOID Mat2D_MultiplyVec2D(Mat2D a, Vec2D b, Vec2D* out) { *out = a * b; }
RIVE_DLL_INT8_BOOL Mat2D_Invert(Mat2D a, Mat2D* out) { return a.invert(out); }

////////////////////////////////////////////////////////////////////////////////////////////////////

class RenderPathSharp : public RenderPath
{
public:
    struct Delegates
    {
        RIVE_DELEGATE_VOID(release, intptr_t ref);
        RIVE_DELEGATE_VOID(rewind, intptr_t ref);
        RIVE_DELEGATE_VOID(addRenderPath,
                           intptr_t ref,
                           intptr_t path,
                           float,
                           float,
                           float,
                           float,
                           float,
                           float);
        RIVE_DELEGATE_VOID(fillRule, intptr_t ref, int rule);
        RIVE_DELEGATE_VOID(moveTo, intptr_t ref, float x, float y);
        RIVE_DELEGATE_VOID(lineTo, intptr_t ref, float x, float y);
        RIVE_DELEGATE_VOID(quadTo, intptr_t ref, float ox, float oy, float x, float y);
        RIVE_DELEGATE_VOID(cubicTo,
                           intptr_t ref,
                           float ox,
                           float oy,
                           float ix,
                           float iy,
                           float x,
                           float y);
        RIVE_DELEGATE_VOID(close, intptr_t ref);
    };

    static Delegates s_delegates;

    RenderPathSharp(intptr_t managedRef) : m_ref(managedRef) {}
    RenderPathSharp(const RenderPathSharp&) = delete;
    RenderPathSharp& operator=(const RenderPathSharp&) = delete;
    ~RenderPathSharp() { s_delegates.release(m_ref); };

    void rewind() override { s_delegates.rewind(m_ref); }
    void fillRule(FillRule value) override { s_delegates.fillRule(m_ref, (int)value); }
    void addRenderPath(RenderPath* path, const Mat2D& m) override
    {
        s_delegates.addRenderPath(m_ref,
                                  static_cast<RenderPathSharp*>(path)->m_ref,
                                  m.xx(),
                                  m.xy(),
                                  m.yx(),
                                  m.yy(),
                                  m.tx(),
                                  m.ty());
    }
    void moveTo(float x, float y) override { s_delegates.moveTo(m_ref, x, y); }
    void lineTo(float x, float y) override { s_delegates.lineTo(m_ref, x, y); }
    void cubicTo(float ox, float oy, float ix, float iy, float x, float y) override
    {
        s_delegates.cubicTo(m_ref, ox, oy, ix, iy, x, y);
    }
    void close() override { s_delegates.close(m_ref); }

    // not an override, but needed for makeRenderPath
    void quadTo(float ox, float oy, float x, float y) { s_delegates.quadTo(m_ref, ox, oy, x, y); }

    const intptr_t m_ref;
};

RenderPathSharp::Delegates RenderPathSharp::s_delegates{};

RIVE_DLL_VOID RenderPath_RegisterDelegates(RenderPathSharp::Delegates delegates)
{
    RenderPathSharp::s_delegates = delegates;
}

////////////////////////////////////////////////////////////////////////////////////////////////////

class RenderImageSharp : public RenderImage
{
public:
    struct Delegates
    {
        RIVE_DELEGATE_VOID(release, intptr_t ref);
        RIVE_DELEGATE_INT32(width, intptr_t ref);
        RIVE_DELEGATE_INT32(height, intptr_t ref);
    };

    static Delegates s_delegates;

    RenderImageSharp(intptr_t managedRef) : m_ref(managedRef)
    {
        m_Width = s_delegates.width(m_ref);
        m_Height = s_delegates.height(m_ref);
    }
    RenderImageSharp(const RenderImageSharp&) = delete;
    RenderImageSharp& operator=(const RenderImageSharp&) = delete;
    ~RenderImageSharp() { s_delegates.release(m_ref); };

    const intptr_t m_ref;
};

RenderImageSharp::Delegates RenderImageSharp::s_delegates{};

RIVE_DLL_VOID RenderImage_RegisterDelegates(RenderImageSharp::Delegates delegates)
{
    RenderImageSharp::s_delegates = delegates;
}

////////////////////////////////////////////////////////////////////////////////////////////////////

class RenderPaintSharp : public RenderPaint
{
public:
    struct Delegates
    {
        RIVE_DELEGATE_VOID(release, intptr_t ref);
        RIVE_DELEGATE_VOID(style, intptr_t ref, int style);
        RIVE_DELEGATE_VOID(color, intptr_t ref, uint32_t color);
        RIVE_DELEGATE_VOID(linearGradient,
                           intptr_t ref,
                           float sx,
                           float sy,
                           float ex,
                           float ey,
                           const uint32_t colors[],
                           const float stops[],
                           int count);
        RIVE_DELEGATE_VOID(radialGradient,
                           intptr_t ref,
                           float cx,
                           float cy,
                           float radius,
                           const uint32_t colors[],
                           const float stops[],
                           int count);
        RIVE_DELEGATE_VOID(thickness, intptr_t ref, float thickness);
        RIVE_DELEGATE_VOID(join, intptr_t ref, int join);
        RIVE_DELEGATE_VOID(cap, intptr_t ref, int cap);
        RIVE_DELEGATE_VOID(blendMode, intptr_t ref, int blendMode);
    };

    static Delegates s_delegates;

    RenderPaintSharp(intptr_t managedRef) : m_ref(managedRef) {}
    RenderPaintSharp(const RenderPaintSharp&) = delete;
    RenderPaintSharp& operator=(const RenderPaintSharp&) = delete;
    ~RenderPaintSharp() { s_delegates.release(m_ref); };

    struct Shader : public RenderShader
    {
        virtual void apply(intptr_t) const = 0;
    };

    struct LinearGradientShader : public Shader
    {
        LinearGradientShader(float sx,
                             float sy,
                             float ex,
                             float ey,
                             const uint32_t colors[],
                             const float stops[],
                             int n) :
            sx(sx), sy(sy), ex(ex), ey(ey), colors(colors, colors + n), stops(stops, stops + n)
        {}
        void apply(intptr_t ref) const override
        {
            s_delegates.linearGradient(ref,
                                       sx,
                                       sy,
                                       ex,
                                       ey,
                                       colors.data(),
                                       stops.data(),
                                       (int)colors.size());
        }
        const float sx, sy;
        const float ex, ey;
        const std::vector<uint32_t> colors;
        const std::vector<float> stops;
    };

    struct RadialGradientShader : public Shader
    {
        RadialGradientShader(float cx,
                             float cy,
                             float radius,
                             const uint32_t colors[],
                             const float stops[],
                             int n) :
            cx(cx), cy(cy), radius(radius), colors(colors, colors + n), stops(stops, stops + n)
        {}
        void apply(intptr_t ref) const override
        {
            s_delegates.radialGradient(ref,
                                       cx,
                                       cy,
                                       radius,
                                       colors.data(),
                                       stops.data(),
                                       (int)colors.size());
        }
        const float cx, cy;
        const float radius;
        const std::vector<uint32_t> colors;
        const std::vector<float> stops;
    };

    void style(RenderPaintStyle style) override { s_delegates.style(m_ref, (int)style); }
    void color(uint32_t value) override { s_delegates.color(m_ref, value); }
    void thickness(float value) override { s_delegates.thickness(m_ref, value); }
    void join(StrokeJoin value) override { s_delegates.join(m_ref, (int)value); }
    void cap(StrokeCap value) override { s_delegates.cap(m_ref, (int)value); }
    void blendMode(BlendMode value) override { s_delegates.blendMode(m_ref, (int)value); }
    void shader(rcp<RenderShader> shader) override { ((Shader*)shader.get())->apply(m_ref); }
    void invalidateStroke() override {}

    const intptr_t m_ref;
};

RenderPaintSharp::Delegates RenderPaintSharp::s_delegates{};

RIVE_DLL_VOID RenderPaint_RegisterDelegates(RenderPaintSharp::Delegates delegates)
{
    RenderPaintSharp::s_delegates = delegates;
}

////////////////////////////////////////////////////////////////////////////////////////////////////

class RendererSharp : public Renderer
{
public:
    struct Delegates
    {
        RIVE_DELEGATE_VOID(save, intptr_t ref);
        RIVE_DELEGATE_VOID(restore, intptr_t ref);
        RIVE_DELEGATE_VOID(transform, intptr_t ref, float, float, float, float, float, float);
        RIVE_DELEGATE_VOID(drawPath, intptr_t ref, intptr_t path, intptr_t paint);
        RIVE_DELEGATE_VOID(clipPath, intptr_t ref, intptr_t path);
        RIVE_DELEGATE_VOID(drawImage, intptr_t ref, intptr_t image, int blendMode, float opacity);
        RIVE_DELEGATE_VOID(drawImageMesh,
                           intptr_t ref,
                           intptr_t image,
                           const float* vertices,
                           const float* texcoords,
                           int vertexCount,
                           const uint16_t* indices,
                           int indexCount,
                           int blendMode,
                           float opacity);
    };

    static Delegates s_delegates;

    RendererSharp(intptr_t managedRef) : m_ref(managedRef) {}
    RendererSharp(const RendererSharp&) = delete;
    RendererSharp& operator=(const RendererSharp&) = delete;

    void save() override { s_delegates.save(m_ref); }
    void restore() override { s_delegates.restore(m_ref); }
    void transform(const Mat2D& m) override
    {
        s_delegates.transform(m_ref, m.xx(), m.xy(), m.yx(), m.yy(), m.tx(), m.ty());
    }
    void drawPath(RenderPath* path, RenderPaint* paint) override
    {
        s_delegates.drawPath(m_ref,
                             static_cast<RenderPathSharp*>(path)->m_ref,
                             static_cast<RenderPaintSharp*>(paint)->m_ref);
    }
    void clipPath(RenderPath* path) override
    {
        s_delegates.clipPath(m_ref, static_cast<RenderPathSharp*>(path)->m_ref);
    }
    void drawImage(const RenderImage* image, BlendMode blendMode, float opacity) override
    {
        s_delegates.drawImage(m_ref,
                              static_cast<const RenderImageSharp*>(image)->m_ref,
                              (int)blendMode,
                              opacity);
    }
    void drawImageMesh(const RenderImage* image,
                       rcp<RenderBuffer> vertices_f32,
                       rcp<RenderBuffer> uvCoords_f32,
                       rcp<RenderBuffer> indices_u16,
                       uint32_t vertexCount,
                       uint32_t indexCount,
                       BlendMode blendMode,
                       float opacity) override
    {
        // We need our buffers and counts to agree.
        assert(vertices_f32->sizeInBytes() == vertexCount * sizeof(Vec2D));
        assert(uvCoords_f32->sizeInBytes() == vertexCount * sizeof(Vec2D));
        assert(indices_u16->sizeInBytes() == indexCount * sizeof(uint16_t));

        // The local matrix is ignored for SkCanvas::drawVertices, so we have to manually scale the
        // UVs to match Skia's convention.
        float w = (float)image->width();
        float h = (float)image->height();
        int n = vertexCount * 2;
        const float* uvs = static_cast<DataRenderBuffer*>(uvCoords_f32.get())->f32s();
        std::vector<float> denormUVs(n);
        for (int i = 0; i < n; i += 2)
        {
            denormUVs[i] = uvs[i] * w;
            denormUVs[i + 1] = uvs[i + 1] * h;
        }

        s_delegates.drawImageMesh(m_ref,
                                  static_cast<const RenderImageSharp*>(image)->m_ref,
                                  static_cast<DataRenderBuffer*>(vertices_f32.get())->f32s(),
                                  denormUVs.data(),
                                  vertexCount,
                                  static_cast<DataRenderBuffer*>(indices_u16.get())->u16s(),
                                  indexCount,
                                  (int)blendMode,
                                  opacity);
    }

private:
    intptr_t m_ref;
};

RendererSharp::Delegates RendererSharp::s_delegates{};

RIVE_DLL_VOID Renderer_RegisterDelegates(RendererSharp::Delegates delegates)
{
    RendererSharp::s_delegates = delegates;
}

struct ComputeAlignmentArgs
{
    int32_t fit;
    float alignX;
    float alignY;
    AABB frame;
    AABB content;
    Mat2D matrix;
};

RIVE_DLL_VOID Renderer_ComputeAlignment(ComputeAlignmentArgs* args)
{
    args->matrix = computeAlignment((Fit)args->fit,
                                    Alignment(args->alignX, args->alignY),
                                    args->frame,
                                    args->content);
}

////////////////////////////////////////////////////////////////////////////////////////////////////

class FactorySharp : public Factory
{
public:
    struct Delegates
    {
        RIVE_DELEGATE_VOID(release, intptr_t ref);
        RIVE_DELEGATE_INTPTR(makeRenderPath,
                             intptr_t ref,
                             intptr_t ptsArray, // Vec2D/SkPoint[nPts]
                             int nPts,
                             intptr_t verbsArray, // uint8_t/PathVerb[nPts]
                             int nVerbs,
                             int fillRule);
        RIVE_DELEGATE_INTPTR(makeEmptyRenderPath, intptr_t ref);
        RIVE_DELEGATE_INTPTR(makeRenderPaint, intptr_t ref);
        RIVE_DELEGATE_INTPTR(decodeImage, intptr_t ref, intptr_t bytesArray, int nBytes);
    };

    static Delegates s_delegates;

    FactorySharp(intptr_t managedRef) : m_ref(managedRef) {}
    ~FactorySharp() { s_delegates.release(m_ref); }

    rcp<RenderBuffer> makeRenderBuffer(RenderBufferType type,
                                       RenderBufferFlags flags,
                                       size_t sizeInBytes) override
    {
        return make_rcp<DataRenderBuffer>(type, flags, sizeInBytes);
    }

    rcp<RenderShader> makeLinearGradient(float sx,
                                         float sy,
                                         float ex,
                                         float ey,
                                         const ColorInt colors[], // [count]
                                         const float stops[],     // [count]
                                         size_t count) override
    {
        return rcp<RenderShader>(
            new RenderPaintSharp::LinearGradientShader(sx, sy, ex, ey, colors, stops, (int)count));
    }

    rcp<RenderShader> makeRadialGradient(float cx,
                                         float cy,
                                         float radius,
                                         const ColorInt colors[], // [count]
                                         const float stops[],     // [count]
                                         size_t count) override
    {
        return rcp<RenderShader>(
            new RenderPaintSharp::RadialGradientShader(cx, cy, radius, colors, stops, (int)count));
    }

    rcp<RenderPath> makeRenderPath(RawPath& rawPath, FillRule fillRule) override
    {
        return make_rcp<RenderPathSharp>(
            s_delegates.makeRenderPath(m_ref,
                                       reinterpret_cast<intptr_t>(rawPath.points().data()),
                                       rawPath.points().size(),
                                       reinterpret_cast<intptr_t>(rawPath.verbs().data()),
                                       rawPath.verbs().size(),
                                       (int)fillRule));
    }

    rcp<RenderPath> makeEmptyRenderPath() override
    {
        return make_rcp<RenderPathSharp>(s_delegates.makeEmptyRenderPath(m_ref));
    }

    rcp<RenderPaint> makeRenderPaint() override
    {
        return make_rcp<RenderPaintSharp>(s_delegates.makeRenderPaint(m_ref));
    }

    rcp<RenderImage> decodeImage(Span<const uint8_t> bytes) override
    {
        intptr_t managedRef =
            s_delegates.decodeImage(m_ref, reinterpret_cast<intptr_t>(bytes.data()), bytes.count());
        return managedRef ? make_rcp<RenderImageSharp>(managedRef) : nullptr;
    }

    const intptr_t m_ref;
};

FactorySharp::Delegates FactorySharp::s_delegates{};

RIVE_DLL_VOID Factory_RegisterDelegates(FactorySharp::Delegates delegates)
{
    FactorySharp::s_delegates = delegates;
}

////////////////////////////////////////////////////////////////////////////////////////////////////

class NativeScene
{
public:
    NativeScene(std::unique_ptr<FactorySharp> factory) : m_Factory(std::move(factory)) {}

    bool loadFile(const uint8_t* fileBytes, int length)
    {
        m_Scene.reset();
        m_Artboard.reset();
        m_File = File::import(Span<const uint8_t>(fileBytes, length), m_Factory.get());
        return m_File != nullptr;
    }

    bool loadArtboard(const char* name)
    {
        m_Scene.reset();
        if (m_File)
        {
            m_Artboard =
                (name && name[0]) ? m_File->artboardNamed(name) : m_File->artboardDefault();
        }
        return m_Artboard != nullptr;
    }

    bool loadStateMachine(const char* name)
    {
        if (m_Artboard)
        {
            m_Scene = (name && name[0]) ? m_Artboard->stateMachineNamed(name)
                                        : m_Artboard->stateMachineAt(0);
        }
        return m_Scene != nullptr;
    }

    bool loadAnimation(const char* name)
    {
        if (m_Artboard)
        {
            m_Scene =
                (name && name[0]) ? m_Artboard->animationNamed(name) : m_Artboard->animationAt(0);
        }
        return m_Scene != nullptr;
    }

    bool setBool(const char* name, bool value)
    {
        if (SMIBool * input; m_Scene && (input = m_Scene->getBool(name)))
        {
            input->value(value);
            return true;
        }
        return false;
    }

    bool setNumber(const char* name, float value)
    {
        if (SMINumber * input; m_Scene && (input = m_Scene->getNumber(name)))
        {
            input->value(value);
            return true;
        }
        return false;
    }

    bool fireTrigger(const char* name)
    {
        if (SMITrigger * input; m_Scene && (input = m_Scene->getTrigger(name)))
        {
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

RIVE_DLL_INTPTR Scene_New(intptr_t managedFactory)
{
    return reinterpret_cast<intptr_t>(
        new NativeScene(std::make_unique<FactorySharp>(managedFactory)));
}

RIVE_DLL_VOID Scene_Delete(intptr_t ref) { delete reinterpret_cast<NativeScene*>(ref); }

RIVE_DLL_INT8_BOOL Scene_LoadFile(intptr_t ref, const uint8_t* fileBytes, int length)
{
    return reinterpret_cast<NativeScene*>(ref)->loadFile(fileBytes, length);
}

RIVE_DLL_INT8_BOOL Scene_LoadArtboard(intptr_t ref, const char* name)
{
    return reinterpret_cast<NativeScene*>(ref)->loadArtboard(name);
}

RIVE_DLL_INT8_BOOL Scene_LoadStateMachine(intptr_t ref, const char* name)
{
    return reinterpret_cast<NativeScene*>(ref)->loadStateMachine(name);
}

RIVE_DLL_INT8_BOOL Scene_LoadAnimation(intptr_t ref, const char* name)
{
    return reinterpret_cast<NativeScene*>(ref)->loadAnimation(name);
}

RIVE_DLL_INT8_BOOL Scene_SetBool(intptr_t ref, const char* name, int32_t value)
{
    return reinterpret_cast<NativeScene*>(ref)->setBool(name, value);
}

RIVE_DLL_INT8_BOOL Scene_SetNumber(intptr_t ref, const char* name, float value)
{
    return reinterpret_cast<NativeScene*>(ref)->setNumber(name, value);
}

RIVE_DLL_INT8_BOOL Scene_FireTrigger(intptr_t ref, const char* name)
{
    return reinterpret_cast<NativeScene*>(ref)->fireTrigger(name);
}

RIVE_DLL_FLOAT Scene_Width(intptr_t ref)
{
    if (Scene* scene = reinterpret_cast<NativeScene*>(ref)->scene())
    {
        return scene->width();
    }
    return 0;
}

RIVE_DLL_FLOAT Scene_Height(intptr_t ref)
{
    if (Scene* scene = reinterpret_cast<NativeScene*>(ref)->scene())
    {
        return scene->height();
    }
    return 0;
}

RIVE_DLL_INT32 Scene_Name(intptr_t ref, char* charArray)
{
    if (Scene* scene = reinterpret_cast<NativeScene*>(ref)->scene())
    {
        const std::string& name = scene->name();
        int32_t numChars = name.length();
        if (charArray)
        {
            memcpy(charArray, name.c_str(), numChars);
        }
        return numChars;
    }
    return 0;
}

RIVE_DLL_INT32 Scene_Loop(intptr_t ref)
{
    if (Scene* scene = reinterpret_cast<NativeScene*>(ref)->scene())
    {
        return (int)reinterpret_cast<NativeScene*>(ref)->scene()->loop();
    }
    return 0;
}

RIVE_DLL_INT8_BOOL Scene_IsTranslucent(intptr_t ref)
{
    if (Scene* scene = reinterpret_cast<NativeScene*>(ref)->scene())
    {
        return reinterpret_cast<NativeScene*>(ref)->scene()->isTranslucent();
    }
    return 0;
}

RIVE_DLL_FLOAT Scene_DurationSeconds(intptr_t ref)
{
    if (Scene* scene = reinterpret_cast<NativeScene*>(ref)->scene())
    {
        return scene->durationSeconds();
    }
    return 0;
}

RIVE_DLL_INT8_BOOL Scene_AdvanceAndApply(intptr_t ref, float elapsedSeconds)
{
    if (Scene* scene = reinterpret_cast<NativeScene*>(ref)->scene())
    {
        return scene->advanceAndApply(elapsedSeconds);
    }
    return false;
}

RIVE_DLL_VOID Scene_Draw(intptr_t ref, intptr_t renderer)
{
    if (Scene* scene = reinterpret_cast<NativeScene*>(ref)->scene())
    {
        RendererSharp nativeRenderer(renderer);
        return scene->draw(&nativeRenderer);
    }
}

RIVE_DLL_VOID Scene_PointerDown(intptr_t ref, Vec2D pos)
{
    if (Scene* scene = reinterpret_cast<NativeScene*>(ref)->scene())
    {
        return scene->pointerDown(pos);
    }
}

RIVE_DLL_VOID Scene_PointerMove(intptr_t ref, Vec2D pos)
{
    if (Scene* scene = reinterpret_cast<NativeScene*>(ref)->scene())
    {
        return scene->pointerMove(pos);
    }
}

RIVE_DLL_VOID Scene_PointerUp(intptr_t ref, Vec2D pos)
{
    if (Scene* scene = reinterpret_cast<NativeScene*>(ref)->scene())
    {
        return scene->pointerUp(pos);
    }
}
