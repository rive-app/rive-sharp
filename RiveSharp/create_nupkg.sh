VERSION=${1:-"1.0.0"}

MSBuild.exe ../native/rive.vcxproj -p:Configuration=Release -p:Platform=x86
MSBuild.exe ../native/rive.vcxproj -p:Configuration=Release -p:Platform=x64

nuget.exe pack RiveSharp.csproj \
  -Build \
  -Properties Configuration=Release \
  -Properties version="$VERSION-alpha.$(git rev-parse --short HEAD)"
