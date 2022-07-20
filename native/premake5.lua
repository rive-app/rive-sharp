workspace "rive-cpp"
    configurations {"debug", "release"}

-- Are we in the "rive-sharp" or "rive" repository?
local handle = io.popen("git remote -v")
local git_remote = handle:read("*a")
handle:close()
if string.find(git_remote, "rive%-sharp") then
    -- In rive-sharp. Rive runtime is a submodule.
    RIVE_RUNTIME_DIR = "../submodules/rive-cpp"
else
    -- In rive. Rive runtime is further up the tree.
    RIVE_RUNTIME_DIR = "../../runtime"
end

project "rive"
    kind "SharedLib"
    language "C++"
    cppdialect "C++17"
    toolset "msc"
    architecture "x64"
    targetdir "bin/x64/%{cfg.buildcfg}"
    objdir "obj/x64/%{cfg.buildcfg}"
    staticruntime "off"  -- /MD for dll
    flags { "FatalWarnings" }
    buildoptions {
        -- /ZW is the flag for UWP compilation:
        --     https://docs.microsoft.com/en-us/cpp/build/reference/zw-windows-runtime-compilation
        "/ZW:nostdlib",
        "/EHsc"  -- Required with /ZW
    }
    includedirs {
        RIVE_RUNTIME_DIR .. "/include",
        "../../include",
    }
    files {
        RIVE_RUNTIME_DIR .. "/src/**.cpp",
        "RiveSharpInterop.cpp"
    }

    filter "configurations:debug"
        defines {"DEBUG"}
        symbols "On"

    filter "configurations:release"
        defines {"RELEASE"}
        defines {"NDEBUG"}
        optimize "On"

    filter {}

    -- For future use: Compile in GMs
    -- newoption {
    --    trigger = "include-gms",
    --    description = "Include source files for Rive GMs"
    -- }
    --
    -- filter "options:include-gms"
    --     includedirs {
    --     }
    --     files {
    --     }
