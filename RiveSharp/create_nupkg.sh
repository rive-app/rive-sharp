VERSION=${1:-"1.0.0"}

cd ../native
premake5 vs2022
msbuild.exe rive.vcxproj -p:Configuration=Release -p:Platform=x86 -p:UseMultiToolTask=true -p:EnforceProcessCountAcrossBuilds=true -m:10
msbuild.exe rive.vcxproj -p:Configuration=Release -p:Platform=x64 -p:UseMultiToolTask=true -p:EnforceProcessCountAcrossBuilds=true -m:10
msbuild.exe rive.vcxproj -p:Configuration=Release -p:Platform=ARM64 -p:UseMultiToolTask=true -p:EnforceProcessCountAcrossBuilds=true -m:10

cd -
nuget.exe restore RiveSharp.csproj
nuget.exe pack RiveSharp.csproj \
  -Build \
  -Properties Configuration=Release \
  -Properties version="$VERSION-alpha.$(git rev-parse --short HEAD)"
