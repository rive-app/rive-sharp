workspace "rive-cpp"

configurations {"Release"}

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
    kind "StaticLib"
    language "C++"
    cppdialect "C++17"
    targetdir "bin/wasm"
    objdir "obj/wasm"
    optimize "Size"
    flags { "FatalWarnings" }
    includedirs {
        RIVE_RUNTIME_DIR .. "/include",
        "../../include",
    }
    files {
        RIVE_RUNTIME_DIR .. "/src/**.cpp",
        "RiveSharpInterop.cpp"
    }
    defines {
        "RELEASE",
        "NDEBUG",
        "WASM"
    }
