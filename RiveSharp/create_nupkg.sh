VERSION=${1:-"1.0.0"}

if ! command -v msbuild.exe &> /dev/null
then
    echo
    echo Put msbuild.exe on your \$PATH!
    exit -1
fi

if ! command -v emcc &> /dev/null
then
    echo
    echo Set up your emscripten environment!
    exit -1
fi

if ! command -v nuget.exe &> /dev/null
then
    echo
    echo Put nuget.exe on your \$PATH!
    exit -1
fi

pushd ../native

# Build native dlls.
premake5.exe --scripts=../../runtime/build vs2022
msbuild.exe rive.vcxproj -p:Configuration=Release -p:Platform=x64 -p:UseMultiToolTask=true -p:EnforceProcessCountAcrossBuilds=true -m:10
msbuild.exe rive.vcxproj -p:Configuration=Release -p:Platform=x86 -p:UseMultiToolTask=true -p:EnforceProcessCountAcrossBuilds=true -m:10
msbuild.exe rive.vcxproj -p:Configuration=Release -p:Platform=ARM64 -p:UseMultiToolTask=true -p:EnforceProcessCountAcrossBuilds=true -m:10
msbuild.exe rive.vcxproj -p:Configuration=Release -p:Platform=ARM -p:UseMultiToolTask=true -p:EnforceProcessCountAcrossBuilds=true -m:10

# Build wasm library.
emsdk install 2.0.23
emsdk activate 2.0.23
premake5.exe --scripts=../../runtime/build --file=premake5_wasm.lua cmake
emcmake cmake .
cmake --build . -j10

popd

 #Pack!
nuget.exe restore RiveSharp.csproj
nuget.exe pack RiveSharp.csproj \
  -Build \
  -Properties Configuration=Release \
  -Properties version="$VERSION-alpha.$(git rev-parse --short HEAD)"
