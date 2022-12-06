workspace "rive-cpp"

configurations {"Release"}

-- Are we in the "rive-sharp" or "rive" repository?
local git_handle = io.popen("git remote -v")
local git_remote = git_handle:read("*a")
git_handle:close()
if string.find(git_remote, "rive%-sharp") then
    -- In rive-sharp. Rive runtime is a submodule.
    RIVE_RUNTIME_DIR = "../submodules/rive-cpp"
else
    -- In rive. Rive runtime is further up the tree.
    RIVE_RUNTIME_DIR = "../../runtime"
end

local emcc_handle = io.popen("emcc --version | grep ^emcc | sed 's/^.*\\([0-9]\\+\\.[0-9]\\+\\.[0-9]\\+\\).*$/\\1/'")
local emcc_version = emcc_handle:read("*a"):gsub("%s+", "")
emcc_handle:close()
TARGET_DIR = "bin/emsdk/" .. emcc_version
print("TARGET_DIR: " .. TARGET_DIR)

project "rive"
do
    kind "StaticLib"
    language "C++"
    cppdialect "C++17"
    targetdir (TARGET_DIR)
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
    filter "options:not no-exceptions"
    do
        -- WebAssembly Exceptions support is now required by Uno.
        buildoptions { "-fwasm-exceptions" }
    end
end

newoption {
    trigger = 'no-exceptions',
    description = 'build without -fwasm-exceptions',
}
